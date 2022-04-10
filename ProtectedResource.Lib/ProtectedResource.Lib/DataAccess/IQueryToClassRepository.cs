using Newtonsoft.Json.Linq;
using ProtectedResource.Lib.Models;

namespace ProtectedResource.Lib.DataAccess
{
    public interface IQueryToClassRepository
    {
        SchemaQuery GetSchema(TableQuery tableQuery, string query);

        string GetJson(string selectQuery, SchemaColumn primaryKey, object value);
        
        void UpdatePartition(string updateSqlTemplate, string partitionKey, SchemaQuery schema, JObject changes);
    }
}