using System;
using ServerCore;
using System.Net;
using Google.Protobuf.Protocol;
using Google.Protobuf;
using Server.Game;
using Server.Data;
using System.Collections.Generic;

namespace Server
{
	public partial class ClientSession : PacketSession
	{
		public Player MyPlayer { get; set; }
		public int SessionId { get; set; }
        int reservedSendBytes = 0;
		long lastSendTick = 0;
        long pingpongTick = 0;


        public PlayerServerState ServerState { get; private set; } = PlayerServerState.ServerStateLogin;

		object lockObj = new object();
		List<ArraySegment<byte>> reserveQueue = new List<ArraySegment<byte>>();



        public void Send(IMessage packet)
		{
			string msgName = packet.Descriptor.Name.Replace("_", string.Empty);
			MsgId msgId = (MsgId)Enum.Parse(typeof(MsgId), msgName);
			ushort size = (ushort)packet.CalculateSize();
			byte[] sendBuffer = new byte[size + 4];
			
			// size 및 id 기입
			Array.Copy(BitConverter.GetBytes((ushort)(size + 4)), 0, sendBuffer, 0, sizeof(ushort));
			Array.Copy(BitConverter.GetBytes((ushort)msgId), 0, sendBuffer, 2, sizeof(ushort));

            // 실제 데이터 기입
            Array.Copy(packet.ToByteArray(), 0, sendBuffer, 4, size);
		
			lock (lockObj)
			{
				reserveQueue.Add(sendBuffer);
			}
		}

		public void FlushSend()
		{
			List<ArraySegment<byte>> sendList = null;

            lock (lockObj)
			{
				long delta = (System.Environment.TickCount64 - lastSendTick);
				if (delta < 100 && reservedSendBytes < 10000)
					return;

				reservedSendBytes = 0;
                lastSendTick = System.Environment.TickCount64;

                if (reserveQueue.Count == 0)
                    return;

				sendList = reserveQueue;
                reserveQueue = new List<ArraySegment<byte>>();
            }

            Send(sendList);
        }

        public override void OnConnected(EndPoint endPoint)
		{
			Console.WriteLine($"OnConnected : {endPoint}");

			{
				S_Connected connectedPacket = new S_Connected();
                Send(connectedPacket);
            }

			GameLogic.Instance.PushAfter(5000, Ping);
        }

		public override void OnRecvPacket(ArraySegment<byte> buffer)
		{
			PacketManager.Instance.OnRecvPacket(this, buffer);
		}

		public override void OnDisconnected(EndPoint endPoint)
		{
            GameLogic.Instance.Push(() =>
            {
                GameRoom room = GameLogic.Instance.Find(1);
                room.Push(room.LeaveGame, MyPlayer.Info.ObjectId);
            });

            SessionManager.Instance.Remove(this);

		}

		public override void OnSend(int numOfBytes)
		{

		}

		public void Ping()
		{
			if (pingpongTick > 0)
			{
                long delta = (System.Environment.TickCount64 - pingpongTick);

                if (delta > 30 * 1000)
                {
                    Disconnect();
                    return;
                }
            }

			S_Ping pingPacket = new S_Ping();
            Send(pingPacket);

			GameLogic.Instance.PushAfter(5000, Ping);
        }

		public void HandlePong()
		{
            pingpongTick = System.Environment.TickCount64;
        }
    }
}
