using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DummyClient
{
    class SessionManager
    {
        static SessionManager session = new SessionManager();
        public static SessionManager Instance { get { return session; } }

        List<ServerSession> sessions = new List<ServerSession>();
        object lockObj = new object();

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
                    C_Chat chatPacket = new C_Chat();
                    chatPacket.chat = "Hello Server !";

                    ArraySegment<byte> segment = chatPacket.Write();

                    s.Send(segment);
                }
            }
        }
    }
}
