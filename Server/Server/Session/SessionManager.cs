
namespace Server
{
    class SessionManager
    {
        static SessionManager session = new SessionManager();
        public static SessionManager Instance { get { return session; } }



        int sessionId = 0;
        Dictionary<int, ClientSession> sessions = new Dictionary<int, ClientSession>();
        object lockObj = new object();



        public ClientSession Generate()
        {
            lock (lockObj)
            {
                ClientSession session = new ClientSession();
                session.SessionId = ++sessionId;
                sessions.Add(sessionId, session);

                Console.WriteLine($"Connected : {sessionId}");

                return session;
            }
        }

        public ClientSession Find(int id)
        {
            lock (lockObj)
            {
                ClientSession session = null;
                sessions.TryGetValue(id, out session);
                return session;
            }
        }

        public void Remove(ClientSession session)
        {
            lock (lockObj)
            {
                sessions.Remove(session.SessionId);
            }
        }
    }
}
