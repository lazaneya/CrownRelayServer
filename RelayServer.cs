using System.Net.Sockets;
using System.Net;
using System.Text;
using System.ComponentModel.Design;
using CrownRelayServer.Objects;
using System;
using System.Threading.Tasks.Dataflow;

namespace CrownRelayServer
{
    class RelayServer
    {
        // UDP Port Props
        Socket serverSocket;
        int port = 10013;

        // Lobby Hosting
        Dictionary<int, Lobby> Lobbies = [];
        Dictionary<EndPoint, int> RegisteredPlayers = new Dictionary<EndPoint, int>();

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

                // Execute user request if valid
                string[] splitText = receivedText.Split("&");

                Console.WriteLine(splitText[0]);
                switch (splitText[0])
                {
                    case ("host"): // Create Lobby : host&{playerName}
                        CreateLobby(result.RemoteEndPoint, splitText);
                        break;
                    case ("join"): // Join Lobby : join&{lobbyId}&{playerName}
                        if (CallIsEmpty(splitText)) break;

                        await JoinLobby(result.RemoteEndPoint, splitText);
                        break;
                    case ("quit"): // Exit Lobby : quit
                        if (CallIsEmpty(splitText)) break;

                        QuitLobby(result.RemoteEndPoint);
                        break;
                    case ("rpc"): // RPC Call : rpc&{objType}&{methodName}&{args[]}
                        if (CallIsEmpty(splitText)) break;

                        Lobby? lobbyRpc = TryGetLobby(result.RemoteEndPoint);
                        if (lobbyRpc == null) break;

                        RpcAll(lobbyRpc, buffer);
                        buffer = new byte[buffer.Length];
                        break;
                    case ("rpcc"): // RPC Clients: rpcc&{objType}&{methodName}&{args[]}
                        if (CallIsEmpty(splitText)) break;

                        Lobby? lobbyRpcClient = TryGetLobby(result.RemoteEndPoint);
                        if (lobbyRpcClient == null) break;

                        RpcClients(lobbyRpcClient, buffer);
                        buffer = new byte[buffer.Length];
                        break;
                    case ("rpch"): // RPC Only Host/Server: rpch&{objType}&{methodName}&{args[]}
                        if (CallIsEmpty(splitText)) break;

                        Lobby? lobbyRpcHost = TryGetLobby(result.RemoteEndPoint);
                        if (lobbyRpcHost == null) break;

                        RpcHost(lobbyRpcHost, buffer);
                        buffer = new byte[buffer.Length];
                        break;
                    default: Console.WriteLine("Invalid Command"); break;
                }
            }
        }

        private bool CallIsEmpty(string[] splitText)
        {
            if (splitText.Length <= 1)
            {
                Console.WriteLine("Empty Call");
                return true;
            }
            return false;
        }

        public Lobby CreateLobby(EndPoint hostEndPoint, string[] command)
        {
            Player host = new Player(hostEndPoint, command[1]);

            Lobby newLobby = new Lobby(host);
            Lobbies.Add(newLobby.Id, newLobby);
            RegisteredPlayers[hostEndPoint] = newLobby.Id;

            SendLobbyInfo(newLobby);
            return newLobby;
        }

        public async Task<Lobby> JoinLobby(EndPoint playerEndPoint, string[] command)
        {
            // Find the lobby by Id
            int lobbyId = int.Parse(command[1]);
            Lobby openLobby = Lobbies[lobbyId];

            Player player = new Player(playerEndPoint, command[2]);
            bool success = openLobby.AddPlayer(player);

            if (!success) // Check for identical player name
            {
                Console.WriteLine("Error: Identical Name");

                await serverSocket.SendToAsync(Encoding.UTF8.GetBytes($"Lobby already contains player with name: {command[2]}, please choose a new name."), playerEndPoint);
                return openLobby;
            }

            // Register player with server and lobby
            RegisteredPlayers[playerEndPoint] = openLobby.Id;

            // Tell NewPlayer what player number they are.
            byte[] buffer = Encoding.UTF8.GetBytes($"rpc&RelayServer&0&SetPlayerNumber&{openLobby.Players.Count - 1}");
            await serverSocket.SendToAsync(buffer, playerEndPoint);

            // Alert all players that Newplayer has joined.
            SendLobbyInfo(openLobby);

            return openLobby;
        }

        public Lobby QuitLobby(EndPoint playerEndPoint)
        {
            // Get Lobby with player registered EndPoint
            int lobbyId = RegisteredPlayers[playerEndPoint];
            Lobby openLobby = Lobbies[lobbyId];

            // Remove player from the lobby
            openLobby.RemovePlayer(playerEndPoint);

            // Unregister player from server
            RegisteredPlayers.Remove(playerEndPoint);

            // Alert the quitting player
            byte[] buffer = Encoding.UTF8.GetBytes($"You have left the lobby");
            
            serverSocket.SendToAsync(buffer, playerEndPoint);

            return openLobby;
        }

        public Lobby? TryGetLobby(EndPoint playerEndPoint)
        {
            try
            {
                return Lobbies[RegisteredPlayers[playerEndPoint]];
            } 
            catch (Exception e)
            {
                Console.WriteLine($"Lobby not found: {e}");
                return null;
            }
        }

        public void RpcAll(Lobby lobby, byte[] rpcBuffer)
        {
            foreach(Player player in lobby.Players)
            {
                serverSocket.SendToAsync(rpcBuffer, player.EP);
            }
        }

        public void RpcClients(Lobby lobby, byte[] rpcBuffer)
        {
            foreach(Player player in lobby.GetAllClients())
            {
                serverSocket.SendToAsync(rpcBuffer, player.EP);
            }
        }

        public void RpcHost(Lobby lobby, byte[] rpcBuffer)
        {
            serverSocket.SendToAsync(rpcBuffer, lobby.Host.EP);
        }

        public void SendLobbyInfo(Lobby lobby)
        {
            byte[] buffer = Encoding.UTF8.GetBytes($"lobbyi&{lobby.Id}&{string.Join("&", lobby.Players)}");

            RpcAll(lobby, buffer);
        }
    }
}

