using System.Collections.Generic;
using System.Threading.Tasks;
using Serilog;

namespace SyslogKinesis.kinesis
{
    /// <summary>
    /// Triggers publishing of items when a queue reaches a certain size or a timer expires
    /// </summary>
    public abstract class BatchedPeriodicPublisher : IEventPublisher
    {
        public abstract void Dispose();
        public abstract Task PublishEvents(IEnumerable<object> eventList);

        public int QueueSizePublishTrigger { get; set; } = 100;
        protected List<object> queue = new List<object>();
        private System.Timers.Timer timer;

        protected BatchedPeriodicPublisher(int publishInterval = 5000)
        {
            this.timer = new System.Timers.Timer(publishInterval);
            this.timer.Elapsed += async (sender, e) => await this.PublishQueue();
            this.timer.Enabled = true;
            this.timer.AutoReset = true;
        }

        public async Task QueueEvent(object item)
        {
            this.queue.Add(item);
            if (this.ShouldQueueByPublished())
            {
                await this.PublishQueue();
            }
        }

        private bool ShouldQueueByPublished()
        {
            return this.queue.Count >= this.QueueSizePublishTrigger;
        }

        private async Task PublishQueue()
        {
            if (this.queue.Count == 0)
            {
                Log.Verbose("Queue is empty, so not publishing events");
                return;
            }

            var savedQueue = this.queue;
            this.queue = new List<object>();
            Log.Information($"Publishing {savedQueue.Count} events");
            await this.PublishEvents(savedQueue);
        }
    }
}
