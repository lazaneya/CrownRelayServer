

using System.Net;

namespace CrownRelayServer.Objects
{
    internal class Player
    {
        public EndPoint EP;
        public string Name;

        public Player(EndPoint ep, string name)
        {
            EP = ep;
            Name = name;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
