using ServerCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    ServerSession serverSession = new ServerSession();
    
    void Start()
    {

        string host = Dns.GetHostName();
        IPHostEntry ipHost = Dns.GetHostEntry(host);
        IPAddress iPAddress = ipHost.AddressList[0];
        IPEndPoint endPoint = new IPEndPoint(iPAddress, 7777);
    
        Connector connector = new Connector();

        connector.Connect(endPoint, () => { return serverSession; }, 1);
    }

    // Update is called once per frame
    void Update()
    {
        List<IPacket> list = PacketQueue.Instance.PopAll();

        foreach (IPacket packet in list)
        {
            PacketManager.Instance.HandlePacket(serverSession, packet);
        }
    }

    public void Send(ArraySegment<byte> segment)
    {
        serverSession.Send(segment);
    }
}
