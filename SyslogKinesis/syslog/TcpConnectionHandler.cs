using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using Serilog;

namespace SyslogKinesis.syslog
{
    /// <summary>
    /// Reads a password off a TCP connection and calls password test method
    /// </summary>
    public class TcpConnectionHandler : ITcpConnectionHandler
    {
        private const int TcpTimeout = 900000; // 15 mins
        private TcpClient client;
        private ILogger syslogLogger;

        public TcpConnectionHandler(ILogger syslogLogger)
        {
            this.syslogLogger = syslogLogger;
        }

        public async Task HandleAsync(TcpClient client)
        {
            try
            {
                Log.Debug($"Received new TCP connection from {client.Client.RemoteEndPoint}");
                this.client = client;
                var netStream = client.GetStream();
                var reader = new StreamReader(netStream);
                var writer = new StreamWriter(netStream) {AutoFlush = true};

                await this.SyslogReceiveAsync(reader, writer);
            }
            catch (TimeoutException ex)
            {
                Log.Debug($"Timed out on connection from {client.Client.RemoteEndPoint}");
            }
            finally
            {
                client.Close();
            }
        }

        public async Task SyslogReceiveAsync(StreamReader reader, TextWriter writer)
        {
            while (true)
            {
                var line = await this.ReadAsync(reader);
                if (line == null)
                {
                    Log.Debug($"Connection closed from {client.Client.RemoteEndPoint}");
                    return;
                }
                
                var syslogMsg = new SyslogMessage(line);
                var ip = ((System.Net.IPEndPoint)this.client.Client.RemoteEndPoint).Address;
                this.syslogLogger.Information("{@SourceIp}: {@SyslogMessage}", ip.ToString(), syslogMsg);
            }
        }

        private async Task<string> ReadAsync(StreamReader reader)
        {
            var readTask = reader.ReadLineAsync();
            if (await Task.WhenAny(readTask, Task.Delay(TcpTimeout)) == readTask)
            {
                return readTask.Result;
            }

            throw new TimeoutException();
        }

        private async Task WriteAsync(TextWriter writer, string line)
        {
            var writeTask = writer.WriteLineAsync(line);
            if (await Task.WhenAny(writeTask, Task.Delay(TcpTimeout)) == writeTask)
            {
                return;
            }

            throw new TimeoutException();
        }
    }
}
