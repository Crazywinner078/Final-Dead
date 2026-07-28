using Dangaronpo.Player;
using Dangaronpo.Puzzle;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Dangaronpo.UI
{
    /// <summary>
    /// 保险柜四位数字输入界面。处理数字选择、上下滚动、提交和错误反馈。
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class SafePuzzleUI : MonoBehaviour
    {
        [SerializeField] private PlayerModeController playerModeController;
        [SerializeField] private TextMeshProUGUI[] digitTexts = new TextMeshProUGUI[4];
        [SerializeField] private TextMeshProUGUI feedbackText;
        [SerializeField] private Color normalDigitColor = Color.white;
        [SerializeField] private Color selectedDigitColor = new Color(1f, 0.93f, 0.65f);
        [SerializeField] private float normalDigitScale = 1f;
        [SerializeField] private float selectedDigitScale = 1.18f;
        [SerializeField] private UnityEvent onDigitChanged;

        private readonly int[] digits = new int[4];
        private CanvasGroup canvasGroup;
        private SafePuzzleController currentController;
        private bool isOpen;
        private int selectedDigitIndex;
        private int openedFrame = -1;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            ValidateReferences();
            if (feedbackText != null)
                feedbackText.gameObject.SetActive(false);
            Hide();
        }

        private void Update()
        {
            if (!isOpen)
                return;

            // 防止按 E 打开 UI 的同一帧又立刻提交 0000。
            if (Time.frameCount == openedFrame)
                return;

            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            {
                MoveSelection(-1);
                return;
            }

            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            {
                MoveSelection(1);
                return;
            }

            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            {
                ChangeDigit(1);
                return;
            }

            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            {
                ChangeDigit(-1);
                return;
            }

            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                SubmitCode();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
                Hide();
        }

        public void Show(SafePuzzleController controller)
        {
            if (controller == null)
                return;

            currentController = controller;
            selectedDigitIndex = 0;
            ResetDigits();
            RefreshDigits();

            if (feedbackText != null)
            {
                feedbackText.text = string.Empty;
                feedbackText.gameObject.SetActive(false);
            }

            isOpen = true;
            openedFrame = Time.frameCount;
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            if (playerModeController != null)
                // 输入密码时锁住移动和视角，避免 WASD 同时移动玩家。
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

            if (feedbackText != null)
            {
                feedbackText.text = string.Empty;
                feedbackText.gameObject.SetActive(false);
            }

            if (wasOpen && playerModeController != null)
                playerModeController.ExitReadingText();
        }

        public void ShowFeedback(string message)
        {
            if (feedbackText == null)
                return;

            feedbackText.gameObject.SetActive(true);
            feedbackText.text = message;
        }

        private void SubmitCode()
        {
            if (currentController == null)
                return;

            // UI 只负责收集输入，密码是否正确交给 SafePuzzleController 判断。
            string code = BuildCode();
            currentController.TrySubmitCode(code);
        }

        private string BuildCode()
        {
            char[] digitsAsChars = new char[4];

            for (int i = 0; i < digits.Length; i++)
                digitsAsChars[i] = (char)('0' + Mathf.Clamp(digits[i], 0, 9));

            return new string(digitsAsChars);
        }

        private void ResetDigits()
        {
            for (int i = 0; i < digits.Length; i++)
                digits[i] = 0;
        }

        private void MoveSelection(int direction)
        {
            selectedDigitIndex = (selectedDigitIndex + direction + digits.Length) % digits.Length;
            ClearFeedback();
            RefreshDigits();
        }

        private void ChangeDigit(int delta)
        {
            // 加 10 再取模，可以让 0 往下滚变成 9。
            digits[selectedDigitIndex] = (digits[selectedDigitIndex] + delta + 10) % 10;
            ClearFeedback();
            RefreshDigits();
            onDigitChanged?.Invoke();
        }

        private void RefreshDigits()
        {
            for (int i = 0; i < digitTexts.Length; i++)
            {
                TextMeshProUGUI digitText = digitTexts[i];

                if (digitText == null)
                    continue;

                if (i < digits.Length)
                    digitText.text = digits[i].ToString();

                bool isSelected = i == selectedDigitIndex;
                digitText.color = isSelected ? selectedDigitColor : normalDigitColor;
                digitText.transform.localScale = Vector3.one * (isSelected ? selectedDigitScale : normalDigitScale);
            }
        }

        private void ValidateReferences()
        {
            if (playerModeController == null)
                Debug.LogError($"{nameof(SafePuzzleUI)} is missing Player Mode Controller.", this);

            if (digitTexts == null || digitTexts.Length < 4)
                Debug.LogError($"{nameof(SafePuzzleUI)} needs four digit text references.", this);

            if (feedbackText == null)
                Debug.LogWarning($"{nameof(SafePuzzleUI)} has no Feedback Text. Wrong-code feedback will be hidden.", this);
        }

        private void ClearFeedback()
        {
            if (feedbackText == null)
                return;

            feedbackText.text = string.Empty;
            feedbackText.gameObject.SetActive(false);
        }
    }
}
