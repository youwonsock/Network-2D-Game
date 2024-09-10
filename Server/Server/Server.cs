using System.Net;

using ServerCore;

namespace Server
{
    /// <summary>
    /// 콘텐츠 서버
    /// </summary>
    class Server
    {
        static Listener _listener = new Listener();
        public static GameRoom Room = new GameRoom();

        static void Main(string[] args)
        {
            Thread.Sleep(1000);
            Console.WriteLine("Server\n\n");

            // DNS (Domain Name System)
            string host = Dns.GetHostName(); // 현재 컴퓨터의 호스트 이름을 가져옴
            IPHostEntry ipHost = Dns.GetHostEntry(host); // 호스트 이름에 해당하는 IP 주소들을 가져옴    ipHost.AddressList => IP 주소 목록
            IPAddress ipAddress = ipHost.AddressList[0]; // 첫 번째 IP 주소를 사용
            IPEndPoint endPoint = new IPEndPoint(ipAddress, 7777); // 서버의 주소와 포트 번호를 설정

            _listener.Init(endPoint, () => { return SessionManager.Instance.Generate(); });
            Console.WriteLine("Listening...");

            FlushRoom();

            while (true)
            {
                JobTimer.Instance.Flush();
            }
        }

        static void FlushRoom()
        {
            Room.Push(() => Room.Flush());
            JobTimer.Instance.Push(FlushRoom, 250);
        }
    }
}