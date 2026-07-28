using Dangaronpo.Data;
using Dangaronpo.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dangaronpo.UI
{
    /// <summary>
    /// 获得道具确认界面。监听 PlayerInventory.ItemAdded，任何来源获得道具都会弹出确认感。
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class PickupConfirmUI : MonoBehaviour
    {
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private PlayerModeController playerModeController;
        [SerializeField] private Image itemImage;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI confirmText;
        [SerializeField] private string confirmMessage = "[E] Confirm";

        private CanvasGroup canvasGroup;
        private bool isOpen;
        private int openedFrame = -1;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            ValidateReferences();
            Hide();
        }

        private void OnEnable()
        {
            if (playerInventory != null)
                // 统一监听背包事件，所以拾取、合成、机关奖励都会进入同一套确认流程。
                playerInventory.ItemAdded += Show;
        }

        private void OnDisable()
        {
            if (playerInventory != null)
                playerInventory.ItemAdded -= Show;
        }

        private void Update()
        {
            if (!isOpen)
                return;

            // 防止按 E 拾取的同一帧，又被确认 UI 读取成关闭输入。
            if (Time.frameCount == openedFrame)
                return;

            if (Input.GetKeyDown(KeyCode.E))
                Hide();
        }

        public void Show(ItemDataSO item)
        {
            if (item == null)
                return;

            if (itemImage != null)
            {
                itemImage.sprite = item.Icon;
                itemImage.enabled = item.Icon != null;
                itemImage.preserveAspect = true;
            }

            if (titleText != null)
                titleText.text = item.DisplayName;

            if (confirmText != null)
                confirmText.text = confirmMessage;

            isOpen = true;
            openedFrame = Time.frameCount;
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            if (playerModeController != null)
                playerModeController.EnterReadingText();
        }

        public void Hide()
        {
            bool wasOpen = isOpen;
            isOpen = false;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            if (wasOpen && playerModeController != null)
                playerModeController.ExitReadingText();
        }

        private void ValidateReferences()
        {
            if (playerInventory == null)
                Debug.LogError($"{nameof(PickupConfirmUI)} is missing Player Inventory.", this);

            if (playerModeController == null)
                Debug.LogError($"{nameof(PickupConfirmUI)} is missing Player Mode Controller.", this);

            if (itemImage == null)
                Debug.LogError($"{nameof(PickupConfirmUI)} is missing Item Image.", this);

            if (titleText == null)
                Debug.LogError($"{nameof(PickupConfirmUI)} is missing Title Text.", this);

            if (confirmText == null)
                Debug.LogError($"{nameof(PickupConfirmUI)} is missing Confirm Text.", this);
        }
    }
}
