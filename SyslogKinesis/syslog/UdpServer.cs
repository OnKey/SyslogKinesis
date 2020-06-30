using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace SyslogKinesis.syslog
{
    class UdpServer
    {
        private CancellationTokenSource cts;
        private int port;
        private ILogger syslogLogger;

        public UdpServer(int port, ILogger syslogLogger)
        {
            cts = new CancellationTokenSource();
            this.port = port;
            this.syslogLogger = syslogLogger;
        }
        
        public async Task Run()
        {
            try
            {
                using var udpClient = new UdpClient(this.port);
                while (!this.cts.IsCancellationRequested)
                {
                    var packet = await udpClient.ReceiveAsync();
                    Task.Run(() => this.HandleAsync(packet));
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error handling UDP client");
            }
        }

        public async Task HandleAsync(UdpReceiveResult udpResult)
        {
            Log.Debug($"Received new UDP message from {udpResult.RemoteEndPoint}");
            var line = Encoding.ASCII.GetString(udpResult.Buffer);
            var syslogMsg = new SyslogMessage(line);
            var ip = udpResult.RemoteEndPoint.Address;
            this.syslogLogger.Information("{@SourceIp}: {@SyslogMessage}", ip.ToString(), syslogMsg);
        }

        public void Stop()
        {
            this.cts.Cancel();
        }
    }
}
