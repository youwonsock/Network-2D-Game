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
            // 방에 입장한 세션을 리스트에 추가
            sessions.Add(session);
            session.Room = this;
        }

        /// <summary>
        /// 세션을 방에서 퇴장시키는 메서드
        /// </summary>
        /// <param name="session"></param>
        public void Leave(ClientSession session)
        {
            sessions.Remove(session);
        }

        /// <summary>
        /// 방에 있는 모든 세션에게 전송
        /// </summary>
        /// <param name="session"></param>
        /// <param name="chat"></param>
        public void Broadcast(ClientSession session, string chat)
        {
            S_Chat packet = new S_Chat();
            packet.playerId = session.SessionId;
            packet.chat = $"Player[{packet.playerId}] : {chat}";

            ArraySegment<byte> segment = packet.Write();

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

            Console.WriteLine($"Flushed {pendingList.Count} items");
            pendingList.Clear();
        }
    }
}
