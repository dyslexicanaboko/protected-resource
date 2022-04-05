using ProtectedResource.Lib.Services;

namespace ProtectedResource.UnitTests.Dummy
{
    public class DummyConfigurationService
        : IConfigurationService
    {
        public string CachingUri => null;

        public int CacheExpirationSeconds => 3600;

        public string MessageQueueUri => null;

        public int PartitionWatcherMilliseconds => 10000;

        public string ConnectionString => null;
    }
}
