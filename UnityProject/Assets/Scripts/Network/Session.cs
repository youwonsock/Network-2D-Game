using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ServerCore
{
    /// <summary>
    /// 패킷을 이용한 통신시 사용할 세션 클래스
    /// </summary>
    public abstract class PacketSession : Session
    {
        public static readonly int HeaderSize = 2;



        // [size(2)][packetId(2)][...][size(2)][packetId(2)][...]
        public sealed override int OnRecv(ArraySegment<byte> buffer)
        {
            int processLen = 0;

            while (true)
            {
                if (buffer.Count < HeaderSize) // 최소한 헤더는 파싱할 수 있어야 함
                    break;

                ushort dataSize = BitConverter.ToUInt16(buffer.Array, buffer.Offset); // 패킷 길이 읽기
                if (buffer.Count < dataSize) // 버퍼에 데이터가 부족하면 루프 탈출
                    break;

                OnRecvPacket(new ArraySegment<byte>(buffer.Array, buffer.Offset, dataSize));

                processLen += dataSize;
                buffer = new ArraySegment<byte>(buffer.Array, buffer.Offset + dataSize, buffer.Count - dataSize);
            }

            return processLen;
        }

        public abstract void OnRecvPacket(ArraySegment<byte> buffer);
    }



    /// <summary>
    /// 클라이언트와의 세션을 관리하는 클래스
    /// </summary>
    public abstract class Session
    {
        Socket socket;
        int disconnected = 0;
        object lockObj = new object();

        SocketAsyncEventArgs sendArgs = new SocketAsyncEventArgs(); // sendArgs를 멤버 변수로 빼놓는 이유는 Send() 호출 시마다 이벤트 객체 생성하는 것을 방지하기 위함
        SocketAsyncEventArgs recvArgs = new SocketAsyncEventArgs();
        Queue<ArraySegment<byte>> sendQueue = new Queue<ArraySegment<byte>>(); // 멀티 스레드 환경에서 Send() 호출 시 큐에 데이터를 넣어놓고 데이터를 꺼내어 전송하도록 함
        List<ArraySegment<byte>> sendBuffList = new List<ArraySegment<byte>>();

        RecvBuffer recvBuffer = new RecvBuffer(65535);



        public abstract void OnConnected(EndPoint endPoint);
        public abstract int OnRecv(ArraySegment<byte> buffer);
        public abstract void OnSend(int numOfBytes);
        public abstract void OnDisconnected(EndPoint endPoint);



        public void Start(Socket socket)
        {
            this.socket = socket;

            recvArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnReceiveCompleted);
            sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);

            RegisterReceive();
        }

        public void Send(ArraySegment<byte> sendBuffer)
        {
            lock (lockObj)
            {
                sendQueue.Enqueue(sendBuffer);

                if (sendBuffList.Count == 0)    // 앞에 전송이 완료된 상태라면
                    RegisterSend();
            }
        }

        public void Send(List<ArraySegment<byte>> sendBuffList)
        {
            if (sendBuffList.Count == 0)
                return;

            lock (lockObj)
            {
                foreach (ArraySegment<byte> sendBuff in sendBuffList)
                    sendQueue.Enqueue(sendBuff);

                if (this.sendBuffList.Count == 0)
                    RegisterSend();
            }
        }

        public void Disconnect()
        {
            if (Interlocked.Exchange(ref disconnected, 1) == 1) // 중복으로 Disconnect() 호출되는 것 방지
                return;

            OnDisconnected(socket.RemoteEndPoint);
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
        }

        private void Clear()
        {
            lock (lockObj)
            {
                sendQueue.Clear();
                sendBuffList.Clear();
            }
        }



        #region 네트워크 통신

        private void RegisterSend()
        {
            if (disconnected == 1)
                return;

            while (sendQueue.Count > 0)
            {
                ArraySegment<byte> buff = sendQueue.Dequeue();
                sendBuffList.Add(buff);      // ArraySegment : 배열의 일부분을 가리키는 구조체
            }
            sendArgs.BufferList = sendBuffList;

            try
            {
                bool pending = socket.SendAsync(sendArgs);
                if (pending == false)
                    OnSendCompleted(null, sendArgs);
            }
            catch (Exception e)
            {
                Console.WriteLine($"RegisterSend Failed {e}");
            }
        }


        private void OnSendCompleted(object sender, SocketAsyncEventArgs args)
        {
            lock (lockObj)
            {
                if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
                {
                    try
                    {
                        sendArgs.BufferList = null;
                        sendBuffList.Clear();

                        OnSend(sendArgs.BytesTransferred);

                        if (sendQueue.Count > 0)
                            RegisterSend();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"OnSendCompleted Failed {e}");
                    }
                }
                else
                {
                    Disconnect();
                }
            }
        }

        private void RegisterReceive()
        {
            if (disconnected == 1)
                return;

            recvBuffer.Clean(); // 이전에 남아있던 데이터를 지움
            ArraySegment<byte> segment = recvBuffer.WriteSegment;
            recvArgs.SetBuffer(segment.Array, segment.Offset, segment.Count);

            try
            {
                bool pending = socket.ReceiveAsync(recvArgs);

                if (pending == false)
                    OnReceiveCompleted(null, recvArgs);
            }
            catch (Exception e)
            {
                Console.WriteLine($"RegisterReceive Failed {e}");
            }
        }

        private void OnReceiveCompleted(object sender, SocketAsyncEventArgs args)
        {
            if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
            {
                try
                {
                    // Write 커서 이동
                    if (recvBuffer.OnWrite(args.BytesTransferred) == false) // 버퍼에 데이터를 쓰는데 실패했을 경우
                    {
                        Disconnect();
                        return;
                    }

                    int processLen = OnRecv(recvBuffer.ReadSegment);
                    if (processLen < 0 || recvBuffer.DataSize < processLen) // 처리한 데이터가 없거나 데이터가 부족할 경우
                    {
                        Disconnect();
                        return;
                    }

                    // Read 커서 이동
                    if (recvBuffer.OnRead(processLen) == false) // 버퍼에서 데이터를 읽는데 실패했을 경우
                    {
                        Disconnect();
                        return;
                    }

                    RegisterReceive();  // 다음 패킷 수신
                }
                catch (Exception e)
                {
                    Console.WriteLine($"OnReceiveCompleted failed {e}");
                }
            }
            else
            {
                Disconnect();
            }
        }

        #endregion
    }
}
