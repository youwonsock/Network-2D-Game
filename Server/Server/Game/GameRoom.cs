using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game
{
    public class GameRoom
    {
        object lockObj = new object();
        public int RoomID { get; set; }

        List<Player> players = new List<Player>();

        public void EnterGame(Player newPlayer)
        {
            if(newPlayer == null)
                return;

            lock (lockObj)
            {
                players.Add(newPlayer);
                newPlayer.Room = this;

                // to self(newPlayer)
                {
                    S_EnterGame enterPacket = new S_EnterGame();
                    enterPacket.Player = newPlayer.Info;
                    newPlayer.Session.Send(enterPacket);

                    S_Spawn spawnPacket = new S_Spawn();
                    foreach (Player p in players)
                    {
                        if (p == newPlayer)
                            continue;

                        spawnPacket.Players.Add(p.Info);
                    }

                    newPlayer.Session.Send(spawnPacket);
                }

                // to others(except newPlayer)  
                {
                    S_Spawn spawnPacket = new S_Spawn();
                    spawnPacket.Players.Add(newPlayer.Info);
                    foreach (Player p in players)
                    {
                        if (p == newPlayer)
                            continue;

                        p.Session.Send(spawnPacket);
                    }
                }
            }
        }

        public void LeaveGame(int playerID)
        {
            lock (lockObj)
            {
                Player player = players.Find(p => p.Info.PlayerId == playerID);
                if (player == null)
                    return;

                players.Remove(player);
                player.Room = null;

                // to self(player)
                {
                    S_LeaveGame leavePacket = new S_LeaveGame();
                    player.Session.Send(leavePacket);
                }

                // to others
                S_Despawn despawnPacket = new S_Despawn();
                despawnPacket.PlayerIds.Add(player.Info.PlayerId);
                foreach (Player p in players)
                {
                    p.Session.Send(despawnPacket);
                }
            }
        }
    }
}
