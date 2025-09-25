
using System;
using MMORPG.Common.Proto.Inventory;

namespace MMORPG.Common.Inventory
{
    //物品类型
    public enum ItemType
    {
        Material,       //材料&tool
        Consumable,     //Consumables
        Equipment,      //武器&equipment
    }
    //物品品质
    public enum Quality
    {
        Common,     // usually
        Uncommon,   // 非凡
        Rare,       // rare
        Epic,       // epic
        Legendary,  // legend
        Artifact,   // 神器
    }


    /// <summary>
    /// 物品基类
    /// </summary>
    [Serializable]
    public class Item
    {
        public int Id { get; set; } // 物品ID
        public string Name { get; set; } // 物品名称
        public ItemType ItemType { get; set; } // 物品种类
        public Quality Quality { get; set; } // 物品品质
        public string Description { get; set; } // 物品描述
        public int Capacity { get; set; } // 物品叠加数量上限
        public string Icon { get; set; } // 存放物品的图片路径，通过Resources加载
        public ItemDefine Define { get; private set; }

        public int Amount;          //数量
        public int SlotId;        //所处位置

        private ItemInfo? _itemInfo;

        public ItemInfo GetItemInfo()
        {
            _itemInfo ??= new ItemInfo { ItemId = Id };
            _itemInfo.Amount = Amount;
            _itemInfo.SlotId = SlotId;
            return _itemInfo;
        }

        public Item(ItemDefine define, int amount = 1, int slotId = 0)
        {
            Define = define;
            Amount = amount;
            SlotId = slotId;

            ItemType = Define.ItemType switch
            {
                "Consumables" => ItemType.Consumable,
                "tool" => ItemType.Material,
                "equipment" => ItemType.Equipment,
                _ => throw new IndexOutOfRangeException()
            };
            Quality = Define.Quality switch
            {
                "usually" => Quality.Common,
                "非凡" => Quality.Uncommon,
                "rare" => Quality.Rare,
                "epic" => Quality.Epic,
                "legend" => Quality.Legendary,
                "神器" => Quality.Artifact,
                _ => throw new IndexOutOfRangeException()
            };

            Id = Define.ID;
            Name = Define.Name;
            Description = Define.Description;
            Capacity = Define.Capacity;
            Icon = Define.Icon;
        }
    }

}