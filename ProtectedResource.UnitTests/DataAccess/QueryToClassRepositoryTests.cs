using Dapper;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using ProtectedResource.IntegrationTests;
using ProtectedResource.Lib.DataAccess;
using ProtectedResource.Lib.Models;
using ProtectedResource.Lib.Services;
using System;
using System.Collections.Generic;
using System.Data;

namespace ProtectedResource.UnitTests.DataAccess
{
    [TestFixture]
    public class QueryToClassRepositoryTests
        : TestBase
    {
        [Test]
        public void Update_statement_generates_changed_columns_only()
        {
            //Arrange
            var updateSqlTemplate = "UPDATE dbo.Table SET {0} WHERE PrimaryKey = @PrimaryKey";

            var paritionKey = "4";

            var changes = JObject.Parse($"{{\"One\":1,\"Two\":2,\"Three\":3,\"PrimaryKey\":{paritionKey}}}");

            var schema = new SchemaQuery
            {
                PrimaryKey = new SchemaColumn {ColumnName = "PrimaryKey", SqlType = "int", SystemType = typeof(int)},
                ColumnsNoPk = new List<SchemaColumn>
                {
                    new SchemaColumn {ColumnName = "One", SqlType = "int", SystemType = typeof(int)},
                    new SchemaColumn {ColumnName = "Two", SqlType = "int", SystemType = typeof(int)},
                    new SchemaColumn {ColumnName = "Three", SqlType = "int", SystemType = typeof(int)}
                }
            };

            var expected = new SqlParamList
            {
                Sql = "UPDATE dbo.Table SET One = @One," + Environment.NewLine +
                      "Two = @Two," + Environment.NewLine +
                      "Three = @Three WHERE PrimaryKey = @PrimaryKey",
                Parameters = new DynamicParameters()
            };

            expected.Parameters.Add("One", 1, DbType.Int32);
            expected.Parameters.Add("Two", 2, DbType.Int32);
            expected.Parameters.Add("Three", 3, DbType.Int32);
            expected.Parameters.Add("PrimaryKey", 4, DbType.Int32);

            //Act
            var reference = new QueryToClassRepository(new ConfigurationService());

            var actual = InvokePrivateMethod<QueryToClassRepository, SqlParamList>(
                reference, 
                "GetUpdateStatement", 
                updateSqlTemplate, paritionKey, schema, changes);

            //Assert
            Assert.AreEqual(expected.Sql, actual.Sql);

            //Not sure how to compare the DynamicParameters object for Assertion, so using the only list provided
            AssertAreEqual(expected.Parameters.ParameterNames, actual.Parameters.ParameterNames);
        }
    }
}
