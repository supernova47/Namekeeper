using System;
using System.Collections.Generic;

namespace SuperNova.ValheimNamekeeper
{
    internal static class InventoryTransaction
    {
        private sealed class CoinState
        {
            internal ItemDrop.ItemData Item;
            internal int Stack, Index;
        }

        // This synchronous section performs no inventory notifications or external callbacks.
        // Publish one Changed notification only after both payment and text have committed.
        internal static void Execute(Inventory inventory, ItemDrop.ItemData item, int cost, Action applyText)
        {
            if (cost < 0) throw new ArgumentOutOfRangeException(nameof(cost));
            if (inventory == null || item == null || applyText == null) throw new ArgumentNullException();
            List<ItemDrop.ItemData> items = inventory.GetAllItems();
            if (!items.Contains(item)) throw new InvalidOperationException("Selected item left inventory");
            var coins = new List<CoinState>();
            long available = 0;
            for (int index = 0; index < items.Count; index++)
            {
                var coin = items[index];
                // Same name/world-level eligibility as vanilla CountItems/RemoveItem.
                if (coin?.m_shared?.m_name != "$item_coins" || coin.m_worldLevel < Game.m_worldLevel || coin.m_stack <= 0) continue;
                coins.Add(new CoinState { Item = coin, Stack = coin.m_stack, Index = index });
                available += coin.m_stack;
            }
            if (available < cost) throw new InvalidOperationException("Insufficient coins");
            var originalData = item.m_customData;
            var savedData = originalData == null ? null : new Dictionary<string, string>(originalData);
            try
            {
                int remaining = cost;
                foreach (CoinState coin in coins)
                {
                    int debit = Math.Min(remaining, coin.Stack);
                    coin.Item.m_stack -= debit;
                    remaining -= debit;
                    if (coin.Item.m_stack == 0) items.Remove(coin.Item);
                    if (remaining == 0) break;
                }
                applyText();
            }
            catch
            {
                // Restore the same coin instances and positions, independent of prefab/space/AddItem.
                foreach (CoinState coin in coins)
                {
                    coin.Item.m_stack = coin.Stack;
                    if (!items.Contains(coin.Item)) items.Insert(Math.Min(coin.Index, items.Count), coin.Item);
                }
                if (originalData != null)
                {
                    originalData.Clear();
                    foreach (var entry in savedData) originalData.Add(entry.Key, entry.Value);
                }
                item.m_customData = originalData;
                throw;
            }
        }
    }
}
