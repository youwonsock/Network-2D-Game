using System;

namespace ServerCore
{
    /// <summary>
    /// RecvBuffer의 재사용을 위한 클래스
    /// </summary>
    public class RecvBuffer
    {
        ArraySegment<byte> buffer;
        int readPos;    // 버퍼에서 읽을 위치
        int writePos;   // 버퍼에 쓸 위치



        public RecvBuffer(int bufferSize)
        {
            buffer = new ArraySegment<byte>(new byte[bufferSize], 0, bufferSize);
        }

        public int DataSize { get { return writePos - readPos; } }  // 버퍼에 남은 처리되지 않은 데이터의 크기
        public int FreeSize { get { return buffer.Count - writePos; } } // 버퍼에 남은 사용 가능한 공간의 크기

        public ArraySegment<byte> ReadSegment   // 버퍼에서 남은 데이터를 읽어오는 함수
        {
            get { return new ArraySegment<byte>(buffer.Array, buffer.Offset + readPos, DataSize); }
        }

        public ArraySegment<byte> WriteSegment  // 버퍼에서 데이터를 쓸 수 있는 위치를 반환하는 함수
        {
            get { return new ArraySegment<byte>(buffer.Array, buffer.Offset + writePos, FreeSize); }
        }



        public void Clean() // 버퍼 초기화
        {
            int dataSize = DataSize;
            if (dataSize == 0)          // 남은 데이터가 없으면
            {
                readPos = writePos = 0;
            }
            else                        // 남은 데이터가 있으면                   
            {
                Array.Copy(buffer.Array, buffer.Offset + readPos, buffer.Array, buffer.Offset, dataSize);
                readPos = 0;
                writePos = dataSize;
            }
        }

        public bool OnRead(int numOfBytes)
        {
            if (numOfBytes > DataSize)
                return false;

            readPos += numOfBytes;
            return true;
        }

        public bool OnWrite(int numOfBytes)
        {
            if (numOfBytes > FreeSize)
                return false;

            writePos += numOfBytes;
            return true;
        }
    }
}
