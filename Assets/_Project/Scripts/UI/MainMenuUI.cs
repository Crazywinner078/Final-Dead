using System.Collections;
using Dangaronpo.Audio;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dangaronpo.UI
{
    /// <summary>
    /// 主菜单界面。负责开始游戏、打开设置、返回菜单和退出游戏。
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private string gameSceneName = "SampleScene";
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private AudioSettingsUI settingsUI;
        [SerializeField] private AudioCuePlayer sfxPlayer;
        [SerializeField] private MusicPlayer musicPlayer;
        [SerializeField, Min(0f)] private float startGameDelay = 0.2f;
        [SerializeField] private bool fadeOutMusicBeforeStartGame = true;
        [SerializeField, Min(0f)] private float startGameMusicFadeOutDuration = 0.8f;

        private bool openedSettingsFromMenu;
        private bool isStartingGame;

        private void Update()
        {
            if (!openedSettingsFromMenu)
                return;

            if (settingsUI == null || settingsUI.IsOpen)
                return;

            openedSettingsFromMenu = false;
            SetMainPanelVisible(true);
        }

        public void StartGame()
        {
            if (isStartingGame)
                return;

            if (string.IsNullOrWhiteSpace(gameSceneName))
            {
                Debug.LogError($"{nameof(MainMenuUI)} has no Game Scene Name.", this);
                return;
            }

            if (sfxPlayer != null)
                sfxPlayer.PlayDefault();

            FadeOutMenuMusic();
            isStartingGame = true;
            // 禁用按钮交互，防止玩家连续点击导致重复 LoadScene。
            SetMainPanelInteractable(false);
            StartCoroutine(LoadGameAfterDelay());
        }

        private IEnumerator LoadGameAfterDelay()
        {
            // 给按钮音效留一点播放时间，否则切场景时 AudioSource 会被销毁。
            float loadDelay = GetLoadDelay();

            if (loadDelay > 0f)
                yield return new WaitForSecondsRealtime(loadDelay);

            SceneManager.LoadScene(gameSceneName);
        }

        private void FadeOutMenuMusic()
        {
            if (!fadeOutMusicBeforeStartGame || musicPlayer == null || !musicPlayer.IsPlaying)
                return;

            musicPlayer.StopMusic(startGameMusicFadeOutDuration);
        }

        private float GetLoadDelay()
        {
            if (!fadeOutMusicBeforeStartGame || musicPlayer == null || !musicPlayer.IsPlaying)
                return startGameDelay;

            return Mathf.Max(startGameDelay, startGameMusicFadeOutDuration);
        }

        public void OpenSettings()
        {
            if (settingsUI == null)
            {
                Debug.LogError($"{nameof(MainMenuUI)} is missing Settings UI.", this);
                return;
            }

            openedSettingsFromMenu = true;
            SetMainPanelVisible(false);
            settingsUI.Open();
        }

        public void CloseSettings()
        {
            openedSettingsFromMenu = false;

            if (settingsUI != null)
                settingsUI.Close();

            SetMainPanelVisible(true);
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void SetMainPanelVisible(bool visible)
        {
            if (mainPanel != null)
                mainPanel.SetActive(visible);
        }

        private void SetMainPanelInteractable(bool interactable)
        {
            if (mainPanel == null)
                return;

            CanvasGroup canvasGroup = mainPanel.GetComponent<CanvasGroup>();

            if (canvasGroup == null)
                canvasGroup = mainPanel.AddComponent<CanvasGroup>();

            canvasGroup.interactable = interactable;
            canvasGroup.blocksRaycasts = interactable;
        }
    }
}
