using System;
using Dangaronpo.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dangaronpo.UI
{
    /// <summary>
    /// 背包里的单个道具格子。只负责显示图标/名称/数量，并把点击事件回传给 InventoryUI。
    /// </summary>
    public class InventorySlotUI : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI quantityText;
        [SerializeField] private GameObject selectedFrame;

        private ItemDataSO itemData;
        private Action<ItemDataSO, RectTransform> clickedCallback;

        private RectTransform rectTransform;

        public ItemDataSO ItemData => itemData;

        private void Awake()
        {
            rectTransform = transform as RectTransform;

            if (button != null)
                button.onClick.AddListener(HandleClicked);
        }

        public void Setup(ItemDataSO item, int quantity, Action<ItemDataSO, RectTransform> onClicked)
        {
            itemData = item;
            clickedCallback = onClicked;

            // slot 自身不保存数量逻辑，只按 InventoryUI 传入的数量刷新显示。
            if (nameText != null)
                nameText.text = GetDisplayName(itemData, quantity);

            if (quantityText != null)
            {
                bool showQuantity = itemData != null && quantity > 1;
                quantityText.gameObject.SetActive(showQuantity);
                quantityText.text = showQuantity ? $"x{quantity}" : string.Empty;
            }

            if (iconImage != null)
            {
                iconImage.sprite = itemData != null ? itemData.Icon : null;
                iconImage.enabled = itemData != null && itemData.Icon != null;
            }

            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            if (selectedFrame != null)
                selectedFrame.SetActive(selected);
        }

        private void HandleClicked()
        {
            if (itemData == null)
                return;

            // 把自己的 RectTransform 一起传回去，方便行动菜单定位到当前 slot 附近。
            clickedCallback?.Invoke(itemData, rectTransform);
        }

        private static string GetDisplayName(ItemDataSO item, int quantity)
        {
            if (item == null)
                return string.Empty;

            if (quantity > 1)
                return $"{item.DisplayName} x{quantity}";

            return item.DisplayName;
        }
    }
}
