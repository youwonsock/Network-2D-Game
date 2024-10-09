using Google.Protobuf.Protocol;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.DB;
using Server.Game;
using ServerCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Server
{
    public partial class ClientSession : PacketSession
    {
        public int AccountDbId { get; private set; }
        
        public List<LobbyPlayerInfo> LobbyPlayers { get; } = new List<LobbyPlayerInfo>();



        public void HandleLogin(C_Login loginPacket)
        {
            Console.WriteLine($"C_LoginHandler : {loginPacket.UniqueId}");

            if(ServerState != PlayerServerState.ServerStateLogin)
                return;

            LobbyPlayers.Clear();

            using (AppDbContext db = new AppDbContext())
            {
                AccountDb findAccount = db.Accounts
                    .Include(a => a.Players)
                    .Where(a => a.AccountName == loginPacket.UniqueId).FirstOrDefault();

                if (findAccount != null)
                {
                    AccountDbId = findAccount.AccountDbId;

                    S_Login loginOk = new S_Login() { LoginOk = 1 };
                    foreach (PlayerDb playerDb in findAccount.Players)
                    {
                        LobbyPlayerInfo lobbyPlayer = new LobbyPlayerInfo()
                        {
                            PlayerDbId = playerDb.PlayerDbId,
                            Name = playerDb.PlayerName,
                            StatInfo = new StatInfo()
                            {
                                Level = playerDb.Level,
                                Hp = playerDb.Hp,
                                MaxHp = playerDb.MaxHp,
                                Attack = playerDb.Attack,
                                Speed = playerDb.Speed,
                                TotalExp = playerDb.TotalExp
                            }
                        };

                        LobbyPlayers.Add(lobbyPlayer);

                        loginOk.Players.Add(lobbyPlayer);
                    }

                    Send(loginOk);
                    ServerState = PlayerServerState.ServerStateLobby;
                }
                else
                {
                    AccountDb newAccount = new AccountDb() { AccountName = loginPacket.UniqueId };
                    db.Accounts.Add(newAccount);
                    bool success = db.SaveChangesEx();
                    if (success == false)
                        return;

                    AccountDbId = newAccount.AccountDbId;

                    S_Login loginOk = new S_Login() { LoginOk = 1 };
                    Send(loginOk);
                    ServerState = PlayerServerState.ServerStateLobby;
                }
            }
        }

        public void HandleEnterGame(C_EnterGame enterGame)
        {
            if(ServerState != PlayerServerState.ServerStateLobby)
                return;

            LobbyPlayerInfo playerInfo = LobbyPlayers.Find(p => p.Name == enterGame.Name);
            if(playerInfo == null)
                return;

            MyPlayer = ObjectManager.Instance.Add<Player>();
            {
                MyPlayer.PlayerDbId = playerInfo.PlayerDbId;
                MyPlayer.Info.Name = playerInfo.Name;
                MyPlayer.Info.PosInfo.State = CreatureState.Idle;
                MyPlayer.Info.PosInfo.MoveDir = MoveDir.Down;
                MyPlayer.Info.PosInfo.PosX = 0;
                MyPlayer.Info.PosInfo.PosY = 0;
                MyPlayer.Stat.MergeFrom(playerInfo.StatInfo);
                MyPlayer.Session = this;

                S_ItemList itemListPacket = new S_ItemList();

                using (AppDbContext db = new AppDbContext())
                {
                    List<ItemDb> items = db.Items 
                        .Where(i => i.OwnerDbId == playerInfo.PlayerDbId)
                        .ToList();

                    foreach (ItemDb itemDb in items)
                    {
                        Item item = Item.MakeItem(itemDb);

                        if(item != null)
                        {
                            MyPlayer.Inven.Add(item);

                            ItemInfo info = new ItemInfo();
                            info.MergeFrom(item.Info);

                            itemListPacket.Items.Add(info);
                        }
                    }
                }

                Send(itemListPacket);
            }

            ServerState = PlayerServerState.ServerStateGame;

            GameRoom room = RoomManager.Instance.Find(1);
            room.Push(room.EnterGame, MyPlayer);
        }

        public void HandleCreatePlayer(C_CreatePlayer createPlayer)
        {
            if (ServerState != PlayerServerState.ServerStateLobby)
                return;

            using (AppDbContext db = new AppDbContext())
            {
                PlayerDb findPlayer = db.Players
                    .Where(p => p.PlayerName == createPlayer.Name).FirstOrDefault();

                if (findPlayer != null)
                {
                    Send(new S_CreatePlayer());
                }
                else
                {
                    StatInfo statInfo = null;
                    DataManager.StatDict.TryGetValue(1, out statInfo);

                    PlayerDb newPlayer = new PlayerDb()
                    {
                        PlayerName = createPlayer.Name,
                        Level = statInfo.Level,
                        Hp = statInfo.Hp,
                        MaxHp = statInfo.MaxHp,
                        Attack = statInfo.Attack,
                        Speed = statInfo.Speed,
                        TotalExp = statInfo.TotalExp,
                        AccountDbId = AccountDbId
                    };

                    db.Players.Add(newPlayer);
                    bool success = db.SaveChangesEx();
                    if (success == false)
                        return;

                    LobbyPlayerInfo lobbyPlayerInfo = new LobbyPlayerInfo()
                    {
                        PlayerDbId = newPlayer.PlayerDbId,
                        Name = newPlayer.PlayerName,
                        StatInfo = new StatInfo()
                        {
                            Level = newPlayer.Level,
                            Hp = newPlayer.Hp,
                            MaxHp = newPlayer.MaxHp,
                            Attack = newPlayer.Attack,
                            Speed = newPlayer.Speed,
                            TotalExp = 0,
                        }
                    };

                    LobbyPlayers.Add(lobbyPlayerInfo);

                    S_CreatePlayer sCreatePlayer = new S_CreatePlayer() { Player = new LobbyPlayerInfo() };
                    sCreatePlayer.Player.MergeFrom(lobbyPlayerInfo);

                    Send(sCreatePlayer);
                }
            }
        }
    }
}
