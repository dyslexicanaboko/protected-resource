using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using ProtectedResource.Entity;
using ProtectedResource.Lib;
using ProtectedResource.Lib.DataAccess;
using ProtectedResource.Lib.Models;
using ProtectedResource.Lib.Services;
using System.Collections.Generic;
using FakeItEasy;

/* To get started with your own demo, refer to the ReadMe.md
 * Use this Fixture to try out the Table Manager for yourself. */
namespace ProtectedResource.IntegrationTests
{
    [TestFixture]
    public class DemoTests
        : TableManagerTestBase
    {
        private readonly IQueryToClassRepository _repo;
        private readonly ICachingService _cachingService;
        private readonly IConfigurationService _config;

        public DemoTests()
        {
            var realConfig = new ConfigurationService();

            _config = A.Fake<IConfigurationService>();

            //Using FakeItEasy as a proxy so that individual config values CAN be modified without having to update the appsettings.json
            //Default behavior will be like the real config from the appsettings.json
            A.CallTo(() => _config.CacheExpirationSeconds).Returns(realConfig.CacheExpirationSeconds);
            A.CallTo(() => _config.CachingUri).Returns(realConfig.CachingUri);
            A.CallTo(() => _config.ConnectionString).Returns(realConfig.ConnectionString);
            A.CallTo(() => _config.MessageQueueUri).Returns(realConfig.MessageQueueUri);
            A.CallTo(() => _config.PartitionWatcherMilliseconds).Returns(realConfig.PartitionWatcherMilliseconds);

            _repo = new QueryToClassRepository(_config);

            //Should live as part of a singleton instance
            _cachingService = new CachingService(_config);
            _cachingService.Initialize(); //Only call once
        }

        [SetUp]
        public void Setup()
        {
            _cachingService.Clear();
        }
        
        [Test]
        public void Demo()
        {
            //Change this value to whatever you want so you can debug freely
            A.CallTo(() => _config.PartitionWatcherMilliseconds).Returns(120000); //2 minutes

            var queue = new MessagingQueueService(_config);

            //Make sure to set TResource for the table manager
            var tm = new TableManager<IntegersEntity>(
                _repo,
                _cachingService,
                queue,
                _config,
                NullLogger<TableManager<IntegersEntity>>.Instance);

            //Make sure to update the Table name
            var tq = new TableQuery
            {
                Schema = "dbo",
                Table = "Integers"
            };
            
            //Create an array of data
            var arr = GetEntitiesAsJson(new List<IntegersEntity>
            {
                new() { IntegersId = 1, Number1 = 1, Number2 = 1, Number3 = 1 }, //0 - First
                new() { IntegersId = 1, Number1 = 1, Number2 = 1, Number3 = 2 }, //1
                new() { IntegersId = 1, Number1 = 1, Number2 = 1, Number3 = 3 }  //2 - Last -- Last in wins
            });

            //Setting the chunk size to equal the array size so that processing triggers after the last item is queued
            var chunkSize = arr.Length;

            tm.Initialize(tq, chunkSize);

            foreach (var json in arr)
            {
                InvokePrivateMethod(tm, "ProcessChangeRequest", json);
            }

            //Visually inspect the data to make sure it did what you expected
            Assert.Pass();
        }
    }
}
