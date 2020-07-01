using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using SyslogKinesis.kinesis;

namespace SyslogKinesisTest
{
    public class BatchedPeriodicPublisherTest
    {
        [Test]
        public void ShouldNotPubishWithLessThan100Events()
        {
            var publisher = new TestPublisher(5000);
            for (var i = 0; i < 99; i++)
            {
                publisher.QueueEvent(item: new object()).Wait();
            }

            Assert.AreEqual(0, publisher.PublishedCount);
        }

        [Test]
        public void ShouldPubishAt100Events()
        {
            var publisher = new TestPublisher(5000);
            for (var i = 0; i < 100; i++)
            {
                publisher.QueueEvent(item: new object()).Wait();
            }

            Assert.AreEqual(1, publisher.PublishedCount);
        }

        [Test]
        public void ShouldNotPubishBeforeTimer()
        {
            var publisher = new TestPublisher(100);
            publisher.QueueEvent(item: new object()).Wait();
            Thread.Sleep(90);

            Assert.AreEqual(0, publisher.PublishedCount);
        }

        [Test]
        public void ShouldPubishAtTimer()
        {
            var publisher = new TestPublisher(100);
            publisher.QueueEvent(item: new object()).Wait();
            Thread.Sleep(110);

            Assert.AreEqual(1, publisher.PublishedCount);
        }
    }

    class TestPublisher : BatchedPeriodicPublisher
    {
        public int PublishedCount = 0;

        public TestPublisher(int publishInterval) : base(publishInterval)
        {
        }

        public override void Dispose()
        {
        }

        public override Task PublishEvents(IEnumerable<object> eventList)
        {
            this.PublishedCount++;
            return Task.CompletedTask;
        }
    }

}
