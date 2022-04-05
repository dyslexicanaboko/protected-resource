using System.Collections.Generic;
using Moq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using ProtectedResource.IntegrationTests;
using ProtectedResource.Lib;
using ProtectedResource.Lib.DataAccess;
using ProtectedResource.Lib.Models;
using ProtectedResource.Lib.Services;
using ProtectedResource.UnitTests.Dummy;

namespace ProtectedResource.UnitTests
{
    [TestFixture]
    public class TableManagerMergeTests
        : TableManagerTestBase
    {
        private const string DefaultJson = "{\"PrimaryKey\":5002,\"ForeignKey\":10,\"ReferenceId\":\"903988D3-B96D-430B-A34B-BB1F0DB7C9F7\",\"IsYes\":true,\"LuckyNumber\":7,\"DollarAmount\":100.00,\"MathCalculation\":6.785939020000000e-001,\"Label\":\"Poisonous\",\"RightNow\":\"2021-10-17T03:19:54.5433333\"}";

        private readonly Mock<IQueryToClassRepository> _mockRepo;
        private readonly Mock<ICachingService> _mockCachingService;
        private DummyCachingService _dummyCache;

        public TableManagerMergeTests()
        {
            var sq = new SchemaQuery
            {
                PrimaryKey = new SchemaColumn 
                { 
                    ColumnName = "PrimaryKey"
                },
                ColumnsAll = new List<SchemaColumn>
                {
                    new SchemaColumn { ColumnName = "PrimaryKey" },
                    new SchemaColumn { ColumnName = "ForeignKey" },
                    new SchemaColumn { ColumnName = "ReferenceId" },
                    new SchemaColumn { ColumnName = "IsYes" },
                    new SchemaColumn { ColumnName = "LuckyNumber" },
                    new SchemaColumn { ColumnName = "DollarAmount" },
                    new SchemaColumn { ColumnName = "MathCalculation" },
                    new SchemaColumn { ColumnName = "Label" },
                    new SchemaColumn { ColumnName = "RightNow" }
                }
            };

            _mockRepo = new Mock<IQueryToClassRepository>();
            _mockRepo
                .Setup(x => x.GetSchema(It.IsAny<TableQuery>(), It.IsAny<string>()))
                .Returns(sq);
            _mockRepo
                .Setup(x => x.GetJson(It.IsAny<string>(), It.IsAny<SchemaColumn>(), It.IsAny<object>()))
                .Returns(DefaultJson);

            _mockCachingService = new Mock<ICachingService>();
            _mockCachingService
                .Setup(x => x.HashGet(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(new CachedValue());
        }

        private TableManager<RudimentaryEntity> GetTableManager()
        {
            var config = new DummyConfigurationService();

            //This cannot be a singleton I don't think, but should only be instantiated once per resource
            //Not sure how I am going to deal with this yet
            var queue = new MessagingQueueService(config);
            
            _dummyCache = new DummyCachingService();
            _dummyCache.Initialize();

            var tm = new TableManager<RudimentaryEntity>(_mockRepo.Object, _dummyCache, queue, config);

            return tm;
        }
        
        [Test]
        public void Three_requests_are_merged_successfully()
        {
            //Arrange
            var expected = "{\"PrimaryKey\":5002,\"ForeignKey\":77,\"IsYes\":false,\"LuckyNumber\":88,\"ReferenceId\":\"903988D3-B96D-430B-A34B-BB1F0DB7C9F7\",\"DollarAmount\":100.00,\"MathCalculation\":6.785939020000000e-001,\"Label\":\"Poisonous\",\"RightNow\":\"2021-10-17T03:19:54.5433333\"}";

            var partitionKey = "5002";

            var tm = GetTableManager();

            var tq = new TableQuery
            {
                Schema = "dbo",
                Table = nameof(RudimentaryEntity)
            };

            var chunkSize = 3;

            tm.Initialize(tq, chunkSize);

            //Might want to consider putting the test json into files
            //PK must be part of every incoming request
            var arr = new[] { 
                "{\"PrimaryKey\":5002,\"ForeignKey\":77}",
                "{\"PrimaryKey\":5002,\"IsYes\":false}",
                "{\"PrimaryKey\":5002,\"LuckyNumber\":88}"
            };

            //Act
            for (var i = 0; i < arr.Length; i++)
            {
                var json = arr[i];

                InvokePrivateMethod(tm, "ProcessChangeRequest", json);
            }

            var actual = _dummyCache.Cache[partitionKey];

            //Assert
            AssertAreEqual(expected, actual);
        }

        [Test]
        public void Json_properties_are_synchronized_properly()
        {
            //Arrange
            var target = JObject.Parse("{\"One\":1,\"Two\":2,\"Three\":3,\"Four\":4}");
            var template = JObject.Parse("{\"One\":1}");
            var expected = new JObject(template);

            var tm = GetTableManager();

            //Act
            var actual = InvokePrivateMethod<TableManager<RudimentaryEntity>, JObject>(tm, "SynchronizeProperties", template, target);

            //Assert
            AssertAreEqual(expected, actual);
        }

        //representative is last into the Queue
        [TestCase("{\"One\":1}", "{\"Two\":2,\"Three\":3,\"Four\":4}", "{\"One\":1,\"Two\":2,\"Three\":3,\"Four\":4}")]
        [TestCase("{\"One\":\"A\",\"Two\":2}", "{\"One\":\"B\",\"Two\":2}", "{\"One\":\"A\", \"Two\":2}")]
        [TestCase("{\"One\":\"A\",\"Two\":2,\"Five\":5}", "{\"One\":\"B\",\"Two\":3,\"Three\":3,\"Four\":4}", "{\"One\":\"A\",\"Two\":2,\"Three\":3,\"Four\":4,\"Five\":5}")]
        public void Json_properties_are_merged_properly(string representative, string otherChanges, string expected)
        {
            //Arrange
            var jRepresentative = JObject.Parse(representative);
            var jOtherChanges = JObject.Parse(otherChanges);
            var jExpected = JObject.Parse(expected);

            var tm = GetTableManager();

            //Act
            var actual = InvokePrivateMethod<TableManager<RudimentaryEntity>, JObject>(tm, "SquashChanges", jRepresentative, jOtherChanges);

            //Assert
            AssertAreEqual(jExpected, actual);
        }
    }
}

