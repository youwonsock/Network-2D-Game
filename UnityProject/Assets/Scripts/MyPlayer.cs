using ServerCore;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class MyPlayer : Player
{
    NetworkManager networkManager;

    void Start()
    {
        StartCoroutine(CoSendPacket());
    
        networkManager = GameObject.Find("NetworkManager").GetComponent<NetworkManager>();
    }



    IEnumerator CoSendPacket()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.25f);

            C_Move movePacket = new C_Move();
            movePacket.posX = UnityEngine.Random.Range(-50, 50);
            movePacket.posY = 0;
            movePacket.posZ = UnityEngine.Random.Range(-50, 50);

            networkManager.Send(movePacket.Write());
        }
    }
}
