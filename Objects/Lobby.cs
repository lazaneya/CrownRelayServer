

using System.Net;

namespace CrownRelayServer.Objects
{
    internal class Lobby
    {
        // Static props
        private static int MasterId = 0;

        // Props
        public int Id { get; private set; }
        public List<Player> Players { get; private set; } = new List<Player>();

        // Getters
        public Player Host
        {
            get { return Players[0]; }
        }

        public Lobby(Player host)
        {
            MasterId += 1;
            Id = MasterId;

            Players.Add(host);
        }

        public bool AddPlayer(Player player)
        {
            foreach (Player p in Players)
            {
                if (p.Name == player.Name) return false;
            }

            Players.Add(player);

            return true;
        }

        public void RemovePlayer(EndPoint playerEP)
        {
            foreach (Player player in Players)
            {
                if (player.EP == playerEP)
                {
                    Players.Remove(player);
                    return;
                }
            }
        }

        public IEnumerable<Player> GetAllClients() { return Players.Skip(1); }
    }
}
