using System.Net.Sockets;
using System.Net;
using System.Text;

namespace CrownRelayServer
{
    class RelayServer
    {

        Socket serverSocket;

        Int32 port = 10013;
        public RelayServer()
        {
            // init server socket
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        }
        
        public async Task StartServer()
        {
            // create an endpoint
            IPEndPoint server = new IPEndPoint(IPAddress.Any, port);

            // bind socket to endpoint
            serverSocket.Bind(server);

            // accept traffic from any endpoint
            EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

            // Buffer for incoming data
            byte[] buffer = new byte[1024];

            Console.WriteLine($"Created Socket at, {server.Address}:{server.Port}");

            while (true)
            {
                // Make server wait for a response
                SocketReceiveFromResult result = await serverSocket.ReceiveFromAsync(
                    new ArraySegment<byte>(buffer),
                    SocketFlags.None,
                    remoteEndPoint
                );

                // Convert received bytes to text
                string receivedText = Encoding.UTF8.GetString(buffer, 0, result.ReceivedBytes);
                Console.WriteLine($"Received from {result.RemoteEndPoint}: {receivedText}");
            }
        }
    }
}

