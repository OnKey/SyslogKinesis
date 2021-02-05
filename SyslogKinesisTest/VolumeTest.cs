using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Serilog;
using SyslogKinesis.kinesis;
using SyslogKinesis.syslog;
using SyslogNet.Client;
using SyslogNet.Client.Serialization;
using SyslogNet.Client.Transport;
using SyslogMessage = SyslogNet.Client.SyslogMessage;

namespace SyslogKinesisTest
{
    public class VolumeTest
    {
        private TestEventPublisher eventPublisher;

            [Test]
        public async Task Send1000TcpMessagesOctetCountingFormat()
        {
            this.StartSyslogServer();
            var sender = new SyslogTcpSender("localhost", 514);
            var serialiser = new SyslogRfc3164MessageSerializer();
            var msgs = new List<SyslogMessage>();
            for (var i = 0; i < 1000; i++)
            {
                msgs.Add(this.GetMsg(i));
            }

            sender.Send(msgs, serialiser);

            await Task.Delay(1000);

            Assert.AreEqual(1000, this.eventPublisher.items.Count);
        }

        [Test]
        public async Task Send1000TcpMessagesNonTransparentFramingFormat()
        {
            this.StartSyslogServer();
            var sender = new SyslogTcpSender("localhost", 514);
            sender.messageTransfer = MessageTransfer.NonTransparentFraming;
            var serialiser = new SyslogRfc3164MessageSerializer();
            var msgs = new List<SyslogMessage>();
            for (var i = 0; i < 1000; i++)
            {
                msgs.Add(this.GetMsg(i));
            }

            sender.Send(msgs, serialiser);

            await Task.Delay(1000);

            Assert.AreEqual(1000, this.eventPublisher.items.Count);
        }

        [Test]
        public async Task Send1000TcpMessagesNullTerminated()
        {
            this.StartSyslogServer();
            var sender = new SyslogTcpSender("localhost", 514);
            sender.messageTransfer = MessageTransfer.NonTransparentFraming;
            sender.trailer = 0x0;
            var serialiser = new SyslogRfc3164MessageSerializer();
            var msgs = new List<SyslogMessage>();
            for (var i = 0; i < 1000; i++)
            {
                msgs.Add(this.GetMsg(i));
            }

            sender.Send(msgs, serialiser);

            await Task.Delay(1000);

            Assert.AreEqual(1000, this.eventPublisher.items.Count);
        }

        private SyslogMessage GetMsg(int i)
        {
            return new SyslogMessage(DateTimeOffset.Now, Facility.LocalUse0, Severity.Error, "testhost", "testapp", i.ToString());
        }

        private void StartSyslogServer()
        {
            SyslogKinesis.Program.ConfigureLogging();
            Log.Information("Starting SyslogKinesis");

            this.eventPublisher = new TestEventPublisher();
            var handler = new TcpConnectionHandler(eventPublisher);
            var tcpListener = new TcpServer(handler, 514);
            _ = tcpListener.Run();

            var udpListener = new UdpServer(514, eventPublisher);
           _ = udpListener.Run();
        }
    }

    class TestEventPublisher : IEventPublisher
    {
        public List<object> items = new List<object>();
        public void Dispose()
        {
        }

        public Task PublishEvents(IEnumerable<object> eventList)
        {
            return Task.CompletedTask;
        }

        public Task QueueEvent(object item)
        {
            this.items.Add(item);
            return Task.CompletedTask;
        }
    }
}
