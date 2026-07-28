using System;
using System.Collections.Generic;
using Dangaronpo.Data;
using UnityEngine;

namespace Dangaronpo.Player
{
    /// <summary>
    /// 玩家背包状态。保存拥有的道具、选中的道具和当前手持道具，并通过事件通知 UI/音效刷新。
    /// </summary>
    public class PlayerInventory : MonoBehaviour
    {
        // items 负责保持背包显示顺序，itemCounts 负责堆叠数量。
        private readonly List<ItemDataSO> items = new List<ItemDataSO>();
        private readonly Dictionary<ItemDataSO, int> itemCounts = new Dictionary<ItemDataSO, int>();
        public IReadOnlyList<ItemDataSO> Items => items;
        public ItemDataSO SelectedItem { get; private set; }
        public ItemDataSO HeldItem { get; private set; }

        public event Action ItemsChanged;
        public event Action<ItemDataSO> ItemAdded;
        public event Action<ItemDataSO> SelectedItemChanged;
        public event Action<ItemDataSO> HeldItemChanged;

        public bool AddItem(ItemDataSO item)
        {
            if (item == null)
            {
                Debug.LogError($"{nameof(PlayerInventory)} cannot add a null item.", this);
                return false;
            }

            if (HasItem(item))
            {
                if (!item.Stackable)
                    return false;

                // 可堆叠道具重复获得时只增加数量，不重复创建 slot。
                itemCounts[item]++;
                ItemsChanged?.Invoke();
                ItemAdded?.Invoke(item);

                Debug.Log($"Added item: {item.DisplayName} x{itemCounts[item]}", this);
                return true;
            }

            items.Add(item);
            itemCounts[item] = 1;
            // UI、拾取确认、音效都通过事件监听背包变化，背包本身不直接引用这些系统。
            ItemsChanged?.Invoke();
            ItemAdded?.Invoke(item);

            Debug.Log($"Added item: {item.DisplayName}", this);
            return true;
        }
        public bool HasItem(ItemDataSO item)
        {
            if (item == null)
                return false;

            return items.Contains(item);
        }

        public int GetItemCount(ItemDataSO item)
        {
            if (item == null)
                return 0;

            return itemCounts.TryGetValue(item, out int count) ? count : 0;
        }

        public bool RemoveItem(ItemDataSO item)
        {
            if (item == null)
                return false;

            if (!HasItem(item))
                return false;

            int currentCount = GetItemCount(item);

            if (item.Stackable && currentCount > 1)
            {
                itemCounts[item] = currentCount - 1;
                ItemsChanged?.Invoke();
                return true;
            }

            // 完全移除道具时，需要同步清掉“选中”和“手持”状态，防止引用已不存在的道具。
            bool removed = items.Remove(item);
            itemCounts.Remove(item);

            if (SelectedItem == item)
                SelectItem(null);

            if (HeldItem == item)
                HoldItem(null);

            ItemsChanged?.Invoke();
            return true;
        }

        public void SelectItem(ItemDataSO item)
        {
            if (item != null && !HasItem(item))
            {
                Debug.LogError($"{nameof(PlayerInventory)} cannot select an item it does not contain.", this);
                return;
            }

            SelectedItem = item;
            SelectedItemChanged?.Invoke(SelectedItem);
        }

        public void HoldItem(ItemDataSO item)
        {
            if (item != null && !HasItem(item))
            {
                Debug.LogError($"{nameof(PlayerInventory)} cannot hold an item it does not contain.", this);
                return;
            }

            if (item != null && !item.CanTakeOut)
            {
                // 只有钥匙、钩子、剪刀、子弹这类需要和场景互动的道具才允许取出。
                Debug.LogError($"{nameof(PlayerInventory)} cannot hold {item.DisplayName} because it cannot be taken out.", this);
                return;
            }

            HeldItem = item;
            HeldItemChanged?.Invoke(HeldItem);
        }
    }
}
