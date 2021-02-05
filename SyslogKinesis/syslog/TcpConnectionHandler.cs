﻿using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using Amazon.Runtime.Internal;
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
        private string RemoteIp;
        private IEventPublisher logger;
        private NetworkStream stream;

        public TcpConnectionHandler(IEventPublisher logger)
        {
            this.logger = logger;
        }

        public async Task HandleAsync(TcpClient client)
        {
            try
            {
                this.RemoteIp = ((System.Net.IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
                Log.Verbose($"Received new TCP connection from {this.RemoteIp}");
                
                this.stream = client.GetStream();

                await this.SyslogReceiveAsync();
            }
            catch (TimeoutException)
            {
                Log.Debug($"Timed out on connection from {this.RemoteIp}");
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

        public async Task SyslogReceiveAsync()
        {
            while (true)
            {
                var line = await this.ReadAsync();
                Log.Verbose($"Received: {line}");
                if (line == null)
                {
                    Log.Verbose($"Connection closed from {this.RemoteIp}");
                    return;
                }

                var syslogMsg = new SyslogMessage(line, this.RemoteIp);
                await this.logger.QueueEvent(syslogMsg);
            }
        }

        private async Task<string> ReadAsync()
        {
            var buffer = new TcpMessageBuffer(this.stream);
            var octetMessage = new OctetCounting(buffer);
            if (await octetMessage.IsOctetCountingFormat())
            {
                return await octetMessage.ReadMessage();
            } 

            var ntfMessage = new NonTransparentFraming(buffer);
            if (await ntfMessage.IsNonTransparentFramingFormat())
            {
                return await ntfMessage.ReadMessage();
            }

            Log.Warning("Invalid message format. TCP syslog, first char: " + buffer.GetByte(0));
            return null;
        }
    }
}
