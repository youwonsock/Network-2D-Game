using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server.Data;
using System;
using System.Collections.Generic;

namespace Server.Game
{
	public class GameRoom : JobTimer
	{
		public int RoomId { get; set; }

		Dictionary<int, Player> playerDict = new Dictionary<int, Player>();
		Dictionary<int, Monster> monsterDict = new Dictionary<int, Monster>();
		Dictionary<int, Projectile> projectileDict = new Dictionary<int, Projectile>();

		public Map Map { get; private set; } = new Map();



		public void Init(int mapId)
		{
			Map.LoadMap(mapId);

			// 테스트를 위한 기본 몬스터 1마리 생성
			Monster monster = ObjectManager.Instance.Add<Monster>();
			monster.CellPos = new Vector2Int(5, 5);
			EnterGame(monster);
		}

		public void Update()
		{
			// 몬스터와 투사체 업데이트 처리
			foreach (Monster monster in monsterDict.Values)
			{
				monster.Update();
			}

			foreach (Projectile projectile in projectileDict.Values)
			{
				projectile.Update();
			}

			Flush();
		}

		public void EnterGame(GameObject gameObject)
		{
			if (gameObject == null)
				return;

			GameObjectType type = ObjectManager.GetObjectTypeById(gameObject.Id);

			if (type == GameObjectType.Player)
			{
				Player player = gameObject as Player;
				playerDict.Add(gameObject.Id, player);
				player.Room = this;

				Map.ApplyMove(player, new Vector2Int(player.CellPos.x, player.CellPos.y));	// 초기 위치로 플레이어 생성

				{
                    // 입장한 플레이어에게 게임 입장 패킷 전송
                    S_EnterGame enterPacket = new S_EnterGame();
					enterPacket.Player = player.Info;
					player.Session.Send(enterPacket);

                    // 입장한 플레이어에게 기존에 방에 존재하는 객체들의 정보 전송
                    S_Spawn spawnPacket = new S_Spawn();
					foreach (Player p in playerDict.Values)
					{
						if (player != p)
							spawnPacket.Objects.Add(p.Info);
					}

					foreach (Monster m in monsterDict.Values)
						spawnPacket.Objects.Add(m.Info);

					foreach (Projectile p in projectileDict.Values)
						spawnPacket.Objects.Add(p.Info);

					player.Session.Send(spawnPacket);
				}
			}
			else if (type == GameObjectType.Monster)
			{
				Monster monster = gameObject as Monster;
				monsterDict.Add(gameObject.Id, monster);
				monster.Room = this;

				Map.ApplyMove(monster, new Vector2Int(monster.CellPos.x, monster.CellPos.y));
			}
			else if (type == GameObjectType.Projectile)
			{
				Projectile projectile = gameObject as Projectile;
				projectileDict.Add(gameObject.Id, projectile);
				projectile.Room = this;
			}
			
			// 다른 플레이어에게 정보 전송
			{
				S_Spawn spawnPacket = new S_Spawn();
				spawnPacket.Objects.Add(gameObject.Info);
				foreach (Player p in playerDict.Values)
				{
					if (p.Id != gameObject.Id)
						p.Session.Send(spawnPacket);
				}
			}

            // test log
            Console.WriteLine($"EnterGame: {gameObject.Id}");
        }

        public void LeaveGame(int objectId)
		{
			GameObjectType type = ObjectManager.GetObjectTypeById(objectId);

			if (type == GameObjectType.Player)
			{
				Player player = null;
				if (playerDict.Remove(objectId, out player) == false)
					return;

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

				Map.ApplyLeave(monster);
				monster.Room = null;
			}
			else if (type == GameObjectType.Projectile)
			{
				Projectile projectile = null;
				if (projectileDict.Remove(objectId, out projectile) == false)
					return;

				projectile.Room = null;
			}

            // 다른 플레이어에게 despawn 패킷 전송
            {
                S_Despawn despawnPacket = new S_Despawn();
				despawnPacket.ObjectIds.Add(objectId);
				foreach (Player p in playerDict.Values)
				{
					if (p.Id != objectId)
						p.Session.Send(despawnPacket);
				}
			}

            // test log
            Console.WriteLine($"LeaveGame: {objectId}");
	    }

		public void HandleMove(Player player, C_Move movePacket)
		{
			if (player == null)
				return;

			PositionInfo movePosInfo = movePacket.PosInfo;
			ObjectInfo info = player.Info;

            // 플레이어의 현제 위치와	이동하려는 위치가 다르면
            if (movePosInfo.PosX != info.PosInfo.PosX || movePosInfo.PosY != info.PosInfo.PosY)
			{
                // 이동하려는 위치가 갈 수 없는 곳이면
                if (Map.CanGo(new Vector2Int(movePosInfo.PosX, movePosInfo.PosY)) == false)
					return;
			}

			// 이동처리
			info.PosInfo.State = movePosInfo.State;
			info.PosInfo.MoveDir = movePosInfo.MoveDir;
			Map.ApplyMove(player, new Vector2Int(movePosInfo.PosX, movePosInfo.PosY));

			// 다른 플레이어에게 이동정보를 알려준다
			S_Move resMovePacket = new S_Move();
			resMovePacket.ObjectId = player.Info.ObjectId;
			resMovePacket.PosInfo = movePacket.PosInfo;

			Broadcast(resMovePacket);

            // test log
            Console.WriteLine($"Player Move: {player.Info.ObjectId} : ({movePosInfo.PosX}, {movePosInfo.PosY})");
        }

		public void HandleSkill(Player player, C_Skill skillPacket)
		{
			if (player == null)
				return;

			ObjectInfo info = player.Info;
			if (info.PosInfo.State != CreatureState.Idle)
				return;

            // 스킬 사용 처리
            info.PosInfo.State = CreatureState.Skill;
			S_Skill skill = new S_Skill() { Info = new SkillInfo() };
			skill.ObjectId = info.ObjectId;
			skill.Info.SkillId = skillPacket.Info.SkillId;
			Broadcast(skill);

            // 스킬 데이터
            Data.Skill skillData = null;
			if (DataManager.SkillDict.TryGetValue(skillPacket.Info.SkillId, out skillData) == false)
				return;

            // 스킬 종류에 따른 처리
            switch (skillData.skillType)
			{
				case SkillType.SkillProjectile:
					{
                        // 게임에 투사체 추가
                        Arrow arrow = ObjectManager.Instance.Add<Arrow>();
						if (arrow == null)
							return;

						arrow.Data = skillData;
						arrow.PosInfo.State = CreatureState.Moving;
						arrow.PosInfo.MoveDir = player.PosInfo.MoveDir;
						arrow.PosInfo.PosX = player.PosInfo.PosX;
						arrow.PosInfo.PosY = player.PosInfo.PosY;
						arrow.Speed = skillData.projectile.speed;
						Push(0, EnterGame, arrow);
					}
					break;
			}

            // test log
            Console.WriteLine($"Player Skill: {player.Info.ObjectId} : {skillPacket.Info.SkillId}");
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

		public void Broadcast(IMessage packet)
		{
			foreach (Player p in playerDict.Values)
			{
				p.Session.Send(packet);
			}
		}
	}
}
