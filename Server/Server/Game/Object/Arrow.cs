using Google.Protobuf.Protocol;
using System;

namespace Server.Game
{
	public class Arrow : Projectile
	{
		long nextMoveTick = 0;



		public override void Update()
		{
			if (Data == null || Data.projectile == null || Room == null)
				return;

			if (nextMoveTick >= Environment.TickCount64)
				return;

			long tick = (long)(1000 / Data.projectile.speed);
            nextMoveTick = Environment.TickCount64 + tick;

			Vector2Int destPos = GetFrontCellPos();
			if (Room.Map.CanGo(destPos))
			{
				CellPos = destPos;

                // 방안에 모든 유저에게 이동 패킷 전송
                S_Move movePacket = new S_Move();
				movePacket.ObjectId = Id;
				movePacket.PosInfo = PosInfo;
				Room.Broadcast(movePacket);
			}
			else
			{
				GameObject target = Room.Map.Find(destPos);
				if (target != null)
					target.OnDamaged(Data.damage);

				Room.Push(0, Room.LeaveGame, Id);   // delete arrow from room
            }
		}
	}
}
