using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using SyslogKinesis.kinesis;

namespace SyslogKinesis.syslog
{
    internal class UdpServer
    {
        private CancellationTokenSource cts;
        private int port;
        private IEventPublisher logger;

        public UdpServer(int port, IEventPublisher logger)
        {
            cts = new CancellationTokenSource();
            this.port = port;
            this.logger = logger;
        }
        
        public async Task Run()
        {
            try
            {
                using var udpClient = new UdpClient(this.port);
                while (!this.cts.IsCancellationRequested)
                {
                    var packet = await udpClient.ReceiveAsync();
                    _ =  this.HandleAsync(packet);
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error handling UDP client");
            }
        }

        public async Task HandleAsync(UdpReceiveResult udpResult)
        {
            Log.Verbose($"Received new UDP message from {udpResult.RemoteEndPoint}");
            var line = Encoding.ASCII.GetString(udpResult.Buffer);

            var ip = udpResult.RemoteEndPoint.Address;
            var syslogMsg = new SyslogMessage(line, ip.ToString());
            await this.logger.QueueEvent(syslogMsg);
        }

        public void Stop()
        {
            this.cts.Cancel();
        }
    }
}
