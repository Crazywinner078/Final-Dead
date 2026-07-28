using Dangaronpo.Data;
using TMPro;
using UnityEngine;

namespace Dangaronpo.UI
{
    /// <summary>
    /// 背包调查用的小提示面板。点击“调查”后固定显示在对应物品格子的右下方。
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class InventoryTooltipUI : MonoBehaviour
    {
        [SerializeField] private RectTransform tooltipRoot;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private Vector2 offset = new Vector2(18f, -18f);

        private CanvasGroup canvasGroup;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();

            if (tooltipRoot == null)
                tooltipRoot = transform as RectTransform;

            Hide();
        }

        public void Show(ItemDataSO item)
        {
            Show(item, null);
        }

        public void Show(ItemDataSO item, RectTransform anchor)
        {
            if (item == null)
                return;

            ShowText(item.DisplayName, item.Description, anchor);
        }

        public void ShowText(string title, string description)
        {
            ShowText(title, description, null);
        }

        public void ShowText(string title, string description, RectTransform anchor)
        {
            if (nameText != null)
                nameText.text = title ?? string.Empty;

            if (descriptionText != null)
                descriptionText.text = description ?? string.Empty;

            canvasGroup.alpha = 1f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            if (anchor != null)
                SetPosition(anchor);

            if (tooltipRoot != null)
                tooltipRoot.SetAsLastSibling();
        }

        public void Hide()
        {
            if (canvasGroup == null)
                return;

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        private void SetPosition(RectTransform anchor)
        {
            if (tooltipRoot == null || anchor == null)
                return;

            Vector3[] corners = new Vector3[4];
            anchor.GetWorldCorners(corners);

            // GetWorldCorners: 0 左下，1 左上，2 右上，3 右下。
            Vector3 bottomRight = corners[3];
            tooltipRoot.position = bottomRight + new Vector3(offset.x, offset.y, 0f);
        }
    }
}
