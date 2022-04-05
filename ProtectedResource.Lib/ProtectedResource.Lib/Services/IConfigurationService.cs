namespace ProtectedResource.Lib.Services
{
    public interface IConfigurationService
    {
        string CachingUri { get; }
        int CacheExpirationSeconds { get; }
        string MessageQueueUri { get; }
        int PartitionWatcherMilliseconds { get; }
    }
}