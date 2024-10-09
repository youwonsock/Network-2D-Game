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
		onRecv.Add((ushort)MsgId.SEnterGame, MakePacket<S_EnterGame>);
		handler.Add((ushort)MsgId.SEnterGame, PacketHandler.S_EnterGameHandler);		
		onRecv.Add((ushort)MsgId.SLeaveGame, MakePacket<S_LeaveGame>);
		handler.Add((ushort)MsgId.SLeaveGame, PacketHandler.S_LeaveGameHandler);		
		onRecv.Add((ushort)MsgId.SSpawn, MakePacket<S_Spawn>);
		handler.Add((ushort)MsgId.SSpawn, PacketHandler.S_SpawnHandler);		
		onRecv.Add((ushort)MsgId.SDespawn, MakePacket<S_Despawn>);
		handler.Add((ushort)MsgId.SDespawn, PacketHandler.S_DespawnHandler);		
		onRecv.Add((ushort)MsgId.SMove, MakePacket<S_Move>);
		handler.Add((ushort)MsgId.SMove, PacketHandler.S_MoveHandler);		
		onRecv.Add((ushort)MsgId.SSkill, MakePacket<S_Skill>);
		handler.Add((ushort)MsgId.SSkill, PacketHandler.S_SkillHandler);		
		onRecv.Add((ushort)MsgId.SChangeHp, MakePacket<S_ChangeHp>);
		handler.Add((ushort)MsgId.SChangeHp, PacketHandler.S_ChangeHpHandler);		
		onRecv.Add((ushort)MsgId.SDie, MakePacket<S_Die>);
		handler.Add((ushort)MsgId.SDie, PacketHandler.S_DieHandler);		
		onRecv.Add((ushort)MsgId.SConnected, MakePacket<S_Connected>);
		handler.Add((ushort)MsgId.SConnected, PacketHandler.S_ConnectedHandler);		
		onRecv.Add((ushort)MsgId.SLogin, MakePacket<S_Login>);
		handler.Add((ushort)MsgId.SLogin, PacketHandler.S_LoginHandler);		
		onRecv.Add((ushort)MsgId.SCreatePlayer, MakePacket<S_CreatePlayer>);
		handler.Add((ushort)MsgId.SCreatePlayer, PacketHandler.S_CreatePlayerHandler);		
		onRecv.Add((ushort)MsgId.SItemList, MakePacket<S_ItemList>);
		handler.Add((ushort)MsgId.SItemList, PacketHandler.S_ItemListHandler);		
		onRecv.Add((ushort)MsgId.SAddItem, MakePacket<S_AddItem>);
		handler.Add((ushort)MsgId.SAddItem, PacketHandler.S_AddItemHandler);
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