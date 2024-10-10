using ServerCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Server
{
	class SessionManager
	{
		static SessionManager session = new SessionManager();
		public static SessionManager Instance { get { return session; } }

		int sessionId = 0;
		Dictionary<int, ClientSession> sessions = new Dictionary<int, ClientSession>();
		object lockObj = new object();


        public List<ClientSession> GetSessions()
        {
            List<ClientSession> sessions = new List<ClientSession>();

            lock (lockObj)
            {
                sessions = this.sessions.Values.ToList();
            }

            return sessions;
        }

        public ClientSession Generate()
		{
			lock (lockObj)
			{
				int sessionId = ++(this.sessionId);

				//새로은 세션 생성 및 아이디 부여
				ClientSession session = new ClientSession();
				session.SessionId = sessionId;
				sessions.Add(sessionId, session);

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
