using ServerCore;
using System.Collections.Specialized;
using System.Net;
using System.Text;

namespace DummyClient
{
    /// <summary>
    /// 서버와 연결 시 수행할 작업을 정의하는 클래스
    /// </summary>
    public class ServerSession : Session
    {
        public override void OnConnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnConnected : {endPoint}");

            // test용 패킷 생성
            C_PlayerInfoReq packet = new C_PlayerInfoReq() { playerId = 1001, name = "ABCD" };

            // test용 스킬 생성
            var skill = new C_PlayerInfoReq.Skill() { id = 101, level = 1, duration = 3.0f };
            skill.attributes.Add(new C_PlayerInfoReq.Skill.Attribute() { att = 77 });
            packet.skills.Add(skill);

            packet.skills.Add(new C_PlayerInfoReq.Skill() { id = 201, level = 2, duration = 4.0f });
            packet.skills.Add(new C_PlayerInfoReq.Skill() { id = 301, level = 3, duration = 5.0f });
            packet.skills.Add(new C_PlayerInfoReq.Skill() { id = 401, level = 4, duration = 6.0f });

            // 패킷 전송
            {
                ArraySegment<byte> s = packet.Write();
                if (s != null)
                    Send(s);
            }
        }

        public override void OnDisconnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnDisconnected : {endPoint}");
        }

        public override int OnRecv(ArraySegment<byte> buffer)
        {
            string recvData = Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count);
            Console.WriteLine($"[From Server] {recvData}");
            return buffer.Count;
        }

        public override void OnSend(int numOfBytes)
        {
            Console.WriteLine($"Transferred bytes: {numOfBytes}");
        }
    }
}
