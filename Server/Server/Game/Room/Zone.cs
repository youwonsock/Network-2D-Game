

using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;

namespace Server.Game
{
    public class Zone
    {
        public int IndexY { get; private set; }
        public int IndexX { get; private set; }

        public HashSet<Player> Players { get; private set; } = new HashSet<Player>();
        public HashSet<Monster> Monsters { get; private set; } = new HashSet<Monster>();
        public HashSet<Projectile> Projectiles { get; private set; } = new HashSet<Projectile>();



        public Zone(int y, int x)
        {
            IndexX = x;
            IndexY = y;
        }

        public void Remove(GameObject gameObject)
        {
            if (gameObject == null)
                return;

            GameObjectType type = ObjectManager.GetObjectTypeById(gameObject.Id);

            switch (type)
            {
                case GameObjectType.Player:
                    Players.Remove(gameObject as Player);
                    break;
                case GameObjectType.Monster:
                    Monsters.Remove(gameObject as Monster);
                    break;
                case GameObjectType.Projectile:
                    Projectiles.Remove(gameObject as Projectile);
                    break;
            }
        }

        public Player FindOnePlayer(Func<Player, bool> condition)
        {
            foreach (Player player in Players)
            {
                if (condition.Invoke(player))
                    return player;
            }

            return null;
        }

        public List<Player> FindAllPlayer(Func<Player, bool> condition)
        {
            List<Player> findList = new List<Player>();

            foreach (Player player in Players)
            {
                if (condition.Invoke(player))
                    findList.Add(player);
            }

            return findList;
        }
    }
}
