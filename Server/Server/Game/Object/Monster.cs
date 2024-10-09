using Google.Protobuf.Protocol;
using Server.Data;
using Server.DB;
using System;
using System.Collections.Generic;

namespace Server.Game
{
	public class Monster : GameObject
    {
        Player target;
        int searchCellDist = 10;
        int chaseCellDist = 20;
        int skillRange = 1;
        long coolTick = 0;
        long nextSearchTick = 0;
        long nextMoveTick = 0;

		public int TemplateId { get; private set; }



        public Monster()
		{
			ObjectType = GameObjectType.Monster;
		}

		public void Init(int templateId)
		{
            TemplateId = templateId;

            MonsterData monsterData = null;
            DataManager.MonsterDict.TryGetValue(TemplateId, out monsterData);
			Stat.MergeFrom(monsterData.stat);
			Stat.Hp = monsterData.stat.MaxHp;

            State = CreatureState.Idle;
        }

        public override void Update()
		{
			switch (State)
			{
				case CreatureState.Idle:
					UpdateIdle();
					break;
				case CreatureState.Moving:
					UpdateMoving();
					break;
				case CreatureState.Skill:
					UpdateSkill();
					break;
				case CreatureState.Dead:
					UpdateDead();
					break;
			}
		}

		protected virtual void UpdateIdle()
		{
			if (nextSearchTick > Environment.TickCount64)
				return;
			nextSearchTick = Environment.TickCount64 + 1000;

			Player target = Room.FindPlayer(p =>
			{
				Vector2Int dir = p.CellPos - CellPos;
				return dir.cellDistFromZero <= searchCellDist;	// 탐지 거리내에 플레이어가 있다면
			});

			if (target == null)
				return;

			this.target = target;           // 타겟 설정
            State = CreatureState.Moving;   // 플레이어를 향해 이동
        }

		protected virtual void UpdateMoving()
		{
			if (nextMoveTick > Environment.TickCount64)
				return;
			int moveTick = (int)(1000 / Speed);
			nextMoveTick = Environment.TickCount64 + moveTick;

			if (target == null || target.Room != Room)	// 타겟이 사라진 경우
			{
				target = null;
				State = CreatureState.Idle;
				BroadcastMove();
				return;
			}

			Vector2Int dir = target.CellPos - CellPos;
			int dist = dir.cellDistFromZero;
			if (dist == 0 || dist > chaseCellDist)	// 타겟을 놓친 경우
			{
				target = null;
				State = CreatureState.Idle;
				BroadcastMove();
				return;
			}

			List<Vector2Int> path = Room.Map.FindPath(CellPos, target.CellPos, checkObjects: false);    // 타겟까지의 경로 계산
            if (path.Count < 2 || path.Count > chaseCellDist)   // 경로가 없거나 너무 긴 경우
            {
				target = null;
				State = CreatureState.Idle;
				BroadcastMove();
				return;
			}

			if (dist <= skillRange && (dir.x == 0 || dir.y == 0)) // 스킬 사용 가능한 경우
            {
				coolTick = 0;
				State = CreatureState.Skill;
				return;
			}

			// 이동
			Dir = GetDirFromVec(path[1] - CellPos);
			Room.Map.ApplyMove(this, path[1]);
			BroadcastMove();
		}

		void BroadcastMove()
		{
			// 다른 플레이어한테도 알려준다
			S_Move movePacket = new S_Move();
			movePacket.ObjectId = Id;
			movePacket.PosInfo = PosInfo;
			Room.Broadcast(movePacket);
		}

		protected virtual void UpdateSkill()
		{
			if (coolTick == 0)	// 쿨타임이 0이면(스킬이 사용 가능하면)
			{
				// 유효한 타겟인지
				if (target == null || target.Room != Room || target.Hp == 0)
				{
					target = null;
					State = CreatureState.Moving;
					BroadcastMove();
					return;
				}

				// 스킬이 아직 사용 가능한지
				Vector2Int dir = (target.CellPos - CellPos);
				int dist = dir.cellDistFromZero;
				bool canUseSkill = (dist <= skillRange && (dir.x == 0 || dir.y == 0));
				if (canUseSkill == false)   // 스킬 사용이 불가능하면
                {
					State = CreatureState.Moving;
					BroadcastMove();
					return;
				}

				// 타게팅 방향 주시
				MoveDir lookDir = GetDirFromVec(dir);
				if (Dir != lookDir)
				{
					Dir = lookDir;
					BroadcastMove();
				}

				Skill skillData = null;
				if (DataManager.SkillDict.TryGetValue(1, out skillData))
				{
					// 데미지 판정
					target.OnDamaged(this, skillData.damage + Stat.Attack);

					// 스킬 사용 Broadcast
					S_Skill skill = new S_Skill() { Info = new SkillInfo() };
					skill.ObjectId = Id;
					skill.Info.SkillId = skillData.id;
					Room.Broadcast(skill);

					// 스킬 쿨타임 적용
					int coolTick = (int)(1000 * skillData.cooldown);
					this.coolTick = Environment.TickCount64 + coolTick;
				}
			}

			if (coolTick > Environment.TickCount64) // 쿨타임이 남아있으면
                return;

			coolTick = 0;   // 쿨타임 초기화
        }

		protected virtual void UpdateDead()
		{

		}

		public override void OnDead(GameObject attacker)
        {
            base.OnDead(attacker);

            GameObject owner = attacker.GetOwner();
             if (owner.ObjectType == GameObjectType.Player)
            {
                RewardData rewardData = GetRandomReward();
                if (rewardData != null)
                {
                    Player player = (Player)owner;
                    DbTransaction.RewardPlayer(player, rewardData, Room);
                }
            }
        }

		private RewardData GetRandomReward()
		{
			MonsterData monsterData = null;
			DataManager.MonsterDict.TryGetValue(TemplateId, out monsterData);

			int rand = new Random().Next(0, 100);
			int sum = 0;
			foreach (RewardData reward in monsterData.rewards)
			{
				sum += reward.probability;
				if(rand < sum)
                    return reward;
            }

            return null;
        }
    }
}
