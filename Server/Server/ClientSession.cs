using ServerCore;
using System.Net;

namespace Server
{
    // 기본 패킷
    class Packet
    {
        public ushort size;
        public ushort packetID;
    }

    class PlayerInfoReq : Packet
    {
        public long playerId;
    }

    class PlayerInfoAck : Packet
    {
        public int hp;
        public int attack;
    }

    // 패킷 구분을 위한 enum
    public enum PacketID
    {
        PlayerInfoReq = 1,
        PlayerInfoOk
    }



    /// <summary>
    /// 클라이언트와 연결 시 수행할 작업을 정의하는 클래스
    /// </summary>
    public class ClientSession : PacketSession
    {
        public override void OnConnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnConnected : {endPoint}");
            Thread.Sleep(5000);
            Disconnect();
        }

        public override void OnDisconnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnDisconnected : {endPoint}");
        }

        public override void OnSend(int numOfBytes)
        {
            Console.WriteLine($"Transferred bytes: {numOfBytes}");
        }

        public override void OnRecvPacket(ArraySegment<byte> buffer)
        {
            int pos = 0;

            ushort size = BitConverter.ToUInt16(buffer.Array, buffer.Offset + pos);
            pos += 2;
            ushort packetId = BitConverter.ToUInt16(buffer.Array, buffer.Offset + pos);
            pos += 2;

            // to do
            switch ((PacketID)packetId)
            {
                case PacketID.PlayerInfoReq:
                    {
                        long playerId = BitConverter.ToInt64(buffer.Array, buffer.Offset + pos);
                        pos += 8;

                        Console.WriteLine($"PlayerInfoReq : {playerId}");
                    }
                    break;
                case PacketID.PlayerInfoOk:
                    {
                        int hp = BitConverter.ToInt32(buffer.Array, buffer.Offset + pos);
                        pos += 4;
                        int attack = BitConverter.ToInt32(buffer.Array, buffer.Offset + pos);
                        pos += 4;

                        Console.WriteLine($"PlayerInfoOk : {hp}, {attack}");
                    }
                    break;
            }

            Console.WriteLine($"Recv Packet ID : {packetId}, Size : {size}");
        }
    }
}
