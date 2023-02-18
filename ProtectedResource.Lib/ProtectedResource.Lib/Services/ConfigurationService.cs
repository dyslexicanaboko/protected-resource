using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace ProtectedResource.Lib.Services
{
    public class ConfigurationService : IConfigurationService
    {
        private const string DefaultConfigName = "appsettings.json";
        private readonly IConfigurationRoot _config;

        public ConfigurationService()
        {
            _config = BuildConfigs();
        }

        private IConfigurationRoot BuildConfigs()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(DefaultConfigName, optional: false, reloadOnChange: true);

            var configuration = builder.Build();

            return configuration;
        }

        public string CachingUri => _config[nameof(CachingUri)];

        public int CacheExpirationSeconds => Convert.ToInt32(_config[nameof(CacheExpirationSeconds)]);

        public string MessageQueueUri => _config[nameof(MessageQueueUri)];

        public int PartitionWatcherMilliseconds => Convert.ToInt32(_config[nameof(PartitionWatcherMilliseconds)]);

        public string ConnectionString => _config.GetConnectionString("TargetDatabase");
    }
}
