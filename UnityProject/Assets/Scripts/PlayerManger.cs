using System.Collections.Generic;
using UnityEngine;

public class PlayerManger
{
    public static PlayerManger Instance { get; } = new PlayerManger();

    MyPlayer myPlayer;
    Dictionary<int, Player> players = new Dictionary<int, Player>();


    
    public void Add(S_PlayerList packet)
    {
        Object obj = Resources.Load("Player");

        foreach (S_PlayerList.Player player in packet.players)
        {
            GameObject go = Object.Instantiate(obj) as GameObject;
            
            if(player.isSelf)
            {
                myPlayer = go.AddComponent<MyPlayer>();
                myPlayer.PlayerId = player.playerId;

                myPlayer.transform.position = new Vector3(player.posX, player.posY, player.posZ);
            }
            else
            {
                Player p = go.AddComponent<Player>();
                p.transform.position = new Vector3(player.posX, player.posY, player.posZ);
                p.PlayerId = player.playerId;

                players.Add(p.PlayerId, p);
            }
        }
    }

    public void EnterGame(S_BroadcastEnterGame packet)
    {
        if(packet.playerId == myPlayer.PlayerId)
            return;

        GameObject go = Object.Instantiate(Resources.Load("Player")) as GameObject;

        Player player = go.AddComponent<Player>();
        player.PlayerId = packet.playerId;
        player.transform.position = new Vector3(packet.posX, packet.posY, packet.posZ);
        players.Add(packet.playerId, player);
    }

    public void LeaveGame(S_BroadcastLeaveGame packet)
    {
        if(myPlayer.PlayerId == packet.playerId)
        {
            GameObject.Destroy(myPlayer.gameObject);
            myPlayer = null;
        }
        else
        {
            if(players.TryGetValue(packet.playerId, out Player player))
            {
                GameObject.Destroy(player.gameObject);
                players.Remove(packet.playerId);
            }
        }
    }

    public void Move(S_BroadcastMove packet)
    {
        if(myPlayer.PlayerId == packet.playerId)
        {
            myPlayer.transform.position = new Vector3(packet.posX, packet.posY, packet.posZ);
        }
        else
        {
            if(players.TryGetValue(packet.playerId, out Player player))
            {
                player.transform.position = new Vector3(packet.posX, packet.posY, packet.posZ);
            }
        }
    }
}
