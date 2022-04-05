using System;
using ProtectedResource.Lib.Models;
using StackExchange.Redis;

namespace ProtectedResource.Lib.Services
{
    public class CachingService 
        : ICachingService
    {
        private ConnectionMultiplexer _cache;
        private IDatabase _cacheDatabase;

        public CachingService()
        {
            //TODO: Inject configuration
        }

        public void Initialize()
        {
            //TODO: Make endpoint configurable
            _cache = ConnectionMultiplexer.Connect(
                new ConfigurationOptions
                {
                    EndPoints = { "localhost:6379" }
                });

            _cacheDatabase = _cache.GetDatabase();
        }

        /* Example input
         * key: DatabaseName_dbo_TableName
         * hashField: PrimaryKey */
        public CachedValue HashGet(string key, string hashField)
        {
            var resource = _cacheDatabase.HashGet(key, hashField);

            var cv = new CachedValue {IsNull = resource.IsNull};

            if (cv.IsNull) return cv;

            cv.Value = resource.ToString();

            return cv;
        }

        public void HashSet(string key, string hashField, string value)
        {
            _cacheDatabase.HashSet(key, new[]
            {
                new HashEntry(hashField, value)
            });

            //TODO: Make expiration configurable
            var expiresOn = DateTime.UtcNow.AddHours(1);

            _cacheDatabase.KeyExpire(key, expiresOn);
        }

        public void Clear()
        {
            _cacheDatabase.Execute("flushdb");
        }
    }
}
