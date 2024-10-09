using System;
using System.Collections.Generic;
using System.Linq;

namespace Server.Game
{
    public class Inventory
    {
        Dictionary<int, Item> items = new Dictionary<int, Item>();

        public void Add(Item item)
        {
            items.Add(item.ItemDbId, item);
        }

        public Item Get(int ItemDbId)
        {
            Item item = null;
            items.TryGetValue(ItemDbId, out item);
            return item;
        }

        public Item Find(Func<Item, bool> condition)
        {
            foreach (var item in items.Values)
            {
                if (condition.Invoke(item))
                    return item;
            }

            return null;
        }

        public int? GetEmptySlot()
        {
            for (int slot = 0; slot < 20; slot++)
            {
                Item item = items.Values.FirstOrDefault(i => i.Slot == slot);
                if (item == null)
                    return slot;
            }

            return null;
        }
    }
}
