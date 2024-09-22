using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game
{
    public class PlayerManager
    {
        public static PlayerManager Instance { get; } = new PlayerManager();

        object lockObj = new object();
        Dictionary<int, Player> players = new Dictionary<int, Player>();
        
        // ???
        int playerID = 1;

        public Player Add()
        {
            Player newPlayer = new Player();

            lock (lockObj)
            {
                newPlayer.Info.PlayerId = playerID;
                players.Add(playerID++, newPlayer);
            }

            return newPlayer;
        }

        public bool Remove(int playerID)
        {
            lock (lockObj)
            {
                return players.Remove(playerID);
            }
        }

        public Player Find(int roomID)
        {
            lock (lockObj)
            {
                Player player = null;
                if (players.TryGetValue(roomID, out player))
                    return player;
            }

            return null;
        }
    }
}
