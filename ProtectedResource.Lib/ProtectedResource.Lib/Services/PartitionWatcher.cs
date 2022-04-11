using Newtonsoft.Json.Linq;
using ProtectedResource.Lib.Events;
using System.Collections.Generic;
using System.Timers;

namespace ProtectedResource.Lib.Services
{
    /// <summary>
    /// Watches a partition queue to make sure it is being dequeued and not getting stale.
    /// The goal is to not process until a threshold is reached in the queue, but if the threshold
    /// is never reached a timer will force the queue to be processed.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PartitionWatcher<T>
    {
        public delegate void StaleQueueHandler(object sender, StaleQueueEventArgs<T> e);

        public event StaleQueueHandler WhenStale;

        public JObject FailedCommitReference { get; set; }

        private readonly Timer _timer;

        private bool _isBusy;

        public PartitionWatcher(IConfigurationService config, string partitionKey)
            : this(config, partitionKey, new Queue<ChangeRequest<T>>())
        {
            
        }

        public PartitionWatcher(IConfigurationService config, string partitionKey, Queue<ChangeRequest<T>> queue)
        {
            PartitionKey = partitionKey;

            Queue = queue;

            _timer = new Timer();
            _timer.Interval = config.PartitionWatcherMilliseconds;
            _timer.AutoReset = false;
            _timer.Elapsed += DequeueTimer_Elapsed;
        }

        private void DequeueTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            //This will raise an event to the TableManager to dequeue this partition
            WhenStale?.Invoke(sender, new StaleQueueEventArgs<T>(this));
        }

        /// <summary>The key used to find a partition in a table.</summary>
        /// <example>Id 12 is supplied when searching by primary key to find a row in a table.</example>
        public string PartitionKey { get; }

        /// <summary>The number of change requests held for processing in the internal queue.</summary>
        public int Count => Queue.Count;

        public Queue<ChangeRequest<T>> Queue { get; }

        /// <summary>Add a change request to the back of the queue.</summary>
        public void Enqueue(ChangeRequest<T> changeRequest) => Queue.Enqueue(changeRequest);

        /// <summary>Remove a change request from the front of the queue.</summary>
        public ChangeRequest<T> Dequeue() => Queue.Dequeue();

        /// <summary>Start the stale check timer</summary>
        public void StartTimer()
        {
            //If the timer is already running then don't do it again
            if (_timer.Enabled || _isBusy) return;

            _timer.Start();
        }

        /// <summary>Stop the stale check timer</summary>
        public void StopTimer() => _timer.Stop();

        /// <summary>Determines if this partition is being processed at the moment</summary>
        public bool IsBusy() => _isBusy;

        /// <summary>Sets this partition as busy or unavailable.</summary>
        public void SetUnavailable() => _isBusy = true;

        /// <summary>Sets this partition as free or available.</summary>
        public void SetAvailable() => _isBusy = false;
    }
}
