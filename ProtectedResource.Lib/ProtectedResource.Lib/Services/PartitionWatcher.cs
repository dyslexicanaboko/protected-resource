using ProtectedResource.Lib.Events;
using System.Collections.Generic;
using System.Timers;

namespace ProtectedResource.Lib.Services
{
    /// <summary>
    /// Watches a partition queue to make sure it is being dequeued and not getting stale.
    /// The goal is to not process until a threshold is reached in the queue, but if the threshold
    /// is never reached a timer will force the queue to be processed.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PartitionWatcher<T>
    {
        public delegate void StaleQueueHandler(object sender, StaleQueueEventArgs<T> e);

        public event StaleQueueHandler WhenStale;

        private readonly Timer _timer;

        public PartitionWatcher(string partitionKey)
            : this(partitionKey, new Queue<ChangeRequest<T>>())
        {

        }

        public PartitionWatcher(string partitionKey, Queue<ChangeRequest<T>> queue)
        {
            PartitionKey = partitionKey;

            Queue = queue;

            _timer = new Timer();
            _timer.Interval = 10000; //TODO: Make timer interval configurable
            _timer.AutoReset = false;
            _timer.Elapsed += DequeueTimer_Elapsed;
        }

        private void DequeueTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            //This will raise an event to the TableManager to dequeue this partition
            WhenStale?.Invoke(sender, new StaleQueueEventArgs<T>(this));
        }

        public string PartitionKey { get; }

        public int Count => Queue.Count;

        public Queue<ChangeRequest<T>> Queue { get; }

        public void Enqueue(ChangeRequest<T> changeRequest) => Queue.Enqueue(changeRequest);
        
        public ChangeRequest<T> Dequeue() => Queue.Dequeue();

        public void Start()
        {
            //If the timer is already running then don't do it again
            if (_timer.Enabled) return;

            _timer.Start();
        }

        public void Stop() => _timer.Stop();
    }
}
