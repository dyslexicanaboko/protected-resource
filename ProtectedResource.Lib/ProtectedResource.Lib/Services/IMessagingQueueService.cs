using System;

namespace ProtectedResource.Lib.Services
{
    public interface IMessagingQueueService
        : IDisposable
    {
        void Start(string queueName, MessagingQueueService.JsonQueueItem processQueueItem);
    }
}