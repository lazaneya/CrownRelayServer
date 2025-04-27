using System.Net;
using System.Net.Sockets;
using System.Text;

namespace CrownRelayServer
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            RelayServer server = new RelayServer();
            await server.StartServer();
        }


    }
}
