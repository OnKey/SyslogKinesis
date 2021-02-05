using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyslogKinesis.syslog
{
    /// <summary>
    /// Syslog TCP protocol where message length is sent followed by a space at the start of a syslog message
    /// </summary>
    internal class OctetCounting
    {
        private TcpMessageBuffer tcpMessageBuffer;

        public OctetCounting(TcpMessageBuffer tcpMessageBuffer)
        {
            this.tcpMessageBuffer = tcpMessageBuffer;
        }

        internal async Task<bool> IsOctetCountingFormat()
        {
            return AsciiBytesToInt(new[] {await this.tcpMessageBuffer.GetByte(0)}) != 0;
        }

        internal async Task<string> ReadMessage()
        {
            await this.ReadMessageLengthFromStream();
            var messageLength = this.GetMessageLength();
            var headerLength = this.tcpMessageBuffer.BytesRead;

            await this.tcpMessageBuffer.ReadNextBytesInToBuffer(messageLength);
            var message = this.tcpMessageBuffer.GetBufferBytes().Skip(headerLength).ToArray();
            return Encoding.ASCII.GetString(message, 0, message.Length);
        }

        internal async Task ReadMessageLengthFromStream()
        {
            // format is length followed by space character
            await this.tcpMessageBuffer.ReadUntilDelimiterByte(new List<byte> { 0x20 });
        }

        internal int GetMessageLength()
        {
            var lengthBytes = this.tcpMessageBuffer.GetBufferBytes();
            return AsciiBytesToInt(lengthBytes.Take(lengthBytes.Length - 1).ToArray()); // skip space at end of length
        }

        private static int AsciiBytesToInt(byte[] bytes)
        {
            var asciiString = Encoding.ASCII.GetString(bytes, 0, bytes.Length);
            short.TryParse(asciiString, out var length);
            return length;
        }
    }
}