using Dangaronpo.Player;
using Dangaronpo.Puzzle;
using TMPro;
using UnityEngine;

namespace Dangaronpo.UI
{
    /// <summary>
    /// 笔记本密码输入界面。输入内容交回 LaptopPuzzleController 判断，自己只负责显示和焦点。
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class LaptopPasswordUI : MonoBehaviour
    {
        [SerializeField] private PlayerModeController playerModeController;
        [SerializeField] private TMP_InputField passwordInput;
        [SerializeField] private TextMeshProUGUI feedbackText;

        private CanvasGroup canvasGroup;
        private LaptopPuzzleController currentController;
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

            // 防止打开 UI 的同一帧把进入交互的按键误判成提交。
            if (Time.frameCount == openedFrame)
                return;

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                SubmitPassword();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
                Hide();
        }

        public void Show(LaptopPuzzleController controller)
        {
            if (controller == null)
                return;

            currentController = controller;
            isOpen = true;
            openedFrame = Time.frameCount;

            if (passwordInput != null)
            {
                // 打开后直接聚焦输入框，玩家可以马上敲密码，不需要再点一下鼠标。
                passwordInput.text = string.Empty;
                passwordInput.lineType = TMP_InputField.LineType.SingleLine;
                passwordInput.ActivateInputField();
                passwordInput.Select();
            }

            ClearFeedback();

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
            currentController = null;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            ClearFeedback();

            if (wasOpen && playerModeController != null)
                playerModeController.ExitReadingText();
        }

        public void ShowFeedback(string message)
        {
            if (feedbackText == null)
                return;

            feedbackText.gameObject.SetActive(true);
            feedbackText.text = message;

            if (passwordInput != null)
            {
                passwordInput.text = string.Empty;
                passwordInput.ActivateInputField();
                passwordInput.Select();
            }
        }

        public void SubmitPassword()
        {
            if (currentController == null)
                return;

            // 密码标准化和是否正确由 controller 处理，UI 不保存谜题答案。
            string inputPassword = passwordInput != null ? passwordInput.text : string.Empty;
            currentController.TrySubmitPassword(inputPassword);
        }

        private void ClearFeedback()
        {
            if (feedbackText == null)
                return;

            feedbackText.text = string.Empty;
            feedbackText.gameObject.SetActive(false);
        }

        private void ValidateReferences()
        {
            if (playerModeController == null)
                Debug.LogError($"{nameof(LaptopPasswordUI)} is missing Player Mode Controller.", this);

            if (passwordInput == null)
                Debug.LogError($"{nameof(LaptopPasswordUI)} is missing Password Input.", this);
        }
    }
}
