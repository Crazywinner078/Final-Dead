using UnityEngine;

namespace Dangaronpo.Data
{
    /// <summary>
    /// 背包道具的数据资产。道具名称、图标、调查文本、手持模型和可操作能力都在这里配置。
    /// </summary>
    [CreateAssetMenu(fileName = "ItemData", menuName = "Dangaronpo/Items/Item Data")]
    public class ItemDataSO : ScriptableObject
    {
        [SerializeField] private string itemId = "item_id";
        [SerializeField] private string displayName = "Item";

        [SerializeField, TextArea(2, 6)]
        private string description;

        [SerializeField, TextArea(2, 8)]
        private string examineText;

        [SerializeField] private Sprite icon;
        [SerializeField] private Sprite examineImage;
        [SerializeField] private GameObject heldPrefab;
        [SerializeField] private bool canTakeOut = true;
        [SerializeField] private bool canCombine = true;
        [SerializeField] private bool stackable;

        public string ItemId => itemId;
        public string DisplayName => displayName;
        public string Description => description;
        public string ExamineText => string.IsNullOrWhiteSpace(examineText) ? description : examineText;
        public Sprite Icon => icon;
        public Sprite ExamineImage => examineImage;
        public GameObject HeldPrefab => canTakeOut ? heldPrefab : null;
        public bool CanTakeOut => canTakeOut;
        public bool CanCombine => canCombine;
        public bool Stackable => stackable;

        private void OnValidate()
        {
            // 保证每个道具有稳定 id；没有手动填写时就退回到资产名。
            if (string.IsNullOrWhiteSpace(itemId))
                itemId = name;

            // 不能取出的道具不应该再持有手持 prefab，避免 Inspector 上配置互相矛盾。
            if (!canTakeOut)
                heldPrefab = null;
        }

    }
}
