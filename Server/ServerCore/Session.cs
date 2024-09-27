using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ServerCore
{
	public abstract class PacketSession : Session
	{
		public static readonly int HeaderSize = 2;

		// [size(2)][packetId(2)][ ... ][size(2)][packetId(2)][ ... ]
		public sealed override int OnRecv(ArraySegment<byte> buffer)
		{
			int processLen = 0;

			while (true)
			{
                // 최소한 헤더 사이즈 이상이 되어야 데이터를 처리할 수 있다.
                if (buffer.Count < HeaderSize)
					break;

				// 도착한 패킷 사이즈 확인
				ushort dataSize = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
				if (buffer.Count < dataSize)
					break;

				// 패킷 해석
				OnRecvPacket(new ArraySegment<byte>(buffer.Array, buffer.Offset, dataSize));

				processLen += dataSize;
				buffer = new ArraySegment<byte>(buffer.Array, buffer.Offset + dataSize, buffer.Count - dataSize);
			}

			return processLen;
		}

		public abstract void OnRecvPacket(ArraySegment<byte> buffer);
	}

	public abstract class Session
	{
		Socket socket;
		int disconnected = 0;
		object lockObj = new object();

		RecvBuffer recvBuffer = new RecvBuffer(65535);

		Queue<ArraySegment<byte>> sendQueue = new Queue<ArraySegment<byte>>();
		List<ArraySegment<byte>> pendingList = new List<ArraySegment<byte>>();
		SocketAsyncEventArgs sendArgs = new SocketAsyncEventArgs();
		SocketAsyncEventArgs recvArgs = new SocketAsyncEventArgs();



		public abstract void OnConnected(EndPoint endPoint);
		public abstract int  OnRecv(ArraySegment<byte> buffer);
		public abstract void OnSend(int numOfBytes);
		public abstract void OnDisconnected(EndPoint endPoint);

		void Clear()
		{
			lock (lockObj)
			{
				sendQueue.Clear();
				pendingList.Clear();
			}
		}

		public void Start(Socket socket)
		{
			this.socket = socket;

			recvArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnRecvCompleted);
			sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);

			RegisterRecv();
		}

		public void Send(List<ArraySegment<byte>> sendBuffList)
		{
			if (sendBuffList.Count == 0)
				return;

			lock (lockObj)
			{
				foreach (ArraySegment<byte> sendBuff in sendBuffList)
					sendQueue.Enqueue(sendBuff);

				if (pendingList.Count == 0)
					RegisterSend();
			}
		}

		public void Send(ArraySegment<byte> sendBuff)
		{
			lock (lockObj)
			{
				sendQueue.Enqueue(sendBuff);
				if (pendingList.Count == 0)
					RegisterSend();
			}
		}

		public void Disconnect()
		{
			if (Interlocked.Exchange(ref disconnected, 1) == 1)
				return;

			OnDisconnected(socket.RemoteEndPoint);
			socket.Shutdown(SocketShutdown.Both);
			socket.Close();
			Clear();
		}

		#region 네트워크 통신

		void RegisterSend()
		{
			if (disconnected == 1)
				return;

			while (sendQueue.Count > 0)
			{
				ArraySegment<byte> buff = sendQueue.Dequeue();
				pendingList.Add(buff);
			}
			sendArgs.BufferList = pendingList;

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

		void OnSendCompleted(object sender, SocketAsyncEventArgs args)
		{
			lock (lockObj)
			{
				if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
				{
					try
					{
						sendArgs.BufferList = null;
						pendingList.Clear();

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

		void RegisterRecv()
		{
			if (disconnected == 1)
				return;

			recvBuffer.Clean();
			ArraySegment<byte> segment = recvBuffer.WriteSegment;
			recvArgs.SetBuffer(segment.Array, segment.Offset, segment.Count);

			try
			{
				bool pending = socket.ReceiveAsync(recvArgs);
				if (pending == false)
					OnRecvCompleted(null, recvArgs);
			}
			catch (Exception e)
			{
				Console.WriteLine($"RegisterRecv Failed {e}");
			}
		}

		void OnRecvCompleted(object sender, SocketAsyncEventArgs args)
		{
			if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
			{
				try
				{
					// Write 커서 이동
					if (recvBuffer.OnWrite(args.BytesTransferred) == false)
					{
						Disconnect();
						return;
					}

					// 컨텐츠 쪽으로 데이터를 넘겨주고 얼마나 처리했는지 받는다
					int processLen = OnRecv(recvBuffer.ReadSegment);
					if (processLen < 0 || recvBuffer.DataSize < processLen)
					{
						Disconnect();
						return;
					}

					// Read 커서 이동
					if (recvBuffer.OnRead(processLen) == false)
					{
						Disconnect();
						return;
					}

					RegisterRecv();
				}
				catch (Exception e)
				{
					Console.WriteLine($"OnRecvCompleted Failed {e}");
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
