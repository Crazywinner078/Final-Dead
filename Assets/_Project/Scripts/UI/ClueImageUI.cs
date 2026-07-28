using Dangaronpo.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dangaronpo.UI
{
    /// <summary>
    /// 全屏线索图片界面。用于展示纸条、相机照片、笔记本线索等图片证据。
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class ClueImageUI : MonoBehaviour
    {
        [SerializeField] private PlayerModeController playerModeController;
        [SerializeField] private Image clueImage;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI confirmText;
        [SerializeField] private string confirmMessage = "[E] Close";

        private CanvasGroup canvasGroup;
        private bool isOpen;
        private int openedFrame = -1;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            ValidateReferences();
            Hide();
        }

        private void Update()
        {
            if (!isOpen)
                return;

            // 防止打开线索图的同一帧被同一个 E 键立刻关闭。
            if (Time.frameCount == openedFrame)
                return;

            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(0))
                Hide();
        }

        public void Show(Sprite image, string title)
        {
            if (image == null)
                return;

            if (clueImage != null)
            {
                clueImage.sprite = image;
                clueImage.enabled = true;
                clueImage.preserveAspect = true;
            }

            if (titleText != null)
                titleText.text = title;

            if (confirmText != null)
                confirmText.text = confirmMessage;

            isOpen = true;
            openedFrame = Time.frameCount;

            // CanvasGroup 控制可见性和是否拦截鼠标，比反复 SetActive 更适合 UI 面板。
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

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
            if (playerModeController == null)
                Debug.LogError($"{nameof(ClueImageUI)} is missing Player Mode Controller.", this);

            if (clueImage == null)
                Debug.LogError($"{nameof(ClueImageUI)} is missing Clue Image.", this);
        }
    }
}
