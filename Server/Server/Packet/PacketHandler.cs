using ServerCore;

/// <summary>
/// 패킷 수신 시 처리 로직을 담당하는 클래스
/// </summary>
class PacketHandler
{
    public static void C_PlayerInfoReqHandler(PacketSession session, IPacket packet)
    {
        C_PlayerInfoReq p = packet as C_PlayerInfoReq;

        Console.WriteLine($"PlayerInfoReq: {p.playerId} {p.name}");

        foreach (C_PlayerInfoReq.Skill skill in p.skills)
        {
            Console.WriteLine($"Skill({skill.id})({skill.level})({skill.duration})");
        }
    }
}