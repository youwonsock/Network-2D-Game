using ServerCore;
using System;
using System.Collections.Generic;
using System.Net;
using Google.Protobuf;

public class NetworkManager
{
	ServerSession session = new ServerSession();

	public void Send(IMessage packet)
	{
        session.Send(packet);
	}

	public void ConnectToGame()
	{
		// DNS (Domain Name System)
		string host = Dns.GetHostName();
		IPHostEntry ipHost = Dns.GetHostEntry(host);
		IPAddress ipAddr = ipHost.AddressList[0];
		IPEndPoint endPoint = new IPEndPoint(ipAddr, 7777);

		Connector connector = new Connector();

		connector.Connect(endPoint,
			() => { return session; },
			1);
	}

	public void Update()
	{
		List<PacketMessage> list = PacketQueue.Instance.PopAll();
		foreach (PacketMessage packet in list)
		{
			Action<PacketSession, IMessage> handler = PacketManager.Instance.GetPacketHandler(packet.Id);
			if (handler != null)
				handler.Invoke(session, packet.Message);
		}

	}

}
