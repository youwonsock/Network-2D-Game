using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 유니티는 메인 스레드에서만 게임을 조작할 수 있기 때문에
/// 패킷의 처리를 메인 스레드에서 해야한다. 때문에 패킷을 큐에 담아두고 메인 스레드에서 처리하도록 한다.
/// </summary>
public class PacketQueue
{
    public static PacketQueue Instance { get; } = new PacketQueue();

    Queue<IPacket> packetQueue = new Queue<IPacket>();
    object lockObj = new object();



    public void Push(IPacket packet)
    {
        lock (lockObj)
        {
            packetQueue.Enqueue(packet);
        }
    }

    public IPacket Pop()
    {
        lock (lockObj)
        {
            if (packetQueue.Count == 0)
                return null;

            return packetQueue.Dequeue();
        }
    }

    public List<IPacket> PopAll()
    {
        List<IPacket> list = new List<IPacket>();

        lock (lockObj)
        {
            while (packetQueue.Count > 0)
                list.Add(packetQueue.Dequeue());
        }

        return list;
    }
}
