using UnityEngine;

namespace Dangaronpo.Data
{
    /// <summary>
    /// 两个背包道具合成一个新道具的配方。顺序不敏感，例如 A+B 和 B+A 都能匹配。
    /// </summary>
    [CreateAssetMenu(fileName = "ItemCombinationRecipe", menuName = "Dangaronpo/Items/Combination Recipe")]
    public class ItemCombinationRecipeSO : ScriptableObject
    {
        [SerializeField] private ItemDataSO firstItem;
        [SerializeField] private ItemDataSO secondItem;
        [SerializeField] private ItemDataSO resultItem;
        [SerializeField] private bool consumeFirstItem = true;
        [SerializeField] private bool consumeSecondItem = true;

        public ItemDataSO FirstItem => firstItem;
        public ItemDataSO SecondItem => secondItem;
        public ItemDataSO ResultItem => resultItem;
        public bool ConsumeFirstItem => consumeFirstItem;
        public bool ConsumeSecondItem => consumeSecondItem;

        public bool Matches(ItemDataSO itemA, ItemDataSO itemB)
        {
            if (itemA == null || itemB == null)
                return false;

            // 合成时玩家点击两个 slot 的顺序不重要。
            return itemA == firstItem && itemB == secondItem
                || itemA == secondItem && itemB == firstItem;
        }

        public bool ShouldConsume(ItemDataSO item)
        {
            if (item == firstItem)
                return consumeFirstItem;

            if (item == secondItem)
                return consumeSecondItem;

            return false;
        }
    }
}
