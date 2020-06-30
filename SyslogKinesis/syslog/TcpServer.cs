using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Serilog;


namespace SyslogKinesis.syslog
{
    public class TcpServer
    {
        private readonly TcpListener listener;
        private CancellationTokenSource cts;
        public ITcpConnectionHandler Handler { get; set; }

        public TcpServer(ITcpConnectionHandler handler, int port)
        {
            cts = new CancellationTokenSource();
            listener = new TcpListener(IPAddress.Any, port);
            this.Handler = handler;
            this.listener = new TcpListener(IPAddress.Any, port);
            this.listener.Start();
        }

        public async Task Run()
        {
            while (!cts.IsCancellationRequested)
            {
                try
                {
                    var client = await this.listener.AcceptTcpClientAsync();
                    Task.Run(() => this.Handler.HandleAsync(client));
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Error handling TCP client");
                }
            }
        }

        public void Stop()
        {
            this.cts.Cancel();
        }
    }
}
