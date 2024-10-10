using Google.Protobuf.Protocol;
using Server.DB;
using Server.Game.Room;

namespace Server.Game
{
	public class Player : GameObject
	{
		public int PlayerDbId { get; set; }
        public ClientSession Session { get; set; }
        public VisionCube Vision { get; set; }
        public Inventory Inven { get; private set; } = new Inventory();

		public int WeaponDamage { get; private set; }
		public int ArmorDefence { get; private set; }

        public override int TotalAttack { get { return Stat.Attack + WeaponDamage; } }
        public override int TotalDefence { get { return ArmorDefence; } }

        public Player()
		{
			ObjectType = GameObjectType.Player;
            Vision = new VisionCube(this);
        }

		public override void OnDamaged(GameObject attacker, int damage)
		{
			base.OnDamaged(attacker, damage);
		}

        public override void OnDead(GameObject attacker)
        {
            base.OnDead(attacker);
        }

        public void OnLeaveGame()
        {
			DbTransaction.SavePlayerStatus_Step1(this, Room);
        }

		public void HandleEquipItem(C_EquipItem equipPacket)
		{
            Item item = Inven.Get(equipPacket.ItemDbId);
            if (item == null)
                return;

            if (item.ItemType == ItemType.Consumable)
                return;

            if (equipPacket.Equipped)
            {
                Item unequipItem = null;

                if (item.ItemType == ItemType.Weapon)
                {
                    unequipItem = Inven.Find(
                        i => i.Equipped && i.ItemType == ItemType.Weapon);
                }
                else if (item.ItemType == ItemType.Armor)
                {
                    ArmorType armorType = ((Armor)item).ArmorType;
                    unequipItem = Inven.Find(
                        i => i.Equipped
                        && i.ItemType == ItemType.Armor
                        && ((Armor)i).ArmorType == armorType);
                }

                if (unequipItem != null)
                {
                    unequipItem.Equipped = false;

                    DbTransaction.EquipItemNoti(this, unequipItem);

                    S_EquipItem unEquipItemPacket = new S_EquipItem();
                    unEquipItemPacket.ItemDbId = unequipItem.ItemDbId;
                    unEquipItemPacket.Equipped = unequipItem.Equipped;

                    Session.Send(unEquipItemPacket);
                }
            }

            item.Equipped = equipPacket.Equipped;

            DbTransaction.EquipItemNoti(this, item);

            S_EquipItem equipItem = new S_EquipItem();
            equipItem.ItemDbId = equipPacket.ItemDbId;
            equipItem.Equipped = equipPacket.Equipped;

            Session.Send(equipItem);

            RefreashAdditionalStat();
        }

        public void RefreashAdditionalStat()
        {
            WeaponDamage = 0;
            ArmorDefence = 0;

            foreach (Item item in Inven.Items.Values)
            {
                if(item.Equipped == false)
                    continue;

                if (item.ItemType == ItemType.Weapon)
                {
                    Weapon weapon = (Weapon)item;
                    WeaponDamage += weapon.Damage;
                }
                else if (item.ItemType == ItemType.Armor)
                {
                    Armor armor = (Armor)item;
                    ArmorDefence += armor.Defence;
                }
            }
        }
    }
}
