using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SyslogKinesis.kinesis
{
    public interface IEventPublisher : IDisposable
    {
        /// <summary>
        /// Publish a set of events
        /// </summary>
        /// <param name="eventList">list of events to publish</param>
        Task PublishEvents(IEnumerable<Object> eventList);

        /// <summary>
        /// Add an event to a queue to by published. The queue is published asynchronously
        /// when it reaches a threshold based on time or number of items
        /// </summary>
        /// <param name="item">An object to publish</param>
        Task QueueEvent(object item);
    }
}
