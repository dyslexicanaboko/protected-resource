using ProtectedResource.Lib.Services;
using System;

namespace ProtectedResource.Lib.Events
{
    public class StaleQueueEventArgs<T>
        : EventArgs
    {
        public StaleQueueEventArgs(PartitionWatcher<T> stalePartitionWatcher)
        {
            StalePartitionWatcher = stalePartitionWatcher;
        }

        public PartitionWatcher<T> StalePartitionWatcher { get; set; }
    }
}
