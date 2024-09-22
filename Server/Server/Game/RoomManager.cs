using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game
{
    public class RoomManager
    {
        public static RoomManager Instance { get; } = new RoomManager();

        object lockObj = new object();
        Dictionary<int, GameRoom> rooms = new Dictionary<int, GameRoom>();
        int roomID = 1;

        public GameRoom Add()
        {
            GameRoom room = new GameRoom();

            lock (lockObj)
            {
                room.RoomID = roomID;
                rooms.Add(room.RoomID++, room);
            }

            return room;
        }

        public bool Remove(int roomID)
        {
            lock (lockObj)
            {
                return rooms.Remove(roomID);
            }
        }

        public GameRoom Find(int roomID)
        {
            lock (lockObj)
            {
                GameRoom room = null;
                if (rooms.TryGetValue(roomID, out room))
                    return room;
            }

            return null;
        }
    }
}
