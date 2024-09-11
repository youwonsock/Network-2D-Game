using ServerCore;

namespace Server
{
    /// <summary>
    /// 게임 룸 클래스
    /// </summary>
    public class GameRoom : IJobQueue
    {
        List<ClientSession> sessions = new List<ClientSession>();
        JobQueue jobQueue = new JobQueue();
        List<ArraySegment<byte>> pendingList = new List<ArraySegment<byte>>();

        /// <summary>
        /// 세션을 방에 입장시키는 메서드
        /// </summary>
        /// <param name="session"></param>
        public void Enter(ClientSession session)
        {
            // 플레이어 입장
            sessions.Add(session);
            session.Room = this;
        
            // 새로 입장한 플레이어에게 기존 플레이어 정보 전송
            S_PlayerList s_PlayerList = new S_PlayerList();
            foreach (ClientSession s in sessions)
            {
                s_PlayerList.players.Add(new S_PlayerList.Player()
                {
                    isSelf = (session == s),
                    playerId = s.SessionId,
                    posX = s.PosX,
                    posY = s.PosY,
                    posZ = s.PosZ
                });
            }
            session.Send(s_PlayerList.Write());

            // 기존 플레이어에게 새로운 플레이어 정보 전송
            S_BroadcastEnterGame s_BroadcastEnterGame = new S_BroadcastEnterGame();
            s_BroadcastEnterGame.playerId = session.SessionId;
            s_BroadcastEnterGame.posX = 0;
            s_BroadcastEnterGame.posY = 0;
            s_BroadcastEnterGame.posZ = 0;
            Broadcast(s_BroadcastEnterGame.Write());
        }

        /// <summary>
        /// 세션을 방에서 퇴장시키는 메서드
        /// </summary>
        /// <param name="session"></param>
        public void Leave(ClientSession session)
        {
            // 플레이어 퇴장
            sessions.Remove(session);
        
            // 모든 플레이어에게 퇴장한 플레이어 정보 전송
            S_BroadcastLeaveGame s_BroadcastLeaveGame = new S_BroadcastLeaveGame();
            s_BroadcastLeaveGame.playerId = session.SessionId;
            Broadcast(s_BroadcastLeaveGame.Write());
        }

        public void Move(ClientSession session, C_Move packet)
        {
            session.PosX = packet.posX;
            session.PosY = packet.posY;
            session.PosZ = packet.posZ;

            // 모든 플레이어에게 이동한 플레이어 정보 전송
            S_BroadcastMove s_BroadcastMove = new S_BroadcastMove();
            s_BroadcastMove.playerId = session.SessionId;
            s_BroadcastMove.posX = session.PosX;
            s_BroadcastMove.posY = session.PosY;
            s_BroadcastMove.posZ = session.PosZ;
            Broadcast(s_BroadcastMove.Write());
        }

        /// <summary>
        /// 방에 있는 모든 세션에게 전송
        /// </summary>
        /// <param name="session"></param>
        /// <param name="chat"></param>
        public void Broadcast(ArraySegment<byte> segment)
        {
            pendingList.Add(segment);
        }

        public void Push(Action job)
        {
            jobQueue.Push(job);
        }

        public void Flush()
        {
            foreach (ClientSession s in sessions)
                s.Send(pendingList);

            //Console.WriteLine($"Flushed {pendingList.Count} items");
            pendingList.Clear();
        }
    }
}
