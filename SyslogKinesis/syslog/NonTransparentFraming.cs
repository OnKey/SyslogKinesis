using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyslogKinesis.syslog
{
    /// <summary>
    /// Syslog TCP transport format where message end is delimited with a character, which could be LF 0xA, CRLF 0xD 0xA or NULL 0x0
    /// </summary>
    internal class NonTransparentFraming
    {
        private TcpMessageBuffer tcpMessageBuffer;
        private static byte[] CrLf = { 0xD, 0xA };

        public NonTransparentFraming(TcpMessageBuffer tcpMessageBuffer)
        {
            this.tcpMessageBuffer = tcpMessageBuffer;
        }

        internal async Task<bool> IsNonTransparentFramingFormat()
        {
            return await this.tcpMessageBuffer.GetByte(0) == 0x3C; // <
        }

        internal async Task<string> ReadMessage()
        {
            await this.tcpMessageBuffer.ReadUntilDelimiterByte(new List<byte> {0xA, 0x0});
            var message = this.tcpMessageBuffer.GetBufferBytes().ToArray();
            var terminatedWithCrlf = message.Skip(message.Length - 2).SequenceEqual(CrLf);
            var removeTrailingCharacters = terminatedWithCrlf ? 2 : 1;
            return Encoding.ASCII.GetString(message, 0, message.Length - removeTrailingCharacters);
        }
    }
}
