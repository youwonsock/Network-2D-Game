
using System.Collections.Generic;

namespace Server.Game
{
    public class GameLogic : JobSerializer
    {
        public static GameLogic Instance { get; } = new GameLogic();

        Dictionary<int, GameRoom> rooms = new Dictionary<int, GameRoom>();
        int roomId = 1;

        public void Update()
        {
            Flush();

            foreach (GameRoom room in rooms.Values)
            {
                room.Update();
            }
        }

        public GameRoom Add(int mapId)
        {
            GameRoom gameRoom = new GameRoom();
            gameRoom.Push(gameRoom.Init, mapId, 50);

            gameRoom.RoomId = roomId;
            rooms.Add(roomId, gameRoom);
            roomId++;

            return gameRoom;
        }

        public bool Remove(int roomId)
        {
            return rooms.Remove(roomId);
        }

        public GameRoom Find(int roomId)
        {
            GameRoom room = null;
            if (rooms.TryGetValue(roomId, out room))
                return room;

            return null;
        }
    }
}
