﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UI_Inventory : UI_Base
{
	public List<UI_Inventory_Item> Items { get; } = new List<UI_Inventory_Item>();

	public override void Init()
	{
		Items.Clear();

		GameObject grid = transform.Find("ItemGrid").gameObject;
		foreach (Transform child in grid.transform)
			Destroy(child.gameObject);

		for (int i = 0; i < 20; i++)
		{
			GameObject go = Managers.Resource.Instantiate("UI/Scene/UI_Inventory_Item", grid.transform);
			UI_Inventory_Item item = go.GetOrAddComponent<UI_Inventory_Item>();
			Items.Add(item);
		}
	}

    public void RefreshUI()
    {
        List<Item> items = Managers.Inven.Items.Values.ToList();
        items.Sort((a, b) => { return a.Slot - b.Slot; });

        foreach (Item item in items)
		{
			if(item.Slot < 0 || item.Slot >= 20)
				continue;
			
			Debug.Log($"RefreshUI: {item.Slot}");


            Items[item.Slot].SetItem(item);
        }
    }
}
