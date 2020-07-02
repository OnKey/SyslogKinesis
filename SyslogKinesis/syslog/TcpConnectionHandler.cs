using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using Serilog;
using SyslogKinesis.kinesis;

namespace SyslogKinesis.syslog
{
    /// <summary>
    /// Reads a password off a TCP connection and calls password test method
    /// </summary>
    public class TcpConnectionHandler : ITcpConnectionHandler
    {
        private const int TcpTimeout = 900000; // 15 mins
        private TcpClient client;
        private string RemoteIp;
        private IEventPublisher logger;

        public TcpConnectionHandler(IEventPublisher logger)
        {
            this.logger = logger;
        }

        public async Task HandleAsync(TcpClient client)
        {
            try
            {
                this.client = client;
                this.RemoteIp = ((System.Net.IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
                Log.Verbose($"Received new TCP connection from {this.RemoteIp}");
                
                var netStream = client.GetStream();
                var reader = new StreamReader(netStream);
                var writer = new StreamWriter(netStream) {AutoFlush = true};

                await this.SyslogReceiveAsync(reader, writer);
            }
            catch (TimeoutException ex)
            {
                Log.Debug($"Timed out on connection from {client.Client.RemoteEndPoint}");
            }
            catch (Exception ex)
            {
                Log.Warning("Something went wrong processing message {@ex}", ex);
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
                Log.Verbose($"Received: {line}");
                if (line == null)
                {
                    Log.Verbose($"Connection closed from {client.Client.RemoteEndPoint}");
                    return;
                }

                var syslogMsg = new SyslogMessage(line, this.RemoteIp);
                await this.logger.QueueEvent(syslogMsg);
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
