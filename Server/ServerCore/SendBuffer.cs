using System.Threading;

namespace ServerCore
{
    /// <summary>
    /// SendBuffer의 편리한 사용을 위한 Helper 클래스
    /// </summary>
    public class SendBufferHelper
    {
        public static ThreadLocal<SendBuffer> CurrentBuffer = new ThreadLocal<SendBuffer>(() => { return null; });

        public static int ChunkSize { get; set; } = 4096 * 100;

        public static ArraySegment<byte> Open(int reserveSize)
        {
            if (CurrentBuffer.Value == null)
                CurrentBuffer.Value = new SendBuffer(ChunkSize);

            if (CurrentBuffer.Value.FreeSize < reserveSize) // 남은 공간이 부족하면 새로운 버퍼로 갱신
                CurrentBuffer.Value = new SendBuffer(ChunkSize);

            return CurrentBuffer.Value.Open(reserveSize);
        }

        public static ArraySegment<byte> Close(int usedSize)
        {
            return CurrentBuffer.Value.Close(usedSize);
        }
    }

    /// <summary>
    /// Send 마다 버퍼를 생성하는 것이 아닌 버퍼의 재사용을 위한 클래스
    /// </summary>
    public class SendBuffer
    {
        // 별도의 클리어 함수가 없는 이유는 SendBuffer는 다른 세션에서도 사용할 수 있기때문에
        // 클리어 함수 호출 시점이 애매해서 그냥 소진될 때까지 사용
        byte[] buffer;
        int usedSize = 0;



        public int FreeSize { get { return buffer.Length - usedSize; } }

        public SendBuffer(int chunkSize)
        {
            buffer = new byte[chunkSize];
        }

        public ArraySegment<byte> Open(int reserveSize) // buffer 반환(시작 위치)
        {
            if (reserveSize > FreeSize)
                return null;

            return new ArraySegment<byte>(buffer, usedSize, reserveSize);
        }

        public ArraySegment<byte> Close(int usedSize) // 사용한 데이터의 크기를 매개변수로 전달해 실제로 사용한 배열 반환
        {
            ArraySegment<byte> segment = new ArraySegment<byte>(buffer, this.usedSize, usedSize);
            this.usedSize += usedSize;

            return segment;
        }
    }
}
