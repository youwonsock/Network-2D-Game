using Google.Protobuf;
using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Server.Game
{
	public partial class GameRoom : JobSerializer
    {
		public const int VisionCells = 13;

        public int RoomId { get; set; }
		public int ZoneCells { get; private set; }

        Dictionary<int, Player> playerDict = new Dictionary<int, Player>();
		Dictionary<int, Monster> monsterDict = new Dictionary<int, Monster>();
		Dictionary<int, Projectile> projectileDict = new Dictionary<int, Projectile>();

		public Zone[,] Zones { get; private set; }
        public Map Map { get; private set; } = new Map();


		public Zone GetZone(Vector2Int cellPos)
        {
            int x = (cellPos.x - Map.MinX) / ZoneCells;
            int y = (Map.MaxY - cellPos.y) / ZoneCells;

            return GetZone(y, x);
        }

		public Zone GetZone(int idxY, int idxX)
		{
            if (idxY < 0 || idxY >= Zones.GetLength(0))
                return null;
            if (idxX < 0 || idxX >= Zones.GetLength(1))
                return null;

            return Zones[idxY, idxX];
        }

        public void Init(int mapId, int zoneCells)
		{
			Map.LoadMap(mapId);

			ZoneCells = zoneCells;
			int countY = (Map.SizeY + zoneCells - 1) / zoneCells;
			int countX = (Map.SizeX + zoneCells - 1) / zoneCells;
            Zones = new Zone[countY, countX];

			for (int y = 0; y < countY; y++)
			{
				for (int x = 0; x < countX; x++)
				{
					Zones[y, x] = new Zone(y, x);
				}
			}

			// 테스트를 위한 기본 몬스터 1마리 생성
			for (int i = 0; i < 500; i++)
			{
				Monster monster = ObjectManager.Instance.Add<Monster>();
				monster.Init(1);
				EnterGame(monster, true);
			}
		}

		public void Update()
		{
			Flush();
		}

		public void EnterGame(GameObject gameObject, bool randPos)
		{
			if (gameObject == null)
				return;

			if (randPos)
            {
                Random rand = new Random();
                Vector2Int respawnPos;
				while (true)
				{
					respawnPos.x = rand.Next(Map.MinX, Map.MaxX + 1);
                    respawnPos.y = rand.Next(Map.MinY, Map.MaxY + 1);

                    if (Map.Find(respawnPos) == null)
                    {
                        gameObject.CellPos = respawnPos;
                        break;
                    }
                } 
			}

			GameObjectType type = ObjectManager.GetObjectTypeById(gameObject.Id);

			if (type == GameObjectType.Player)
			{
				Player player = gameObject as Player;
				playerDict.Add(gameObject.Id, player);
				player.Room = this;

				player.RefreashAdditionalStat();

                Map.ApplyMove(player, new Vector2Int(player.CellPos.x, player.CellPos.y));	// 초기 위치로 플레이어 생성

				GetZone(player.CellPos).Players.Add(player);

                {
                    // 입장한 플레이어에게 게임 입장 패킷 전송
                    S_EnterGame enterPacket = new S_EnterGame();
					enterPacket.Player = player.Info;
					player.Session.Send(enterPacket);

					player.Vision.Update();
				}
			}
			else if (type == GameObjectType.Monster)
			{
				Monster monster = gameObject as Monster;
				monsterDict.Add(gameObject.Id, monster);
				monster.Room = this;

				GetZone(monster.CellPos).Monsters.Add(monster);
                Map.ApplyMove(monster, new Vector2Int(monster.CellPos.x, monster.CellPos.y));

				monster.Update();
			}
			else if (type == GameObjectType.Projectile)
			{
				Projectile projectile = gameObject as Projectile;
				projectileDict.Add(gameObject.Id, projectile);
				projectile.Room = this;

                GetZone(projectile.CellPos).Projectiles.Add(projectile);
                projectile.Update();
            }
			
			// 다른 플레이어에게 정보 전송
			{
				S_Spawn spawnPacket = new S_Spawn();
				spawnPacket.Objects.Add(gameObject.Info);
				Broadcast(gameObject.CellPos, spawnPacket);
            }
        }

        public void LeaveGame(int objectId)
		{
			GameObjectType type = ObjectManager.GetObjectTypeById(objectId);
            Vector2Int cellPos;

			if (type == GameObjectType.Player)
			{
				Player player = null;
				if (playerDict.Remove(objectId, out player) == false)
					return;

				cellPos = player.CellPos;
				player.OnLeaveGame();

				// 맵에서 플레이어 제거
				Map.ApplyLeave(player);
				player.Room = null;

				{
					// 플레이어에게 퇴장 패킷 전송
					S_LeaveGame leavePacket = new S_LeaveGame();
					player.Session.Send(leavePacket);
				}
			}
			else if (type == GameObjectType.Monster)
			{
				Monster monster = null;
				if (monsterDict.Remove(objectId, out monster) == false)
					return;

				cellPos = monster.CellPos;
				Map.ApplyLeave(monster);
				monster.Room = null;
			}
			else if (type == GameObjectType.Projectile)
			{
				Projectile projectile = null;
				if (projectileDict.Remove(objectId, out projectile) == false)
					return;

				cellPos = projectile.CellPos;
				Map.ApplyLeave(projectile);
                projectile.Room = null;
			}
			else
				return;

            S_Despawn despawnPacket = new S_Despawn();
            despawnPacket.ObjectIds.Add(objectId);
            Broadcast(cellPos, despawnPacket);
	    }

        public Player FindPlayer(Func<GameObject, bool> condition)
		{
            // 조건에 만족하는 플레이어 찾기
            foreach (Player player in playerDict.Values)
			{
				if (condition.Invoke(player))
					return player;
			}

			return null;
		}

		public Player FindClosestPlayer(Vector2Int pos, int range)
        {
			List<Player> players = GetAdjacentPlayers(pos, range);

			players.Sort((a, b) =>
			{
				int leftDist = (a.CellPos - pos).cellDistFromZero;
                int rightDist = (b.CellPos - pos).cellDistFromZero;
				return leftDist - rightDist;
            });

            foreach (Player p in players)
            {
				List<Vector2Int> path = Map.FindPath(pos, p.CellPos, checkObjects: true);

				if(path.Count < 2 || path.Count > range)
					continue;

				return p;
            }

            return null;
        }

        public void Broadcast(Vector2Int pos, IMessage packet)
		{
			List<Zone> zones = GetAdjacentZones(pos);

			foreach (Player p in zones.SelectMany(z => z.Players))
            {
                int dx = p.CellPos.x - pos.x;
                int dy = p.CellPos.y - pos.y;

                if (Math.Abs(dx) > GameRoom.VisionCells || Math.Abs(dy) > GameRoom.VisionCells)
                    continue;

                p.Session.Send(packet);
			}
		}
		
		public List<Player> GetAdjacentPlayers(Vector2Int cellPos, int range)
        {
			List<Zone> zones = GetAdjacentZones(cellPos, range);
			return zones.SelectMany(z => z.Players).ToList();
        }

        public List<Zone> GetAdjacentZones(Vector2Int cellPos, int range = VisionCells)
		{
			HashSet<Zone> zones = new HashSet<Zone>();

			int maxY = cellPos.y + range;
            int minY = cellPos.y - range;
            int maxX = cellPos.x + range;
            int minX = cellPos.x - range;

			Vector2Int leftTop = new Vector2Int(minX, maxY);
			int minIndexX = (leftTop.x - Map.MinX) / ZoneCells;
            int minIndexY = (Map.MaxY - leftTop.y) / ZoneCells;

            Vector2Int rightBottom = new Vector2Int(maxX, minY);
            int maxIndexX = (rightBottom.x - Map.MinX) / ZoneCells;
            int maxIndexY = (Map.MaxY - rightBottom.y) / ZoneCells;

			for(int y = minIndexY; y <= maxIndexY; y++)
			{
                for (int x = minIndexX; x <= maxIndexX; x++)
				{
					Zone zone = GetZone(y, x);
                    if (zone != null)
                        zones.Add(zone);
                }
            }

			return zones.ToList();
        }
	}
}
