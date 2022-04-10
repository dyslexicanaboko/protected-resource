using ProtectedResource.Lib.Models;
using ProtectedResource.Lib.Services;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace ProtectedResource.Lib.DataAccess
{
    /// <summary>
    ///     The base Data Access Layer
    ///     All Data Access Layers should inherit from this base class
    /// </summary>
    public abstract class BaseRepository
    {
        protected string ConnectionString;

        protected BaseRepository(IConfigurationService config)
        {
            ConnectionString = config.ConnectionString;
        }

        protected SchemaRaw GetFullSchemaInformation(string sql)
        {
            using (var con = new SqlConnection(ConnectionString))
            {
                using (var cmd = new SqlCommand(sql, con))
                {
                    con.Open();

                    var rs = new SchemaRaw();

                    using (var dr = cmd.ExecuteReader())
                    {
                        var tblSqlServer = dr.GetSchemaTable();

                        var tblGeneric = new DataTable();
                        tblGeneric.Load(dr);
    
                        rs.SqlServerSchema = tblSqlServer;
                    }

                    using (var da = new SqlDataAdapter(cmd))
                    {
                        var tblGeneric = new DataTable();
                        
                        da.FillSchema(tblGeneric, SchemaType.Source);

                        rs.GenericSchema = tblGeneric;
                    }

                    return rs;
                }
            }
        }

        protected IDataReader ExecuteStoredProcedure(string storedProcedure, params SqlParameter[] parameters)
        {
            var con = new SqlConnection(ConnectionString);

            con.Open();

            var cmd = new SqlCommand(storedProcedure, con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandTimeout = 0;

            if (parameters.Any()) cmd.Parameters.AddRange(parameters);

            var reader = cmd.ExecuteReader(CommandBehavior.CloseConnection);

            return reader;
        }

        protected object ExecuteScalar(string sql)
        {
            using (var con = new SqlConnection(ConnectionString))
            {
                con.Open();

                using (var cmd = new SqlCommand(sql, con))
                {
                    cmd.CommandTimeout = 0;

                    return cmd.ExecuteScalar();
                }
            }
        }
    }
}