using Google.Protobuf;
using Google.Protobuf.Protocol;
using ServerCore;
using System;
using System.Collections.Generic;

class PacketManager
{
	#region Singleton
	static PacketManager instance = new PacketManager();
	public static PacketManager Instance { get { return instance; } }

	PacketManager()
	{
		Register();
	}

	#endregion

	Dictionary<ushort, Action<PacketSession, ArraySegment<byte>, ushort>> onRecv = new Dictionary<ushort, Action<PacketSession, ArraySegment<byte>, ushort>>();
	Dictionary<ushort, Action<PacketSession, IMessage>> handler = new Dictionary<ushort, Action<PacketSession, IMessage>>();
		
	public Action<PacketSession, IMessage, ushort> CustomHandler { get; set; }



	public void Register()
	{		
		onRecv.Add((ushort)MsgId.CMove, MakePacket<C_Move>);
		handler.Add((ushort)MsgId.CMove, PacketHandler.C_MoveHandler);		
		onRecv.Add((ushort)MsgId.CSkill, MakePacket<C_Skill>);
		handler.Add((ushort)MsgId.CSkill, PacketHandler.C_SkillHandler);		
		onRecv.Add((ushort)MsgId.CLogin, MakePacket<C_Login>);
		handler.Add((ushort)MsgId.CLogin, PacketHandler.C_LoginHandler);		
		onRecv.Add((ushort)MsgId.CEnterGame, MakePacket<C_EnterGame>);
		handler.Add((ushort)MsgId.CEnterGame, PacketHandler.C_EnterGameHandler);		
		onRecv.Add((ushort)MsgId.CCreatePlayer, MakePacket<C_CreatePlayer>);
		handler.Add((ushort)MsgId.CCreatePlayer, PacketHandler.C_CreatePlayerHandler);
	}

	public void OnRecvPacket(PacketSession session, ArraySegment<byte> buffer)
	{
		ushort count = 0;

		ushort size = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
		count += 2;
		ushort id = BitConverter.ToUInt16(buffer.Array, buffer.Offset + count);
		count += 2;

		Action<PacketSession, ArraySegment<byte>, ushort> action = null;
		if (onRecv.TryGetValue(id, out action))
			action.Invoke(session, buffer, id);
	}

	void MakePacket<T>(PacketSession session, ArraySegment<byte> buffer, ushort id) where T : IMessage, new()
	{
		T pkt = new T();
		pkt.MergeFrom(buffer.Array, buffer.Offset + 4, buffer.Count - 4);

		if (CustomHandler != null)
		{
			CustomHandler.Invoke(session, pkt, id);
		}
		else
		{
			Action<PacketSession, IMessage> action = null;
			if (handler.TryGetValue(id, out action))
				action.Invoke(session, pkt);
		}
	}

	public Action<PacketSession, IMessage> GetPacketHandler(ushort id)
	{
		Action<PacketSession, IMessage> action = null;
		if (handler.TryGetValue(id, out action))
			return action;
		return null;
	}
}