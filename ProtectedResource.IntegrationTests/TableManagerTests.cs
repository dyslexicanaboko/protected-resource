using NUnit.Framework;
using ProtectedResource.Lib;
using ProtectedResource.Lib.DataAccess;
using ProtectedResource.Lib.Models;
using ProtectedResource.Lib.Services;

namespace ProtectedResource.IntegrationTests
{
    [TestFixture]
    public class TableManagerTests
        : TableManagerTestBase
    {
        private readonly IQueryToClassRepository _repo;
        private readonly ICachingService _cachingService;
        private readonly IConfigurationService _config;

        public TableManagerTests()
        {
            _repo = new QueryToClassRepository();

            _config = new ConfigurationService();

            //Should live as part of a singleton instance
            _cachingService = new CachingService(_config);
            _cachingService.Initialize(); //Only call once
        }

        [SetUp]
        public void Setup()
        {
            _cachingService.Clear();
        }

        private TableManager<RudimentaryEntity> GetTableManager()
        {
            //This cannot be a singleton I don't think, but should only be instantiated once per resource
            //Not sure how I am going to deal with this yet
            var queue = new MessagingQueueService(_config);

            var tm = new TableManager<RudimentaryEntity>(_repo, _cachingService, queue, _config);

            return tm;
        }

        //If Redis is not running this will fail
        [Test]
        public void Can_initialize()
        {
            var tm = GetTableManager();

            var tq = new TableQuery
            {
                Schema = "dbo",
                Table = $"{nameof(RudimentaryEntity)}"
            };

            tm.Initialize(tq, 10);

            Assert.Pass();
        }

        //RudimentaryEntityExchange
        //RudimentaryEntityQueue
        [Test]
        public void Can_start_queue()
        {
            var entity = $"{nameof(RudimentaryEntity)}";

            var tm = GetTableManager();

            var tq = new TableQuery
            {
                Schema = "dbo",
                Table = entity
            };

            tm.Initialize(tq, 10);
            tm.Start(entity + "Queue");

            Assert.Pass();
        }

        [Test]
        public void Can_process_queue_single_request()
        {
            var partitionKey = $"{nameof(RudimentaryEntity)}";

            var tm = GetTableManager();

            var tq = new TableQuery
            {
                Schema = "dbo",
                Table = partitionKey
            };

            tm.Initialize(tq, 10);

            var json = "{\"PrimaryKey\":5002,\"ForeignKey\":10,\"ReferenceId\":\"903988D3-B96D-430B-A34B-BB1F0DB7C9F7\",\"IsYes\":true,\"LuckyNumber\":7,\"DollarAmount\":100.00,\"MathCalculation\":6.785939020000000e-001,\"Label\":\"Poisonous\",\"RightNow\":\"2021-10-17T03:19:54.5433333\"}";

            //string key, string json, T entity
            InvokePrivateMethod(tm, "ProcessChangeRequest", json);

            Assert.Pass();
        }

        [Test]
        public void Can_process_queue_multiple_requests()
        {
            var partitionKey = $"{nameof(RudimentaryEntity)}";

            var tm = GetTableManager();

            var tq = new TableQuery
            {
                Schema = "dbo",
                Table = partitionKey
            };

            var chunkSize = 10;

            tm.Initialize(tq, chunkSize);

            var arr = GetRandomJsonRequests(chunkSize);

            for (var i = 0; i < arr.Length; i++)
            {
                var json = arr[i];

                InvokePrivateMethod(tm, "ProcessChangeRequest", json);
            }

            Assert.Pass();
        }
    }
}
