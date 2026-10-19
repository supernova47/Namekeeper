using System.Collections.Generic;

// Minimal data doubles for fault injection. These are not Valheim runtime tests.
internal static class Game { internal static int m_worldLevel; }
internal sealed class Inventory
{
    internal readonly List<ItemDrop.ItemData> Items = new List<ItemDrop.ItemData>();
    public List<ItemDrop.ItemData> GetAllItems() => Items;
}
internal sealed class ItemDrop
{
    internal sealed class ItemData
    {
        public SharedData m_shared = new SharedData();
        public int m_stack = 1, m_worldLevel, m_quality = 3, m_variant = 2;
        public float m_durability = 41.5f;
        public Dictionary<string, string> m_customData = new Dictionary<string, string>();
        internal sealed class SharedData { public string m_name = "$axe"; }
    }
}

internal sealed class Piece
{
    internal sealed class Requirement
    {
        public ItemDrop m_resItem;
        public int m_amount, m_extraAmountOnlyOneIngredient, m_amountPerLevel;
        public bool m_upgraderResource, m_recover;
    }
}
