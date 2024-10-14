# Network-2D-Game

## Developer Info
* 유원석(You Won Sock)
* GitHub : https://github.com/youwonsock
* Mail : qazwsx233434@gmail.com

### Development kits

<p>
<img src="https://upload.wikimedia.org/wikipedia/commons/thumb/1/19/Unity_Technologies_logo.svg/1280px-Unity_Technologies_logo.svg.png" height="40">
</p>

<p>
<img src="https://upload.wikimedia.org/wikipedia/commons/thumb/7/7d/Microsoft_.NET_logo.svg/640px-Microsoft_.NET_logo.svg.png" height="40">
</p>

<b><h2>Periods</h2></b>

* 2024-08 - 2023-09

## Contribution

### Account Server

![login](https://github.com/user-attachments/assets/a919d285-0b14-461e-8a8a-991db1f6eba0)

ASP web server를 사용해 로그인 서버를 제작하였습니다.  
AccountServer는 Client로부터 계정 생성 및 로그인 요청을 받아 DB의 계정 정보와 비교하여 결과를 반환합니다.

<details>
<summary>AccountServer/Controllers/AccountController.cs</summary>
<div markdown="1">

```c#

  [Route("api/[controller]")]
  [ApiController]
  public class AccountController : ControllerBase
  {
      AppDbContext context;
  
      public AccountController(AppDbContext context)
      {
          this.context = context;
      }
  
      [HttpPost]
      [Route("create")]
      public CreateAccountPacketRes CreateAccount([FromBody] CreateAccountPacketReq req)
      {
          CreateAccountPacketRes res = new CreateAccountPacketRes();
  
          AccountDb account = context.Accounts
                                  .AsNoTracking()
                                  .Where(a => a.AccountName == req.AccountName)
                                  .FirstOrDefault();
  
          if (account == null)
          {
              context.Accounts.Add(new AccountDb
              {
                  AccountName = req.AccountName,
                  Password = req.Password
              });
  
              bool success = context.SaveChangesEx();
              res.Success = success;
          }
          else
          {
              res.Success = false;
          }
  
          return res;
      }
  
      [HttpPost]
      [Route("login")]
      public LoginAccountPacketRes LoginAccount([FromBody] LoginAccountPacketReq req)
      {
          LoginAccountPacketRes res = new LoginAccountPacketRes();
  
          AccountDb account = context.Accounts
                                  .AsNoTracking()
                                  .Where(a => a.AccountName == req.AccountName && a.Password == req.Password)
                                  .FirstOrDefault();
  
          if (account == null)
          {
              res.Success = false;
          }
          else
          {
              res.Success = true;
          }
  
          return res;
      }
  }

```

</div>
</details>

<details>
<summary>Client/UI/Scene/UI_LoginScene.cs</summary>
<div markdown="1">

```c#

    public void OnClickCreateButton(PointerEventData evt)
    {
        string account = Get<GameObject>((int)GameObjects.AccountName).GetComponent<InputField>().text;
        string password = Get<GameObject>((int)GameObjects.Password).GetComponent<InputField>().text;

        CreateAccountPacketReq packet = new CreateAccountPacketReq();
        { packet.AccountName = account; packet.Password = password; }

        Managers.Web.SendPostRequest<CreateAccountPacketRes>("account/create", packet, (res) =>
        {
            Debug.Log($"Create Account: {res.Success}");

            Get<GameObject>((int)GameObjects.AccountName).GetComponent<InputField>().text = "";
            Get<GameObject>((int)GameObjects.Password).GetComponent<InputField>().text = "";
        });
    }

    public void OnClickLoginButton(PointerEventData evt)
    {
        string account = Get<GameObject>((int)GameObjects.AccountName).GetComponent<InputField>().text;
        string password = Get<GameObject>((int)GameObjects.Password).GetComponent<InputField>().text;

        LoginAccountPacketReq packet = new LoginAccountPacketReq();
        { packet.AccountName = account; packet.Password = password; }

        Managers.Web.SendPostRequest<LoginAccountPacketRes>("account/login", packet, (res) =>
        {
            Debug.Log($"Login Account: {res.Success}");

            Get<GameObject>((int)GameObjects.AccountName).GetComponent<InputField>().text = "";
            Get<GameObject>((int)GameObjects.Password).GetComponent<InputField>().text = "";

            if (res.Success)
            {
                Managers.Network.ConnectToGame();
                SceneManager.LoadScene("Game");
            }
        });
    }

```

</div>
</details>
</br></br></br>

### Server

#### DataBase 연동

![스크린샷 2024-10-14 185524](https://github.com/user-attachments/assets/c7683922-4a7d-4814-af1e-f9d49207a5dd)
![스크린샷 2024-10-14 185639](https://github.com/user-attachments/assets/d004407a-e17a-4699-861f-c86c19a08690)

Entity Framework를 사용해 DB와 연동하였습니다.  
플레이어 스탯 정보와 아이템 정보를 DB에 저장하여 이 정보를 이용해 서버에서 게임을 진행합니다.

<details>
<summary>Server/DB/DbTransaction.cs</summary>
<div markdown="1">

```c#

  public partial class DbTransaction : JobSerializer
  {
      public static DbTransaction Instance { get; } = new DbTransaction();



      public static void SavePlayerStatus(Player player, GameRoom room)
      {
          if (player == null || room == null)
              return;

          PlayerDb playerDb = new PlayerDb();
          playerDb.PlayerDbId = player.PlayerDbId;
          playerDb.Hp = player.Stat.Hp;
          Instance.Push<PlayerDb, GameRoom>(SaveToDb, playerDb, room);
      }

      private static void SaveToDb(PlayerDb playerDb, GameRoom room)
      {
          using (AppDbContext db = new AppDbContext())
          {
              db.Entry(playerDb).State = EntityState.Unchanged;
              db.Entry(playerDb).Property(nameof(PlayerDb.Hp)).IsModified = true;
              db.SaveChangesEx();
          }
      }

      public static void RewardPlayer(Player player, RewardData rewardData, GameRoom room)
      {
          if (player == null || rewardData == null || room == null)
              return;

          int? slot = player.Inven.GetEmptySlot();
          if (slot == null)
              return;

          ItemDb itemDb = new ItemDb()
          {
              TemplateId = rewardData.itemId,
              Count = rewardData.count,
              Slot = slot.Value,
              OwnerDbId = player.PlayerDbId
          };

          Instance.Push(() =>
          {
              using (AppDbContext db = new AppDbContext())
              {
                  db.Items.Add(itemDb);
                  bool success = db.SaveChangesEx();
                  if (success)
                  {
                      room.Push(() =>
                      {
                          Item newItem = Item.MakeItem(itemDb);
                          player.Inven.Add(newItem);

                          {
                              S_AddItem itemPacket = new S_AddItem();
                              ItemInfo itemInfo = new ItemInfo();
                              itemInfo.MergeFrom(newItem.Info);
                              itemPacket.Items.Add(itemInfo);

                              player.Session.Send(itemPacket);
                          }
                      });
                  }
              }
          });
      }
  }

```

</div>
</details>

#### JobQueue

Thread간의 Lock경합을 줄이기 위해 JobQueue를 사용하였습니다.
패킷 수신 시 이를 처리하는 Job을 JobQueue에 넣어 서버, 클라이언트 모두 메인 스레드에서 처리하도록 하였습니다.

<details>
<summary>Server/Game/Job/JobSerializer.cs</summary>
<div markdown="1">

```c#

  public class JobSerializer
  {
      JobTimer timer = new JobTimer();
      Queue<IJob> jobQueue = new Queue<IJob>();
      object lockObj = new object();
      bool flush = false;

      public IJob PushAfter(int tickAfter, Action action) { return PushAfter(tickAfter, new Job(action)); }
      public IJob PushAfter<T1>(int tickAfter, Action<T1> action, T1 t1) { return PushAfter(tickAfter, new Job<T1>(action, t1)); }
      public IJob PushAfter<T1, T2>(int tickAfter, Action<T1, T2> action, T1 t1, T2 t2) { return PushAfter(tickAfter, new Job<T1, T2>(action, t1, t2)); }
      public IJob PushAfter<T1, T2, T3>(int tickAfter, Action<T1, T2, T3> action, T1 t1, T2 t2, T3 t3) { return PushAfter(tickAfter, new Job<T1, T2, T3>(action, t1, t2, t3)); }

      public IJob PushAfter(int tickAfter, IJob job)
      {
          timer.Push(job, tickAfter);
          return job;
      }

      public void Push(Action action) { Push(new Job(action)); }
      public void Push<T1>(Action<T1> action, T1 t1) { Push(new Job<T1>(action, t1)); }
      public void Push<T1, T2>(Action<T1, T2> action, T1 t1, T2 t2) { Push(new Job<T1, T2>(action, t1, t2)); }
      public void Push<T1, T2, T3>(Action<T1, T2, T3> action, T1 t1, T2 t2, T3 t3) { Push(new Job<T1, T2, T3>(action, t1, t2, t3)); }

      public void Push(IJob job)
      {
          lock (lockObj)
          {
              jobQueue.Enqueue(job);
          }
      }

      public void Flush()
      {
          timer.Flush();

          while (true)
          {
              IJob job = Pop();
              if (job == null)
                  return;

              job.Execute();
          }
      }

      IJob Pop()
      {
          lock (lockObj)
          {
              if (jobQueue.Count == 0)
              {
                  flush = false;
                  return null;
              }
              return jobQueue.Dequeue();
          }
      }
  }

```

</div>
</details>

#### Stat

![Inven](https://github.com/user-attachments/assets/0de5e876-cca5-4e93-a08c-1262d66dd74a)

간단한 인벤토리 시스템을 구현하였습니다.  
플레이어가 아이템 장착, 헤제 시 서버로 장착 여부 및 변경된 스탯을 전송하여 DB에 저장합니다.
이때 변경된 스탯을 기반으로 서버에서 게임 로직연산을 하게됩니다. 

<details>
<summary>Server/Data/Data.Contents.cs</summary>
<div markdown="1">

```c#

  message StatInfo {
  int32 level = 1;
  int32 hp = 2;
  int32 maxHp = 3;
  int32 attack = 4;
  float speed = 5;
  int32 totalExp = 6;
  }

  message ItemInfo {
  int32 itemDbId = 1;
  int32 templateId = 2;
  int32 count = 3;
  int32 slot = 4;
  bool equipped = 5;
  }

```

</div>
</details>

<details>
<summary>Server/Session/ClientSession_PreGame.cs</summary>
<div markdown="1">

```c#

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
                  Hp = statInfo.Hp <= 0 ? statInfo.MaxHp : statInfo.Hp,
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
                      Hp = statInfo.Hp <= 0 ? statInfo.MaxHp : statInfo.Hp,
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

```

</div>
</details>

#### 공간 분할 및 패킷 전송 최적화

![공간 분할](https://github.com/user-attachments/assets/88cc95ad-bd12-4fdc-b3e3-98416ad679b6)

패킷 전송 최적화를 위해 공간 분할을 구현하였습니다.  
플레이어가 이동할 때마다 서버에서 플레이어의 위치를 확인하고 범위내의 다른 플레이어에게만 이동 패킷을 전송합니다.  
클라이언트는 전송받은 패킷을 이용해 범위 밖의 오브젝트를 제거하고 범위 내의 오브젝트를 추가합니다.

<details>
<summary>Server/Game/Room/Zone.cs</summary>
<div markdown="1">

```c#

  public class Zone
  {
      public int IndexY { get; private set; }
      public int IndexX { get; private set; }

      public HashSet<Player> Players { get; private set; } = new HashSet<Player>();
      public HashSet<Monster> Monsters { get; private set; } = new HashSet<Monster>();
      public HashSet<Projectile> Projectiles { get; private set; } = new HashSet<Projectile>();



      public Zone(int y, int x)
      {
          IndexX = x;
          IndexY = y;
      }

      public void Remove(GameObject gameObject)
      {
          if (gameObject == null)
              return;

          GameObjectType type = ObjectManager.GetObjectTypeById(gameObject.Id);

          switch (type)
          {
              case GameObjectType.Player:
                  Players.Remove(gameObject as Player);
                  break;
              case GameObjectType.Monster:
                  Monsters.Remove(gameObject as Monster);
                  break;
              case GameObjectType.Projectile:
                  Projectiles.Remove(gameObject as Projectile);
                  break;
          }
      }

      public Player FindOnePlayer(Func<Player, bool> condition)
      {
          foreach (Player player in Players)
          {
              if (condition.Invoke(player))
                  return player;
          }

          return null;
      }

      public List<Player> FindAllPlayer(Func<Player, bool> condition)
      {
          List<Player> findList = new List<Player>();

          foreach (Player player in Players)
          {
              if (condition.Invoke(player))
                  findList.Add(player);
          }

          return findList;
      }
  }

```

</div>
</details>

<details>
<summary>Server/Game/Room/VisionCube.cs</summary>
<div markdown="1">

```c#

  public class VisionCube
  {
      public Player Owner { get; private set; }
      public HashSet<GameObject> PreviousObjects { get; private set; } = new HashSet<GameObject>();



      public VisionCube(Player owner)
      {
          Owner = owner;
      }

      public HashSet<GameObject> GetherObjects()
      {
          if (Owner == null || Owner.Room == null)
              return null;

          HashSet<GameObject> objects = new HashSet<GameObject>();

          Vector2Int cellPos = Owner.CellPos;
          List<Zone> zones = Owner.Room.GetAdjacentZones(cellPos);

          foreach (Zone zone in zones)
          {
              foreach (Player player in zone.Players)
              {
                  int dx = player.CellPos.x - cellPos.x;
                  int dy = player.CellPos.y - cellPos.y;

                  if (Math.Abs(dx) > GameRoom.VisionCells || Math.Abs(dy) > GameRoom.VisionCells)
                      continue;

                  objects.Add(player);
              }

              foreach (Monster monster in zone.Monsters)
              {
                  int dx = monster.CellPos.x - cellPos.x;
                  int dy = monster.CellPos.y - cellPos.y;

                  if (Math.Abs(dx) > GameRoom.VisionCells || Math.Abs(dy) > GameRoom.VisionCells)
                      continue;

                  objects.Add(monster);
              }

              foreach (Projectile projectile in zone.Projectiles)
              {
                  int dx = projectile.CellPos.x - cellPos.x;
                  int dy = projectile.CellPos.y - cellPos.y;

                  if (Math.Abs(dx) > GameRoom.VisionCells || Math.Abs(dy) > GameRoom.VisionCells)
                      continue;

                  objects.Add(projectile);
              }
          }

          return objects;
      }

      public void Update()
      {
          if (Owner == null || Owner.Room == null)
              return;

          HashSet<GameObject> currentObjects = GetherObjects();

          List<GameObject> added = currentObjects.Except(PreviousObjects).ToList();
          if (added.Count > 0)
          {
              S_Spawn spawnPacket = new S_Spawn();

              foreach (GameObject obj in added)
              {
                  ObjectInfo info = obj.Info;
                  info.MergeFrom(obj.Info);
                  spawnPacket.Objects.Add(info);
              }

              Owner.Session.Send(spawnPacket);
          }

          List<GameObject> removed = PreviousObjects.Except(currentObjects).ToList();
          if (removed.Count > 0)
          {
              S_Despawn despawnPacket = new S_Despawn();
              foreach (GameObject obj in removed)
              {
                  despawnPacket.ObjectIds.Add(obj.Id);
              }

              Owner.Session.Send(despawnPacket);
          }

          PreviousObjects = currentObjects;

          Owner.Room.PushAfter(500, Update);
      }
  }

```

</div>
</details>

#### A* Pathfinding

![A star pathfinding](https://github.com/user-attachments/assets/af6c9ac6-a7ee-4e93-8a52-28b198ae93f5)

A* 알고리즘을 사용해 몬스터의 이동 경로를 계산하였습니다.  
서버에서 몬스터의 이동 경로를 계산하고 이동 패킷을 클라이언트에 전송합니다.

<details>
<summary>Server/Game/Room/Map.cs</summary>
<div markdown="1">

```c#

  // U D L R
  int[] _deltaY = new int[] { 1, -1, 0, 0 };
  int[] _deltaX = new int[] { 0, 0, -1, 1 };
  int[] _cost = new int[] { 10, 10, 10, 10 };

  public List<Vector2Int> FindPath(Vector2Int startCellPos, Vector2Int destCellPos, bool checkObjects = true, int maxDist = 10)
  {
      List<Pos> path = new List<Pos>();

      HashSet<Pos> closeList = new HashSet<Pos>();
      Dictionary<Pos, int> openList = new Dictionary<Pos, int>();
      Dictionary<Pos, Pos> parent = new Dictionary<Pos, Pos>();

      PriorityQueue<PQNode> pq = new PriorityQueue<PQNode>();

      Pos pos = Cell2Pos(startCellPos);
      Pos dest = Cell2Pos(destCellPos);

      openList.Add(pos, 10 * (Math.Abs(dest.Y - pos.Y) + Math.Abs(dest.X - pos.X)));
      pq.Push(new PQNode() { F = 10 * (Math.Abs(dest.Y - pos.Y) + Math.Abs(dest.X - pos.X)), G = 0, Y = pos.Y, X = pos.X });
      parent.Add(pos, pos);

      while (pq.Count > 0)
      {
          PQNode pqNode = pq.Pop();
          Pos node = new Pos(pqNode.Y, pqNode.X);

          if (closeList.Contains(node))
              continue;

          closeList.Add(node);

          if (node.Y == dest.Y && node.X == dest.X)
              break;

          for (int i = 0; i < _deltaY.Length; i++)
          {
              Pos next = new Pos(node.Y + _deltaY[i], node.X + _deltaX[i]);

              if(Math.Abs(dest.Y - next.Y) + Math.Abs(dest.X - next.X) > maxDist)
                  continue;

              if (next.Y != dest.Y || next.X != dest.X)
              {
                  if (CanGo(Pos2Cell(next), checkObjects) == false) // CellPos
                      continue;
              }

              if (closeList.Contains(next))
                  continue;

              int g = 0;// node.G + _cost[i];
              int h = 10 * ((dest.Y - next.Y) * (dest.Y - next.Y) + (dest.X - next.X) * (dest.X - next.X));

              int value = 0;
              if (!openList.TryGetValue(next, out value))
                  value = Int32.MaxValue;

              if (value < g + h)
                  continue;

              if(!openList.TryAdd(next, g + h))
                  openList[next] = g + h;

              pq.Push(new PQNode() { F = g + h, G = g, Y = next.Y, X = next.X });

              if (!parent.TryAdd(next, node))
                  parent[next] = node;
          }
      }

      return CalcCellPathFromParent(parent, dest);
  }

  List<Vector2Int> CalcCellPathFromParent(Dictionary<Pos, Pos> parent, Pos dest)
  {
      List<Vector2Int> cells = new List<Vector2Int>();

      if (parent.ContainsKey(dest) == false)
      {
          Pos nearDest = new Pos();
          int nearDist = Int32.MaxValue;

          foreach (Pos p in parent.Keys)
          {
              int dist = Math.Abs(dest.Y - p.Y) + Math.Abs(dest.X - p.X);

              if(dist < nearDist)
              {
                  nearDest = p;
                  nearDist = dist;
              }
          }

          dest = nearDest;
      }

      Pos pos = dest;
      while (parent[pos] != pos)
      {
          cells.Add(Pos2Cell(pos));
          pos = parent[pos];
      }
      cells.Add(Pos2Cell(pos));
      cells.Reverse();

      return cells;
  }

  Pos Cell2Pos(Vector2Int cell)
  {
      return new Pos(MaxY - cell.y, cell.x - MinX);
  }

  Vector2Int Pos2Cell(Pos pos)
  {
      return new Vector2Int(pos.X + MinX, MaxY - pos.Y);
  }

```

</div>
</details>

