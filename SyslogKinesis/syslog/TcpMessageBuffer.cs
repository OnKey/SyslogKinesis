using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SyslogKinesis.syslog
{
    /// <summary>
    /// Holds a syslog message while it is being read from a TCP stream
    /// </summary>
    internal class TcpMessageBuffer
    {
        private byte[] readBuffer = new byte[100];
        internal int BytesRead;
        private Stream stream;

        internal TcpMessageBuffer(Stream stream)
        {
            this.stream = stream;
        }

        internal async Task ReadNextBytesInToBuffer(int length)
        {
            if (this.BytesRead + length > this.readBuffer.Length)
            {
                Array.Resize(ref this.readBuffer, this.BytesRead + length);
            }

            await this.stream.ReadAsync(this.readBuffer, this.BytesRead, length);
            this.BytesRead += length;
        }

        internal async Task<byte> GetByte(int i)
        {
            if (this.BytesRead < i + 1)
            {
                await this.ReadNextBytesInToBuffer(i + 1 - this.BytesRead);
            }

            return this.readBuffer[i];
        }

        internal async Task ReadUntilDelimiterByte(List<byte> delimiters)
        {
            var currentByte = this.BytesRead == 0 ? (byte)0 : this.readBuffer[this.BytesRead - 1];
            while (!delimiters.Contains(currentByte))
            {
                await this.ReadNextBytesInToBuffer(1);
                currentByte = this.readBuffer[this.BytesRead - 1];
            }
        }

        internal byte[] GetBufferBytes()
        {
            return this.readBuffer.Take(this.BytesRead).ToArray();
        }
    }
}
