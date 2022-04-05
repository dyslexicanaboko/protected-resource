using Dapper;
using Newtonsoft.Json.Linq;
using ProtectedResource.Lib.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace ProtectedResource.Lib.DataAccess
{
    /// <summary>
    /// The Data Access layer for the Code Generator
    /// </summary>
    public class QueryToClassRepository
        : BaseRepository, IQueryToClassRepository
    {
        public SchemaQuery GetSchema(TableQuery tableQuery, string query)
        {
            var rs = GetFullSchemaInformation(query);

            var sq = new SchemaQuery();
            sq.Query = query;
            sq.TableQuery = tableQuery;
            sq.HasPrimaryKey = rs.GenericSchema.PrimaryKey.Any();

            sq.ColumnsAll = new List<SchemaColumn>(rs.GenericSchema.Columns.Count);

            foreach (DataColumn dc in rs.GenericSchema.Columns)
            {
                var sqlServerColumn = rs.SqlServerSchema.Select($"ColumnName = '{dc.ColumnName}'").Single();

                var sc = new SchemaColumn
                {
                    ColumnName = dc.ColumnName,
                    IsDbNullable = dc.AllowDBNull,
                    SystemType = dc.DataType,
                    SqlType = sqlServerColumn.Field<string>("DataTypeName").ToLower(), //Casing is inconsistent, need it consistent for lookups
                    Size = sqlServerColumn.Field<int>("ColumnSize"),
                    Precision = sqlServerColumn.Field<short>("NumericPrecision"),
                    Scale = sqlServerColumn.Field<short>("NumericScale")
                };

                sq.ColumnsAll.Add(sc);
            }

            if (!sq.HasPrimaryKey) return sq;
            
            //TODO: This is assuming a single column is the primary key which is a bad idea, but okay for now
            var pk = rs.GenericSchema.PrimaryKey.First();

            sq.PrimaryKey = sq
                .ColumnsAll
                .Single(x => x.ColumnName.Equals(pk.ColumnName, StringComparison.InvariantCultureIgnoreCase));

            sq.PrimaryKey.IsIdentity = pk.AutoIncrement;
            sq.PrimaryKey.IsPrimaryKey = true;

            sq.ColumnsNoPk = sq
                .ColumnsAll
                .Where(x => !x.ColumnName.Equals(pk.ColumnName, StringComparison.InvariantCultureIgnoreCase))
                .ToList();

            return sq;
        }

        public string GetJson(string selectQuery, SchemaColumn primaryKey, object value)
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                var p = new DynamicParameters();
                p.Add($"@{primaryKey.ColumnName}", value);

                var lst = connection.Query<string>(selectQuery, p).ToList();

                if (!lst.Any()) return null;

                var json = lst.Single();

                return json;
            }
        }

        // This assumes single column primary keys for right now
        public void UpdatePartition(string updateSqlTemplate, string partitionKey, SchemaQuery schema, JObject changes)
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction(IsolationLevel.Snapshot))
                {
                    var sqlParamList = GetUpdateStatement(updateSqlTemplate, partitionKey, schema, changes);

                    connection.Execute(sqlParamList.Sql, sqlParamList.Parameters, transaction);
                }
            }
        }

        private SqlParamList GetUpdateStatement(string updateSqlTemplate, string partitionKey, SchemaQuery schema, JObject changes)
        {
            var result = new SqlParamList();
            
            var properties = changes.Properties().ToArray();

            var parameters = new DynamicParameters();
            var lstUpdate = new List<string>(properties.Length);

            //Getting the parameters for the non-PK columns
            foreach (var property in properties)
            {
                var column = schema.ColumnsNoPk.SingleOrDefault(x => x.ColumnName.Equals(property.Name));

                if(column == null) continue;

                lstUpdate.Add($"{column.ColumnName} = @{column.ColumnName}");

                SetParameter(parameters, column, property);
            }

            //For the sake of simplicity converting to JProperty instead of creating an overload
            var pk = new JProperty("P", partitionKey);

            //Adding the final parameter for the PrimaryKey/PartitionKey
            SetParameter(parameters, schema.PrimaryKey, pk);

            var strColumnList = string.Join($",{Environment.NewLine}", lstUpdate);

            result.Sql = string.Format(updateSqlTemplate, strColumnList);
            result.Parameters = parameters;

            return result;
        }

        private void SetParameter(DynamicParameters parameters, SchemaColumn column, JProperty property)
        {
            var t = SqlTypes[column.SqlType];

            if (!MapSqlDbTypeToDbTypeLoose.TryGetValue(t, out var dbType))
                throw new ApplicationException($"SqlDbType {t} does not have a known DbType mapping. 0x202203202029");

            var value = property.Value.ToObject(column.SystemType);
            
            //These Sql Type Properties will be set dynamically and based on the type
            byte? scale = null;
            byte? precision = null;
            int? size = null;

            //Adapted from: ~\simple-class-creator\SimpleClassCreator.Lib\Services\Generators\RepositoryDapperGenerator.cs
            //TODO: Need to work through every type to see what the combinations are
            switch (t)
            {
                case SqlDbType.DateTime2:
                    scale = (byte)column.Scale;
                    break;
                case SqlDbType.Decimal:
                    precision = (byte)column.Precision;
                    scale = (byte)column.Scale;
                    break;
                case SqlDbType.VarChar:
                case SqlDbType.NVarChar:
                case SqlDbType.Char:
                case SqlDbType.NChar:
                    size = column.Size;
                    break;
            }

            parameters.Add(
                column.ColumnName, 
                value, 
                dbType,
                scale: scale,
                precision: precision,
                size: size);
        }

        //Parking the Sql Type Mappings here temporarily, probably should not stay here.
        //Copied from: ~\simple-class-creator\SimpleClassCreator.Lib\Services\TypesService.cs
        #region Sql type mappings
        /// <summary>
        /// Loose mapping going from SQL Server database type to Database type. Does not account for all types!
        /// </summary>
        private static readonly Dictionary<SqlDbType, DbType> MapSqlDbTypeToDbTypeLoose = new Dictionary<SqlDbType, DbType>
        {
            { SqlDbType.BigInt, DbType.Int64 },
            //{ SqlDbType.Binary, ??? },
            { SqlDbType.Bit, DbType.Boolean },
            { SqlDbType.Char, DbType.AnsiStringFixedLength },
            { SqlDbType.Date, DbType.Date },
            { SqlDbType.DateTime, DbType.DateTime },
            { SqlDbType.DateTime2, DbType.DateTime2 },
            { SqlDbType.DateTimeOffset, DbType.DateTimeOffset },
            { SqlDbType.Decimal, DbType.Decimal },
            { SqlDbType.Float, DbType.Double },
            { SqlDbType.Int, DbType.Int32 },
            { SqlDbType.Money, DbType.Currency },
            { SqlDbType.NChar, DbType.StringFixedLength },
            { SqlDbType.NVarChar, DbType.String },
            { SqlDbType.Real, DbType.Single },
            { SqlDbType.SmallInt, DbType.Int16 },
            { SqlDbType.Time, DbType.Time },
            { SqlDbType.TinyInt, DbType.Byte },
            { SqlDbType.UniqueIdentifier, DbType.Guid },
            { SqlDbType.VarBinary, DbType.Binary },
            { SqlDbType.VarChar, DbType.AnsiString },
            { SqlDbType.Xml, DbType.Xml }
        };

        /// <summary>
        /// Strong mapping of Sql Server Database type lower case names to their equivalent Enumeration.
        /// </summary>
        private static readonly Dictionary<string, SqlDbType> SqlTypes = GetEnumDictionary<SqlDbType>(true);

        private static Dictionary<string, T> GetEnumDictionary<T>(bool? keyIsLowerCase = null)
            where T : struct, IConvertible
        {
            if (!typeof(T).IsEnum)
                throw new ArgumentException("T must be an enumerated type");

            var t = typeof(T);

            var names = Enum.GetNames(t);

            if (keyIsLowerCase.HasValue)
            {
                Func<string, string> f;

                if (keyIsLowerCase.Value)
                {
                    f = s => s.ToLower();
                }
                else
                {
                    f = s => s.ToUpper();
                }

                names = names.Select(x => f(x)).ToArray();
            }

            var values = (T[])Enum.GetValues(t);

            var dict = new Dictionary<string, T>(names.Length);

            for (var i = 0; i < names.Length; i++)
            {
                dict.Add(names[i], values[i]);
            }

            return dict;
        } 
        #endregion
    }
}
