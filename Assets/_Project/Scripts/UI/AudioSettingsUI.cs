using Dangaronpo.Audio;
using Dangaronpo.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dangaronpo.UI
{
    /// <summary>
    /// 音量设置界面。控制 BGM 和 SFX 音量，并用 PlayerPrefs 保存玩家设置。
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class AudioSettingsUI : MonoBehaviour
    {
        private const string BgmVolumeKey = "Settings.BgmVolume";
        private const string SfxVolumeKey = "Settings.SfxVolume";

        [SerializeField] private PlayerModeController playerModeController;
        [SerializeField] private MusicPlayer musicPlayer;
        [SerializeField] private AudioCuePlayer sfxPlayer;
        [SerializeField] private Slider bgmSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private TextMeshProUGUI bgmValueText;
        [SerializeField] private TextMeshProUGUI sfxValueText;
        [SerializeField] private Button quitButton;
        [SerializeField] private KeyCode toggleKey = KeyCode.F1;
        [SerializeField, Range(0f, 1f)] private float defaultBgmVolume = 0.3f;
        [SerializeField, Range(0f, 1f)] private float defaultSfxVolume = 0.9f;

        private CanvasGroup canvasGroup;

        public bool IsOpen { get; private set; }

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            IgnoreLabelTextUsedAsValueText();
            ConfigureSliders();
            LoadAndApplySettings();
            SetVisible(false);
            ValidateReferences();
        }

        private void OnEnable()
        {
            // Slider 的值变化直接驱动音量，同时刷新百分比文本和 PlayerPrefs。
            if (bgmSlider != null)
                bgmSlider.onValueChanged.AddListener(SetBgmVolume);

            if (sfxSlider != null)
                sfxSlider.onValueChanged.AddListener(SetSfxVolume);

            if (quitButton != null)
                quitButton.onClick.AddListener(QuitGame);
        }

        private void OnDisable()
        {
            if (bgmSlider != null)
                bgmSlider.onValueChanged.RemoveListener(SetBgmVolume);

            if (sfxSlider != null)
                sfxSlider.onValueChanged.RemoveListener(SetSfxVolume);

            if (quitButton != null)
                quitButton.onClick.RemoveListener(QuitGame);
        }

        private void Update()
        {
            if (toggleKey != KeyCode.None && Input.GetKeyDown(toggleKey))
                Toggle();

            if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
                Close();
        }

        public void Toggle()
        {
            if (IsOpen)
                Close();
            else
                Open();
        }

        public void Open()
        {
            if (!CanOpen())
                return;

            IsOpen = true;
            SetVisible(true);

            if (playerModeController != null)
                playerModeController.EnterSettings();
        }

        public void Close()
        {
            if (!IsOpen)
                return;

            IsOpen = false;
            SaveSettings();
            SetVisible(false);

            if (playerModeController != null && playerModeController.CurrentMode == PlayerModeController.PlayerMode.Settings)
                playerModeController.ExitSettings();
        }

        public void SetBgmVolume(float targetVolume)
        {
            float value = Mathf.Clamp01(targetVolume);

            if (musicPlayer != null)
                musicPlayer.SetVolume(value);

            RefreshValueText(bgmValueText, value);
            PlayerPrefs.SetFloat(BgmVolumeKey, value);
        }

        public void SetSfxVolume(float targetVolume)
        {
            float value = Mathf.Clamp01(targetVolume);

            if (sfxPlayer != null)
                sfxPlayer.SetVolume(value);

            RefreshValueText(sfxValueText, value);
            PlayerPrefs.SetFloat(SfxVolumeKey, value);
        }

        public void ResetToDefaults()
        {
            SetSliderValueWithoutNotify(bgmSlider, defaultBgmVolume);
            SetSliderValueWithoutNotify(sfxSlider, defaultSfxVolume);
            SetBgmVolume(defaultBgmVolume);
            SetSfxVolume(defaultSfxVolume);
            SaveSettings();
        }

        public void QuitGame()
        {
            SaveSettings();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void LoadAndApplySettings()
        {
            // 没有保存过设置时使用默认音量；主菜单和游戏场景都能复用同一份偏好。
            float bgmVolume = LoadVolumeOrDefault(BgmVolumeKey, defaultBgmVolume);
            float sfxVolume = LoadVolumeOrDefault(SfxVolumeKey, defaultSfxVolume);

            SetSliderValueWithoutNotify(bgmSlider, bgmVolume);
            SetSliderValueWithoutNotify(sfxSlider, sfxVolume);
            SetBgmVolume(bgmVolume);
            SetSfxVolume(sfxVolume);
        }

        private float LoadVolumeOrDefault(string key, float defaultVolume)
        {
            float fallbackVolume = Mathf.Clamp01(defaultVolume);
            float savedVolume = PlayerPrefs.GetFloat(key, fallbackVolume);

            if (float.IsNaN(savedVolume) || float.IsInfinity(savedVolume))
            {
                // PlayerPrefs 偶尔会因为编辑器测试或手动改注册表留下非法值，直接回退默认音量。
                Debug.LogWarning($"{nameof(AudioSettingsUI)} ignored invalid saved volume for {key}.", this);
                PlayerPrefs.DeleteKey(key);
                return fallbackVolume;
            }

            return Mathf.Clamp01(savedVolume);
        }

        private void SaveSettings()
        {
            PlayerPrefs.Save();
        }

        private bool CanOpen()
        {
            if (playerModeController == null)
                return true;

            // 只允许在自由移动或已经打开设置时切换，避免和背包/调查文本抢模式。
            return playerModeController.CurrentMode == PlayerModeController.PlayerMode.FreeLook
                || playerModeController.CurrentMode == PlayerModeController.PlayerMode.Settings;
        }

        private void ConfigureSliders()
        {
            ConfigureSlider(bgmSlider);
            ConfigureSlider(sfxSlider);
        }

        private static void ConfigureSlider(Slider slider)
        {
            if (slider == null)
                return;

            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
        }

        private static void SetSliderValueWithoutNotify(Slider slider, float value)
        {
            if (slider != null)
                slider.SetValueWithoutNotify(Mathf.Clamp01(value));
        }

        private static void RefreshValueText(TextMeshProUGUI valueText, float value)
        {
            if (valueText != null)
                valueText.text = $"{Mathf.RoundToInt(value * 100f)}%";
        }

        private void IgnoreLabelTextUsedAsValueText()
        {
            // SampleScene 的旧设置面板把左侧标签误接到了 ValueText 字段。
            // 这里避免运行时把“音乐音量 / 音效音量”覆盖成百分比，导致标签消失。
            if (IsLabelText(bgmValueText, "音乐音量"))
                bgmValueText = null;

            if (IsLabelText(sfxValueText, "音效音量"))
                sfxValueText = null;
        }

        private static bool IsLabelText(TextMeshProUGUI text, string expectedLabel)
        {
            if (text == null)
                return false;

            string normalizedText = text.text.Replace("\n", string.Empty).Trim();
            return normalizedText == expectedLabel;
        }

        private void SetVisible(bool visible)
        {
            if (canvasGroup == null)
                return;

            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }

        private void ValidateReferences()
        {
            if (playerModeController == null)
                Debug.LogWarning($"{nameof(AudioSettingsUI)} has no Player Mode Controller. Opening settings will not lock player control.", this);

            if (musicPlayer == null)
                Debug.LogError($"{nameof(AudioSettingsUI)} is missing Music Player.", this);

            if (sfxPlayer == null)
                Debug.LogError($"{nameof(AudioSettingsUI)} is missing SFX Player.", this);

            if (bgmSlider == null)
                Debug.LogError($"{nameof(AudioSettingsUI)} is missing BGM Slider.", this);

            if (sfxSlider == null)
                Debug.LogError($"{nameof(AudioSettingsUI)} is missing SFX Slider.", this);
        }
    }
}
