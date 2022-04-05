using Dapper;

namespace ProtectedResource.Lib.Models
{
    /// <summary>
    /// Dynamically generated SQL accompanied by labeled and loaded Dapper <see cref="DynamicParameters"/> object.
    /// Intention is to use this object together with a Dapper execution.
    /// </summary>
    public class SqlParamList
    {
        public string Sql { get; set; }

        public DynamicParameters Parameters { get; set; }
    }
}
