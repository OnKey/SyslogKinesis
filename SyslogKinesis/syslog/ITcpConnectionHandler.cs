using System.Net.Sockets;
using System.Threading.Tasks;

namespace SyslogKinesis.syslog
{
    public interface ITcpConnectionHandler
    {
        /// <summary>
        /// Called each time a new connection is made to the TCP Listener
        /// </summary>
        /// <param name="client"></param>
        Task HandleAsync(TcpClient client);
    }
}
