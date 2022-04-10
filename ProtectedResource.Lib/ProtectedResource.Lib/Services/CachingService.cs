using ProtectedResource.Lib.Models;
using StackExchange.Redis;
using System;

namespace ProtectedResource.Lib.Services
{
    public class CachingService 
        : ICachingService
    {
        private ConnectionMultiplexer _cache;
        private IDatabase _cacheDatabase;
        private readonly IConfigurationService _config;
        private readonly int _expirationSeconds;

        public CachingService(IConfigurationService config)
        {
            _config = config;
            
            _expirationSeconds = _config.CacheExpirationSeconds;
        }

        public void Initialize()
        {
            //Only initialize a single time
            if (_cache != null) return;

            _cache = ConnectionMultiplexer.Connect(
                new ConfigurationOptions
                {
                    EndPoints = { _config.CachingUri }
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

            var expiresOn = DateTime.UtcNow.AddSeconds(_expirationSeconds);

            _cacheDatabase.KeyExpire(key, expiresOn);
        }

        /// <summary>This should only be used for testing.</summary>
        public void Clear()
        {
            _cacheDatabase.Execute("flushdb");
        }

        public void Dispose()
        {
            _cache?.Dispose();
        }
    }
}
