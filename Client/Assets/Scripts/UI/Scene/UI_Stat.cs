﻿using Data;
using Google.Protobuf.Protocol;
using UnityEngine;
using UnityEngine.UI;

public class UI_Stat : UI_Base
{
    bool isInit = false;

    enum Images
    {
        Slot_Helmet,
        Slot_Armor,
        Slot_Boots,
        Slot_Weapon,
        Slot_Shield
    }

    enum Texts
    {
        NameText,
        AttackValueText,
        DefenceValueText
    }

    public override void Init()
    {
        Bind<Image>(typeof(Images));
        Bind<Text>(typeof(Texts));

        isInit = true;
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (!isInit)
            return;

        Get<Image>((int)Images.Slot_Helmet).enabled = false;
        Get<Image>((int)Images.Slot_Armor).enabled = false;
        Get<Image>((int)Images.Slot_Boots).enabled = false;
        Get<Image>((int)Images.Slot_Weapon).enabled = false;
        Get<Image>((int)Images.Slot_Shield).enabled = false;
            
        foreach (Item item in Managers.Inven.Items.Values)
        {
            if (item.Equipped == false)
                continue;

            ItemData itemData = null;
            Managers.Data.ItemDict.TryGetValue(item.TemplateId, out itemData);
            Sprite icon = Managers.Resource.Load<Sprite>(itemData.iconPath);

            if(item.ItemType == ItemType.Weapon)
            {
                Get<Image>((int)Images.Slot_Weapon).enabled = true;
                Get<Image>((int)Images.Slot_Weapon).sprite = icon;
            }
            else if (item.ItemType == ItemType.Armor)
            {
                Armor armor = (Armor)item;
                switch (armor.ArmorType)
                {
                    case ArmorType.Helmet:
                        Get<Image>((int)Images.Slot_Helmet).enabled = true;
                        Get<Image>((int)Images.Slot_Helmet).sprite = icon;
                        break;
                    case ArmorType.Armor:
                        Get<Image>((int)Images.Slot_Armor).enabled = true;
                        Get<Image>((int)Images.Slot_Armor).sprite = icon;
                        break;
                    case ArmorType.Boots:
                        Get<Image>((int)Images.Slot_Boots).enabled = true;
                        Get<Image>((int)Images.Slot_Boots).sprite = icon;
                        break;
                }
            }
        }

        MyPlayerController player = Managers.Object.MyPlayer;
        player.RefreshAdditionalStat();

        Get<Text>((int)Texts.NameText).text = player.name;

        int totalDamage = player.WeaponDamage + player.Stat.Attack;    
        Get<Text>((int)Texts.AttackValueText).text = $"{totalDamage} (+{player.WeaponDamage})";
        Get<Text>((int)Texts.DefenceValueText).text = $"{player.ArmorDefence}";
    }
}
