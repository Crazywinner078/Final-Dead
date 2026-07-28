using System.Collections;
using Dangaronpo.Player;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Dangaronpo.UI
{
    /// <summary>
    /// 结局黑屏 UI。触发后立刻遮住画面，再显示结局文字和可选按钮。
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class EndingUI : MonoBehaviour
    {
        [SerializeField] private PlayerModeController playerModeController;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Color backgroundColor = Color.black;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI bodyText;
        [SerializeField] private GameObject buttonRoot;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private string title = "THE END";
        [SerializeField, TextArea(2, 5)] private string body = "房间恢复了寂静。";
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField, Min(0f)] private float textDelay = 0.15f;
        [SerializeField, Min(0f)] private float buttonDelay = 1.2f;

        private CanvasGroup canvasGroup;
        private Coroutine showRoutine;

        public bool IsShowing { get; private set; }

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            BindButtons();
            HideImmediate();
            ValidateReferences();
        }

        private void OnDestroy()
        {
            UnbindButtons();
        }

        public void Show()
        {
            Show(title, body);
        }

        public void Show(string targetTitle, string targetBody)
        {
            if (showRoutine != null)
                StopCoroutine(showRoutine);

            showRoutine = StartCoroutine(ShowRoutine(targetTitle, targetBody));
        }

        public void RestartCurrentScene()
        {
            Time.timeScale = 1f;
            Scene activeScene = SceneManager.GetActiveScene();

            if (activeScene.buildIndex >= 0)
                SceneManager.LoadScene(activeScene.buildIndex);
            else
                SceneManager.LoadScene(activeScene.name);
        }

        public void ReturnToMainMenu()
        {
            if (string.IsNullOrWhiteSpace(mainMenuSceneName))
            {
                Debug.LogError($"{nameof(EndingUI)} has no Main Menu Scene Name.", this);
                return;
            }

            Time.timeScale = 1f;
            SceneManager.LoadScene(mainMenuSceneName);
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private IEnumerator ShowRoutine(string targetTitle, string targetBody)
        {
            IsShowing = true;

            // 黑屏必须立刻出现；文字和按钮可以稍微延后，让扣扳机/枪响的声音先落下来。
            SetPanelVisible(true);
            SetTextVisible(false);
            SetButtonsVisible(false);

            if (backgroundImage != null)
                backgroundImage.color = backgroundColor;

            if (playerModeController != null)
                playerModeController.EnterReadingText();

            if (textDelay > 0f)
                yield return new WaitForSecondsRealtime(textDelay);

            if (titleText != null)
                titleText.text = targetTitle ?? string.Empty;

            if (bodyText != null)
                bodyText.text = targetBody ?? string.Empty;

            SetTextVisible(true);

            float remainingButtonDelay = Mathf.Max(0f, buttonDelay - textDelay);

            if (remainingButtonDelay > 0f)
                yield return new WaitForSecondsRealtime(remainingButtonDelay);

            SetButtonsVisible(true);
            canvasGroup.interactable = true;
            showRoutine = null;
        }

        private void HideImmediate()
        {
            IsShowing = false;
            SetPanelVisible(false);
            SetTextVisible(false);
            SetButtonsVisible(false);
        }

        private void SetPanelVisible(bool visible)
        {
            if (canvasGroup == null)
                return;

            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = visible;
        }

        private void SetTextVisible(bool visible)
        {
            if (titleText != null)
                titleText.gameObject.SetActive(visible);

            if (bodyText != null)
                bodyText.gameObject.SetActive(visible);
        }

        private void SetButtonsVisible(bool visible)
        {
            if (buttonRoot != null)
            {
                buttonRoot.SetActive(visible);
                return;
            }

            if (restartButton != null)
                restartButton.gameObject.SetActive(visible);

            if (mainMenuButton != null)
                mainMenuButton.gameObject.SetActive(visible);

            if (quitButton != null)
                quitButton.gameObject.SetActive(visible);
        }

        private void BindButtons()
        {
            if (restartButton != null)
                restartButton.onClick.AddListener(RestartCurrentScene);

            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(ReturnToMainMenu);

            if (quitButton != null)
                quitButton.onClick.AddListener(QuitGame);
        }

        private void UnbindButtons()
        {
            if (restartButton != null)
                restartButton.onClick.RemoveListener(RestartCurrentScene);

            if (mainMenuButton != null)
                mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);

            if (quitButton != null)
                quitButton.onClick.RemoveListener(QuitGame);
        }

        private void ValidateReferences()
        {
            if (playerModeController == null)
                Debug.LogWarning($"{nameof(EndingUI)} has no Player Mode Controller. Ending screen will not lock player control.", this);

            if (backgroundImage == null)
                Debug.LogWarning($"{nameof(EndingUI)} has no Background Image. Make sure the panel itself is black.", this);

            if (titleText == null)
                Debug.LogWarning($"{nameof(EndingUI)} has no Title Text.", this);

            if (bodyText == null)
                Debug.LogWarning($"{nameof(EndingUI)} has no Body Text.", this);
        }
    }
}
