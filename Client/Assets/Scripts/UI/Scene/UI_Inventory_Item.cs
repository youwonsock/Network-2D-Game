using Google.Protobuf.Protocol;
using UnityEngine;
using UnityEngine.UI;

public class UI_Inventory_Item : UI_Base
{
	[SerializeField]
	Image icon = null;

	[SerializeField]
	Image frame = null;

	public int ItemDbId { get; private set; }
	public int TemplateId { get; private set; }
    public int Count { get; private set; }
	public bool Equipped { get; private set; }

    public override void Init()
	{
		icon.gameObject.BindEvent((e) =>
        {
            Data.ItemData itemData = null;
            Managers.Data.ItemDict.TryGetValue(TemplateId, out itemData);

            if(itemData == null)
                return;

            if (itemData.itemType == ItemType.Consumable)	// 사용 아이템
				return;

            C_EquipItem equipPacket = new C_EquipItem();
			equipPacket.ItemDbId = ItemDbId;
			equipPacket.Equipped = !Equipped;

            Managers.Network.Send(equipPacket);
        });
	}

	public void SetItem(Item item)
	{
        if (item == null)
        {
            ItemDbId = 0;
            TemplateId = 0;
            Count = 0;
            Equipped = false;

            this.icon.gameObject.SetActive(false);
            this.frame.gameObject.SetActive(false);
        }
        else
        {
            ItemDbId = item.ItemDbId;
            TemplateId = item.TemplateId;
            Count = item.Count;
            Equipped = item.Equipped;

            Data.ItemData itemData = null;
            Managers.Data.ItemDict.TryGetValue(TemplateId, out itemData);

            Sprite icon = Managers.Resource.Load<Sprite>(itemData.iconPath);
            this.icon.sprite = icon;

            this.icon.gameObject.SetActive(true);
            frame.gameObject.SetActive(Equipped);
        }
    }
}
