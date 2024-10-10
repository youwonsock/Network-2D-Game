using Google.Protobuf.Protocol;
using ServerCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Server.Game
{
	public struct Pos
	{
		public Pos(int y, int x) { Y = y; X = x; }
		public int Y;
		public int X;

        public static bool operator ==(Pos lhs, Pos rhs)
        {
            return lhs.Y == rhs.Y && lhs.X == rhs.X;
        }

        public static bool operator !=(Pos lhs, Pos rhs)
        {
            return lhs.Y != rhs.Y || lhs.X != rhs.X;
        }

		public override bool Equals(object obj)
		{
            return this == (Pos)obj;
        }

        public override int GetHashCode()
		{
			long v = (Y << 32) | X;
			return v.GetHashCode();
		}

		public override string ToString()
		{
			return base.ToString();
		}
    }

	public struct PQNode : IComparable<PQNode>
	{
		public int F;
		public int G;
		public int Y;
		public int X;

		public int CompareTo(PQNode other)
		{
			if (F == other.F)
				return 0;
			return F < other.F ? 1 : -1;
		}
	}

	public struct Vector2Int
	{
		public int x;
		public int y;

		public Vector2Int(int x, int y) { this.x = x; this.y = y; }

		public static Vector2Int up { get { return new Vector2Int(0, 1); } }
		public static Vector2Int down { get { return new Vector2Int(0, -1); } }
		public static Vector2Int left { get { return new Vector2Int(-1, 0); } }
		public static Vector2Int right { get { return new Vector2Int(1, 0); } }

		public static Vector2Int operator+(Vector2Int a, Vector2Int b)
		{
			return new Vector2Int(a.x + b.x, a.y + b.y);
		}

		public static Vector2Int operator -(Vector2Int a, Vector2Int b)
		{
			return new Vector2Int(a.x - b.x, a.y - b.y);
		}

		public float magnitude { get { return (float)Math.Sqrt(sqrMagnitude); } }
		public int sqrMagnitude { get { return (x * x + y * y); } }
		public int cellDistFromZero { get { return Math.Abs(x) + Math.Abs(y); } }
	}

	public class Map
	{
		public int MinX { get; set; }
		public int MaxX { get; set; }
		public int MinY { get; set; }
		public int MaxY { get; set; }

		public int SizeX { get { return MaxX - MinX + 1; } }
		public int SizeY { get { return MaxY - MinY + 1; } }

		bool[,] _collision;
		GameObject[,] _objects;



		public bool CanGo(Vector2Int cellPos, bool checkObjects = true)
		{
			if (cellPos.x < MinX || cellPos.x > MaxX)
				return false;
			if (cellPos.y < MinY || cellPos.y > MaxY)
				return false;

			int x = cellPos.x - MinX;
			int y = MaxY - cellPos.y;
			return !_collision[y, x] && (!checkObjects || _objects[y, x] == null);
		}

		public GameObject Find(Vector2Int cellPos)
		{
			if (cellPos.x < MinX || cellPos.x > MaxX)
				return null;
			if (cellPos.y < MinY || cellPos.y > MaxY)
				return null;

			int x = cellPos.x - MinX;
			int y = MaxY - cellPos.y;
			return _objects[y, x];
		}

		public bool ApplyLeave(GameObject gameObject)
		{
			if (gameObject.Room == null)
				return false;
			if (gameObject.Room.Map != this)
				return false;

			PositionInfo posInfo = gameObject.PosInfo;
			if (posInfo.PosX < MinX || posInfo.PosX > MaxX)
				return false;
			if (posInfo.PosY < MinY || posInfo.PosY > MaxY)
				return false;

			Zone zone = gameObject.Room.GetZone(gameObject.CellPos);
			zone.Remove(gameObject);

            {
				int x = posInfo.PosX - MinX;
				int y = MaxY - posInfo.PosY;
				if (_objects[y, x] == gameObject)
					_objects[y, x] = null;
			}

			return true;
		}

		public bool ApplyMove(GameObject gameObject, Vector2Int dest, bool checkObjects = true, bool collision = true)
		{
            if (gameObject.Room == null)
				return false;
			if (gameObject.Room.Map != this)
				return false;

            // 새로운 위치로 이동 가능한지 확인
            PositionInfo posInfo = gameObject.PosInfo;
			if (CanGo(dest, checkObjects) == false)
				return false;

			// 이동 가능하면 이동
			if (collision)
			{
				{
					int x = posInfo.PosX - MinX;
					int y = MaxY - posInfo.PosY;
					if (_objects[y, x] == gameObject)
						_objects[y, x] = null;
				}
				{
					int x = dest.x - MinX;
					int y = MaxY - dest.y;
					_objects[y, x] = gameObject;
				}
			}

			GameObjectType type = ObjectManager.GetObjectTypeById(gameObject.Id);
            if (type == GameObjectType.Player)
			{
                Player player = gameObject as Player;
                if (player != null)
                {
                    Zone now = gameObject.Room.GetZone(player.CellPos);
                    Zone next = gameObject.Room.GetZone(dest);

                    if (now != next)
                    {
						now.Players.Remove(player);
						next.Players.Add(player);
                    }
                }
            }
			else if (type == GameObjectType.Monster)
			{
                Monster monster = gameObject as Monster;
                if (monster != null)
                {
                    Zone now = gameObject.Room.GetZone(monster.CellPos);
                    Zone next = gameObject.Room.GetZone(dest);

                    if (now != next)
                    {
                        now.Monsters.Remove(monster);
                        next.Monsters.Add(monster);
                    }
                }
            }
			else if (type == GameObjectType.Projectile)
			{
                Projectile projectile = gameObject as Projectile;
                if (projectile != null)
                {
                    Zone now = gameObject.Room.GetZone(projectile.CellPos);
                    Zone next = gameObject.Room.GetZone(dest);

                    if (now != next)
                    {
                        now.Projectiles.Remove(projectile);
                        next.Projectiles.Add(projectile);
                    }
                }
            }

            // 실제 좌표 이동
            posInfo.PosX = dest.x;
			posInfo.PosY = dest.y;
			return true;
		}

		public void LoadMap(int mapId, string pathPrefix = "../../../../../Common/MapData")
		{
			string mapName = "Map_" + mapId.ToString("000");

			// Collision 관련 파일
			string text = File.ReadAllText($"{pathPrefix}/{mapName}.txt");
			StringReader reader = new StringReader(text);

			MinX = int.Parse(reader.ReadLine());
			MaxX = int.Parse(reader.ReadLine());
			MinY = int.Parse(reader.ReadLine());
			MaxY = int.Parse(reader.ReadLine());

			int xCount = MaxX - MinX + 1;
			int yCount = MaxY - MinY + 1;
			_collision = new bool[yCount, xCount];
			_objects = new GameObject[yCount, xCount];

			for (int y = 0; y < yCount; y++)
			{
				string line = reader.ReadLine();
				for (int x = 0; x < xCount; x++)
				{
					_collision[y, x] = (line[x] == '1' ? true : false);
				}
			}
		}

		#region A* PathFinding

		// U D L R
		int[] _deltaY = new int[] { 1, -1, 0, 0 };
		int[] _deltaX = new int[] { 0, 0, -1, 1 };
		int[] _cost = new int[] { 10, 10, 10, 10 };

		public List<Vector2Int> FindPath(Vector2Int startCellPos, Vector2Int destCellPos, bool checkObjects = true, int maxDist = 10)
		{
			List<Pos> path = new List<Pos>();

			HashSet<Pos> closeList = new HashSet<Pos>();
			Dictionary<Pos, int> openList = new Dictionary<Pos, int>();
			Dictionary<Pos, Pos> parent = new Dictionary<Pos, Pos>();

            PriorityQueue<PQNode> pq = new PriorityQueue<PQNode>();

			Pos pos = Cell2Pos(startCellPos);
			Pos dest = Cell2Pos(destCellPos);

			openList.Add(pos, 10 * (Math.Abs(dest.Y - pos.Y) + Math.Abs(dest.X - pos.X)));
			pq.Push(new PQNode() { F = 10 * (Math.Abs(dest.Y - pos.Y) + Math.Abs(dest.X - pos.X)), G = 0, Y = pos.Y, X = pos.X });
			parent.Add(pos, pos);

			while (pq.Count > 0)
			{
				PQNode pqNode = pq.Pop();
				Pos node = new Pos(pqNode.Y, pqNode.X);

                if (closeList.Contains(node))
					continue;

				closeList.Add(node);

				if (node.Y == dest.Y && node.X == dest.X)
					break;

				for (int i = 0; i < _deltaY.Length; i++)
				{
					Pos next = new Pos(node.Y + _deltaY[i], node.X + _deltaX[i]);

					if(Math.Abs(dest.Y - next.Y) + Math.Abs(dest.X - next.X) > maxDist)
                        continue;

                    if (next.Y != dest.Y || next.X != dest.X)
					{
						if (CanGo(Pos2Cell(next), checkObjects) == false) // CellPos
							continue;
					}

                    if (closeList.Contains(next))
                        continue;

                    int g = 0;// node.G + _cost[i];
					int h = 10 * ((dest.Y - next.Y) * (dest.Y - next.Y) + (dest.X - next.X) * (dest.X - next.X));

					int value = 0;
					if (!openList.TryGetValue(next, out value))
						value = Int32.MaxValue;

                    if (value < g + h)
						continue;

					if(!openList.TryAdd(next, g + h))
						openList[next] = g + h;

					pq.Push(new PQNode() { F = g + h, G = g, Y = next.Y, X = next.X });

					if (!parent.TryAdd(next, node))
						parent[next] = node;
				}
			}

			return CalcCellPathFromParent(parent, dest);
		}

		List<Vector2Int> CalcCellPathFromParent(Dictionary<Pos, Pos> parent, Pos dest)
		{
			List<Vector2Int> cells = new List<Vector2Int>();

			if (parent.ContainsKey(dest) == false)
			{
				Pos nearDest = new Pos();
				int nearDist = Int32.MaxValue;

                foreach (Pos p in parent.Keys)
				{
					int dist = Math.Abs(dest.Y - p.Y) + Math.Abs(dest.X - p.X);

					if(dist < nearDist)
					{
						nearDest = p;
                        nearDist = dist;
                    }
                }

				dest = nearDest;
            }

            Pos pos = dest;
			while (parent[pos] != pos)
			{
				cells.Add(Pos2Cell(pos));
                pos = parent[pos];
            }
			cells.Add(Pos2Cell(pos));
			cells.Reverse();

			return cells;
		}

		Pos Cell2Pos(Vector2Int cell)
		{
			return new Pos(MaxY - cell.y, cell.x - MinX);
		}

		Vector2Int Pos2Cell(Pos pos)
		{
			return new Vector2Int(pos.X + MinX, MaxY - pos.Y);
		}

		#endregion
	}

}
