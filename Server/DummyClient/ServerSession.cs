using ServerCore;
using System.Net;
using System.Text;

namespace DummyClient
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
    /// 서버와 연결 시 수행할 작업을 정의하는 클래스
    /// </summary>
    public class ServerSession : Session
    {
        // unsafe : 포인터 사용을 위해 빌드 속성 변경
        static unsafe void ToBytes(byte[] array, int offset, ulong value)
        {   
            // fixed : unsafe 컨텍스트에서만 허용되는 키워드로
            // 해당 블록 내부의 변수 및 객체를 가비지 컬렉터의 관리 대상에서 제외시킴
            fixed (byte* ptr = &array[offset])
            {
                *(ulong*)ptr = value;
            }
        }

        static unsafe void ToBytes<T>(byte[] array, int offset, T value) where T : unmanaged
        {
            fixed (byte* ptr = &array[offset])
            {
                *(T*)ptr = value;
            }
        }

        public override void OnConnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnConnected : {endPoint}");

            // 패킷 생성
            PlayerInfoReq packet = new PlayerInfoReq() { size = 4, packetID = (ushort)PacketID.PlayerInfoReq, playerId = 1001 };

            for(int i = 0; i < 5; ++i)
            {
                ArraySegment<byte> openSegment = SendBufferHelper.Open(4096);

                ushort size = 0;
                bool success = true;

                size += 2;
                success &= BitConverter.TryWriteBytes(new Span<byte>(openSegment.Array, openSegment.Offset + size, openSegment.Count - size), packet.packetID);
                size += 2;
                success &= BitConverter.TryWriteBytes(new Span<byte>(openSegment.Array, openSegment.Offset + size, openSegment.Count - size), packet.playerId);
                size += 8;
                success &= BitConverter.TryWriteBytes(new Span<byte>(openSegment.Array, openSegment.Offset, openSegment.Count), size);

                ArraySegment<byte> sendBuff = SendBufferHelper.Close(size);

                if (success)
                    Send(sendBuff);
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
