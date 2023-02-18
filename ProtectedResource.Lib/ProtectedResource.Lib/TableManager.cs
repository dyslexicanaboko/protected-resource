using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ProtectedResource.Lib.DataAccess;
using ProtectedResource.Lib.Events;
using ProtectedResource.Lib.Models;
using ProtectedResource.Lib.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProtectedResource.Lib
{
	/// <summary>
	/// TODO: Statistics should be collected so long as it doesn't degrade performance
	/// </summary>
	/// <typeparam name="TResource"></typeparam>
	public class TableManager<TResource>
		where TResource : IResource, new()
	{
		private readonly Dictionary<string, PartitionWatcher<TResource>> _partitionWatchers;
		private readonly IQueryToClassRepository _repository;
		private readonly ICachingService _cachingService;
		private readonly IMessagingQueueService _messagingQueueService;
		private int _chunkSize;
		private string _resourceKey;
		private SchemaQuery _schema;
		private string _selectByPartitionKey;
		private string _updateByPartitionKeyTemplate;
		private readonly IConfigurationService _config;
		private readonly ILogger<TableManager<TResource>> _logger;
		
		public TableManager(
			IQueryToClassRepository repository,
			ICachingService cachingService,
			IMessagingQueueService messagingQueueService,
			IConfigurationService config,
			ILogger<TableManager<TResource>> logger)
		{
			_repository = repository;

			_cachingService = cachingService;

			_messagingQueueService = messagingQueueService;

			_partitionWatchers = new Dictionary<string, PartitionWatcher<TResource>>();

			_config = config;

			_logger = logger;
		}

		public void Initialize(TableQuery tableQuery, int chunkSize)
		{
			_chunkSize = chunkSize;

			var t = tableQuery;

			_resourceKey = $"{t.Database}_{t.Schema}_{t.Table}";

			var query = $"SET FMTONLY ON; SELECT * FROM {t.Schema}.{t.Table}; SET FMTONLY OFF;";

			_schema = _repository.GetSchema(t, query);

			var selectList = string.Join(",", _schema.ColumnsAll.Select(x => x.ColumnName));

			_selectByPartitionKey = $@"SELECT {selectList} 
				FROM {t.Schema}.{t.Table} 
				WHERE {_schema.PrimaryKey.ColumnName} = @{_schema.PrimaryKey.ColumnName}
				FOR JSON AUTO, WITHOUT_ARRAY_WRAPPER";

			//Would it be better to just create all permutations of update list up front and select it based on hash? -- Issue #5
			_updateByPartitionKeyTemplate = $@"UPDATE {t.Schema}.{t.Table} SET
				{{0}}
				WHERE {_schema.PrimaryKey.ColumnName} = @{_schema.PrimaryKey.ColumnName}";
		}

		//I would want to be able to process multiple streams of changes in one call
		//  I am thinking like taking 10 requests at a time and performing a merge where last in wins for conflicts
		//  If anything needs to happen as a result of a value changing, it will be fired off but this is the wrong place for that to happen
		//The resource can be partitioned by grouping if one exists
		private void ProcessPartition(PartitionWatcher<TResource> partitionWatcher, int chunkSize)
		{
			try
			{
				//Since the partition is being processed, the timer should be stopped and the watcher
				//should set its status as busy.
				partitionWatcher.SetUnavailable();
				partitionWatcher.StopTimer();

				//Take a chunk of the items in the queue
				var length = partitionWatcher.Count >= chunkSize ? chunkSize : partitionWatcher.Count;

				var arr = new ChangeRequest<TResource>[length];

				//Add them to an array in reverse order so that the representative item is last in the squash
				for (var i = length - 1; i >= 0; i--)
				{
					arr[i] = partitionWatcher.Dequeue();
				}

				//Representative object - last in and final say for squash
				var rep = arr[0];
				var repJObject = JObject.Parse(rep.PatchJson);

				/* Get cached copy
				 *     Cache will store object as Json
				 *     If cache does not have a copy, get one from the database and keep it in memory for an hour (configurable) */
				var cachedCopy = GetResource(_resourceKey, partitionWatcher.PartitionKey);
				var cachedJObject = JObject.Parse(cachedCopy);

				//Perform squash on chunk - via JSON
				//Only include fields from cached copy that will change for squash
				//https://www.newtonsoft.com/json/help/html/MergeJson.htm
				for (var i = 1; i < arr.Length; i++)
				{
					var otherJObject = JObject.Parse(arr[i].PatchJson);

					repJObject = SquashChanges(repJObject, otherJObject);
				}

				//If there was a previous failure, then merge it back in
				if (partitionWatcher.FailedCommitReference != null)
				{
					repJObject = SquashChanges(repJObject, partitionWatcher.FailedCommitReference);
				}

				//Cached will have all properties always, representative will not. Make them match.
				//This impacts the SQL creation as well
				//https://www.newtonsoft.com/json/help/html/jobjectproperties.htm
				//https://www.newtonsoft.com/json/help/html/m_newtonsoft_json_linq_jobject_remove.htm

				var partialJObject = SynchronizeProperties(repJObject, cachedJObject);

				//If the representative and cached copy are identical then there is nothing to do here
				//https://www.newtonsoft.com/json/help/html/DeepEquals.htm
				if (JToken.DeepEquals(repJObject, partialJObject)) return;

				//Clone the representative to make a copy for partial update to the database
				var repJObjectSqlCopy = new JObject(repJObject);

				//Merge representative with full copy to create the changed object to be persisted to cache
				repJObject = SquashChanges(repJObject, cachedJObject);

				//Merge representative with partial copy to create the changed object to be persisted to database
				repJObjectSqlCopy = SquashChanges(repJObjectSqlCopy, partialJObject);

				//JSON to persist to cache
				var finalJson = repJObject.ToString(Formatting.None);

				bool committed;

				try
				{
					//Partial object to use for updating the database
					_repository.UpdatePartition(
						_updateByPartitionKeyTemplate,
						partitionWatcher.PartitionKey,
						_schema,
						repJObjectSqlCopy);

					//Only successful if transaction commits with no error
					committed = true;
				}
				catch (Exception ex)
				{
					/* If transaction fails:
					 *    1. Take the merged object and throw it back on the internal queue
					 *    2. Log the error
					 *    3. Have a limit on how many times there can be a sequential failure until killing this manager */
					//TODO: Need to test out the failures and see what information should be submitted as logging parameters
					_logger.LogCritical(ex, "Transaction failed to commit.");

					committed = false;
				}

				if (committed)
				{
					//Requires the full object
					_cachingService.HashSet(_resourceKey, partitionWatcher.PartitionKey, finalJson);

					//Disassociate from any previous references
					partitionWatcher.FailedCommitReference = null;
				}
				else
				{
					//Keep a reference to what could not be committed
					partitionWatcher.FailedCommitReference = repJObjectSqlCopy;

					//Delete the partition from cache so that it will be re-cached from the database on the next loop.
					//There is a strong possibility that something changed on the database which is what caused the transaction to fail.
					//More often than not it's Deadlock.
					_cachingService.HashDelete(_resourceKey, partitionWatcher.PartitionKey);
				}
			}
			finally
			{
				partitionWatcher.SetAvailable();
			}

			/* Right now assuming no one outside will change the cached object.
			 *		After transaction - the cached version should probably be deleted just in case an outside source
			 *		is also changing the partition.
			 *
			 *		The alternative is to refresh the cache after every commit. */
		}

		/// <summary>
		/// Make the properties of the target match the properties of the template.
		/// A new copy of the target is returned.
		/// </summary>
		/// <param name="template">The properties to keep</param>
		/// <param name="target">The <see cref="JObject"/> to remove properties from</param>
		/// <returns>New copy of the target with removed properties</returns>
		private JObject SynchronizeProperties(JObject template, JObject target)
		{
			var clone = new JObject(target);

			foreach (var (propertyName, _) in target)
			{
				if (template.ContainsKey(propertyName)) continue;

				clone.Remove(propertyName);
			}

			return clone;
		}

		private JObject SquashChanges(JObject representative, JObject otherChanges)
		{
			//This feels backwards, but since I need the representative to
			//maintain dominance, it is merged on top of the other changes
			otherChanges.Merge(representative);

			//otherChanges is returned, but it is now the new representative
			return otherChanges;
		}

		private string GetResource(string resourceKey, string partitionKey)
		{
			var resource = _cachingService.HashGet(resourceKey, partitionKey);

			if (!resource.IsNull) return resource.Value;
			
			//Grab from the database
			var json = _repository.GetJson(_selectByPartitionKey, _schema.PrimaryKey, partitionKey);

			_cachingService.HashSet(resourceKey, partitionKey, json);

			return json;
		}

		//Start reading from the queue X number at a time to perform a merge -- I don't think this can be done
		//The consumer will read whatever it gets. It has to put the data in internal queues as well. One queue per partition.
		//Chunk size will be how much to process from each partition queue
		public void Start(string queueName)
		{
			_messagingQueueService.Start(queueName, ProcessChangeRequest);
		}

		//Generically process a dequeued item from the main Message Queue (RabbitMQ)
		private void ProcessChangeRequest(string json)
		{
			var entity = JsonConvert.DeserializeObject<TResource>(json);

			var key = entity.GetPartitionKey();

			//Partition the items into separate queues
			if (!_partitionWatchers.TryGetValue(key, out var watcher))
			{
				watcher = new PartitionWatcher<TResource>(_config, key);
				watcher.WhenStale += PartitionWatcher_WhenStale;

				_partitionWatchers.Add(key, watcher);
			}

			var changeRequest = new ChangeRequest<TResource>
			{
				ModifiedResource = entity,
				PatchJson = json,
				RequestToken = Guid.NewGuid(),
				PartitionKey = key
			};
			
			watcher.Enqueue(changeRequest);
			watcher.StartTimer();

			Console.WriteLine(" [x] Received {0}", json);

			//If the queue count isn't greater than the chunk size, wait for more items
			//If the watcher is busy merging a chunk already, then wait until it's available again
			if (watcher.Count < _chunkSize || watcher.IsBusy()) return;

			ProcessPartition(watcher, _chunkSize);
		}

		private void PartitionWatcher_WhenStale(object sender, StaleQueueEventArgs<TResource> e)
		{
			ProcessPartition(e.StalePartitionWatcher, _chunkSize);
		}

		//This is for debug only, going to keep it around until I don't need it anymore
		private void DebugDequeueImmediately()
		{
			foreach (var (key, value) in _partitionWatchers)
			{
				ProcessPartition(value, _chunkSize);
			}
		}
	}
}
