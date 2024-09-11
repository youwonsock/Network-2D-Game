
namespace DummyClient
{
    class SessionManager
    {
        static SessionManager session = new SessionManager();
        public static SessionManager Instance { get { return session; } }

        List<ServerSession> sessions = new List<ServerSession>();
        object lockObj = new object();
        Random rand = new Random();

        public ServerSession Generate()
        {
            lock (lockObj)
            {
                ServerSession s = new ServerSession();
                sessions.Add(s);
                return s;
            }
        }

        public void SendForEach()
        {
            lock (lockObj)
            {
                foreach (ServerSession s in sessions)
                {
                    C_Move movePacket = new C_Move();
                    movePacket.posX = rand.Next(-50, 50);
                    movePacket.posY = 0;
                    movePacket.posZ = rand.Next(-50, 50);

                    s.Send(movePacket.Write());
                }
            }
        }
    }
}
