using ServerCore;
using System.Collections.Specialized;
using System.Net;
using System.Text;

namespace DummyClient
{
    /// <summary>
    /// 서버와 연결 시 수행할 작업을 정의하는 클래스
    /// </summary>
    public class ServerSession : PacketSession
    {
        public override void OnConnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnConnected : {endPoint}");
        }

        public override void OnDisconnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnDisconnected : {endPoint}");
        }

        public override void OnRecvPacket(ArraySegment<byte> buffer)
        {
            PacketManager.Instance.OnRecvPacket(this, buffer);
        }

        public override void OnSend(int numOfBytes)
        {
           // Console.WriteLine($"Transferred bytes: {numOfBytes}");
        }
    }
}
