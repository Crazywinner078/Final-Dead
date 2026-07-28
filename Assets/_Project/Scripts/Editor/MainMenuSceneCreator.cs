#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Dangaronpo.Audio;
using Dangaronpo.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Dangaronpo.EditorTools
{
    /// <summary>
    /// 编辑器工具：一键生成主菜单场景和基础设置 UI。只在 Unity Editor 中运行，不会进入游戏包体逻辑。
    /// </summary>
    public static class MainMenuSceneCreator
    {
        private const string MenuScenePath = "Assets/_Project/Scenes/MainMenu.unity";
        private const string GameScenePath = "Assets/_Project/Scenes/SampleScene.unity";
        private const string GameSceneName = "SampleScene";

        [MenuItem("Tools/Dangaronpo/Create Main Menu Scene")]
        public static void CreateMainMenuScene()
        {
            Directory.CreateDirectory("Assets/_Project/Scenes");

            // 生成独立 MainMenu scene，避免手动从零搭 Canvas、按钮和音频对象。
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Camera camera = CreateCamera();
            Canvas canvas = CreateCanvas();
            CreateEventSystem();

            AudioCuePlayer sfxPlayer = CreateSfxPlayer();
            MusicPlayer musicPlayer = CreateMusicPlayer();

            MainMenuUI mainMenuUI = CreateMainMenu(canvas.transform, musicPlayer, sfxPlayer);
            CreateBackgroundCameraMarker(camera);

            EditorSceneManager.SaveScene(scene, MenuScenePath);
            // 生成后自动把主菜单和游戏场景加入 Build Settings，保证按钮 LoadScene 能找到目标。
            AddScenesToBuildSettings();
            AssetDatabase.Refresh();

            Object sceneAsset = AssetDatabase.LoadAssetAtPath<Object>(MenuScenePath);
            Selection.activeObject = sceneAsset;

            Debug.Log($"Created main menu scene at {MenuScenePath}.", sceneAsset);
            Debug.Log($"{nameof(MainMenuUI)} is ready on {mainMenuUI.name}. Drag a BGM clip onto MusicPlayer if needed.", mainMenuUI);
        }

        private static Camera CreateCamera()
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            // 菜单场景也需要 AudioListener，否则按钮音效和 BGM 都听不到。
            cameraObject.AddComponent<AudioListener>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.015f, 0.015f, 0.018f);
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            return camera;
        }

        private static Canvas CreateCanvas()
        {
            GameObject canvasObject = new GameObject("Canvas");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static void CreateEventSystem()
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        private static AudioCuePlayer CreateSfxPlayer()
        {
            GameObject audioRoot = new GameObject("AudioRoot");

            // 主菜单自己的 SFXPlayer 只服务菜单音效，切到游戏场景后由游戏场景音频对象接管。
            GameObject sfxObject = new GameObject("SfxPlayer");
            sfxObject.transform.SetParent(audioRoot.transform);
            AudioSource source = sfxObject.AddComponent<AudioSource>();
            AudioCuePlayer cuePlayer = sfxObject.AddComponent<AudioCuePlayer>();
            SetObject(cuePlayer, "audioSource", source);

            return cuePlayer;
        }

        private static MusicPlayer CreateMusicPlayer()
        {
            GameObject audioRoot = GameObject.Find("AudioRoot");

            GameObject musicObject = new GameObject("MusicPlayer");
            musicObject.transform.SetParent(audioRoot.transform);
            AudioSource source = musicObject.AddComponent<AudioSource>();
            MusicPlayer musicPlayer = musicObject.AddComponent<MusicPlayer>();
            SetObject(musicPlayer, "musicSource", source);
            SetFloat(musicPlayer, "volume", 0.3f);

            return musicPlayer;
        }

        private static MainMenuUI CreateMainMenu(Transform canvasTransform, MusicPlayer musicPlayer, AudioCuePlayer sfxPlayer)
        {
            GameObject root = CreateUIObject("MainMenuRoot", canvasTransform);
            Stretch(root.GetComponent<RectTransform>());

            Image background = root.AddComponent<Image>();
            background.color = new Color(0.025f, 0.025f, 0.03f, 1f);

            GameObject mainPanel = CreateUIObject("MainPanel", root.transform);
            RectTransform mainPanelRect = mainPanel.GetComponent<RectTransform>();
            mainPanelRect.anchorMin = new Vector2(0.12f, 0.16f);
            mainPanelRect.anchorMax = new Vector2(0.52f, 0.84f);
            mainPanelRect.offsetMin = Vector2.zero;
            mainPanelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI title = CreateText("Title", mainPanel.transform, "绝命终结室", 76, TextAlignmentOptions.Left);
            RectTransform titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.73f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            title.color = new Color(0.95f, 0.95f, 0.92f);

            TextMeshProUGUI subtitle = CreateText("Subtitle", mainPanel.transform, "A first-person mystery room prototype", 28, TextAlignmentOptions.Left);
            RectTransform subtitleRect = subtitle.GetComponent<RectTransform>();
            subtitleRect.anchorMin = new Vector2(0f, 0.63f);
            subtitleRect.anchorMax = new Vector2(1f, 0.73f);
            subtitleRect.offsetMin = Vector2.zero;
            subtitleRect.offsetMax = Vector2.zero;
            subtitle.color = new Color(0.72f, 0.72f, 0.68f);

            Button startButton = CreateMenuButton("StartButton", mainPanel.transform, "开始游戏", new Vector2(0f, 0.42f), new Vector2(0.62f, 0.53f));
            Button settingsButton = CreateMenuButton("SettingsButton", mainPanel.transform, "设置", new Vector2(0f, 0.28f), new Vector2(0.62f, 0.39f));
            Button quitButton = CreateMenuButton("QuitButton", mainPanel.transform, "退出游戏", new Vector2(0f, 0.14f), new Vector2(0.62f, 0.25f));

            GameObject settingsPanel = CreateSettingsPanel(root.transform, musicPlayer, sfxPlayer);

            MainMenuUI mainMenuUI = root.AddComponent<MainMenuUI>();
            SetString(mainMenuUI, "gameSceneName", GameSceneName);
            SetObject(mainMenuUI, "mainPanel", mainPanel);
            SetObject(mainMenuUI, "settingsUI", settingsPanel.GetComponent<AudioSettingsUI>());
            SetObject(mainMenuUI, "sfxPlayer", sfxPlayer);
            SetObject(mainMenuUI, "musicPlayer", musicPlayer);
            SetFloat(mainMenuUI, "startGameDelay", 0.2f);
            SetBool(mainMenuUI, "fadeOutMusicBeforeStartGame", true);
            SetFloat(mainMenuUI, "startGameMusicFadeOutDuration", 0.8f);

            // 用持久化 UnityEvent 绑定按钮，生成后的 scene 可以直接运行，不需要手动接按钮事件。
            UnityEventTools.AddPersistentListener(startButton.onClick, mainMenuUI.StartGame);
            UnityEventTools.AddPersistentListener(settingsButton.onClick, mainMenuUI.OpenSettings);
            UnityEventTools.AddPersistentListener(quitButton.onClick, mainMenuUI.QuitGame);

            Button backButton = settingsPanel.transform.Find("BackButton")?.GetComponent<Button>();
            if (backButton != null)
                UnityEventTools.AddPersistentListener(backButton.onClick, mainMenuUI.CloseSettings);

            return mainMenuUI;
        }

        private static GameObject CreateSettingsPanel(Transform parent, MusicPlayer musicPlayer, AudioCuePlayer sfxPlayer)
        {
            GameObject panel = CreateUIObject("SettingsPanel", parent);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.3f, 0.22f);
            rect.anchorMax = new Vector2(0.7f, 0.78f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = panel.AddComponent<Image>();
            image.color = new Color(0.055f, 0.055f, 0.065f, 0.96f);

            CanvasGroup canvasGroup = panel.AddComponent<CanvasGroup>();
            AudioSettingsUI settingsUI = panel.AddComponent<AudioSettingsUI>();

            TextMeshProUGUI title = CreateText("SettingsTitle", panel.transform, "设置", 44, TextAlignmentOptions.Center);
            SetRect(title.GetComponent<RectTransform>(), new Vector2(0.08f, 0.78f), new Vector2(0.92f, 0.95f));

            TextMeshProUGUI bgmLabel = CreateText("BgmLabel", panel.transform, "背景音乐", 26, TextAlignmentOptions.Left);
            SetRect(bgmLabel.GetComponent<RectTransform>(), new Vector2(0.12f, 0.61f), new Vector2(0.42f, 0.69f));

            Slider bgmSlider = CreateSlider("BgmSlider", panel.transform, new Vector2(0.42f, 0.61f), new Vector2(0.78f, 0.69f));
            TextMeshProUGUI bgmValue = CreateText("BgmValue", panel.transform, "30%", 24, TextAlignmentOptions.Right);
            SetRect(bgmValue.GetComponent<RectTransform>(), new Vector2(0.79f, 0.61f), new Vector2(0.9f, 0.69f));

            TextMeshProUGUI sfxLabel = CreateText("SfxLabel", panel.transform, "音效", 26, TextAlignmentOptions.Left);
            SetRect(sfxLabel.GetComponent<RectTransform>(), new Vector2(0.12f, 0.47f), new Vector2(0.42f, 0.55f));

            Slider sfxSlider = CreateSlider("SfxSlider", panel.transform, new Vector2(0.42f, 0.47f), new Vector2(0.78f, 0.55f));
            TextMeshProUGUI sfxValue = CreateText("SfxValue", panel.transform, "90%", 24, TextAlignmentOptions.Right);
            SetRect(sfxValue.GetComponent<RectTransform>(), new Vector2(0.79f, 0.47f), new Vector2(0.9f, 0.55f));

            Button resetButton = CreateMenuButton("ResetButton", panel.transform, "重置", new Vector2(0.18f, 0.18f), new Vector2(0.42f, 0.29f));
            Button backButton = CreateMenuButton("BackButton", panel.transform, "返回", new Vector2(0.58f, 0.18f), new Vector2(0.82f, 0.29f));

            Button quitButton = CreateMenuButton("SettingsQuitButton", panel.transform, "退出游戏", new Vector2(0.33f, 0.04f), new Vector2(0.67f, 0.15f));

            SetObject(settingsUI, "musicPlayer", musicPlayer);
            SetObject(settingsUI, "sfxPlayer", sfxPlayer);
            SetObject(settingsUI, "bgmSlider", bgmSlider);
            SetObject(settingsUI, "sfxSlider", sfxSlider);
            SetObject(settingsUI, "bgmValueText", bgmValue);
            SetObject(settingsUI, "sfxValueText", sfxValue);
            SetObject(settingsUI, "quitButton", quitButton);
            SetEnum(settingsUI, "toggleKey", "None");
            SetFloat(settingsUI, "defaultBgmVolume", 0.3f);
            SetFloat(settingsUI, "defaultSfxVolume", 0.9f);

            panel.SetActive(true);
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            UnityEventTools.AddPersistentListener(resetButton.onClick, settingsUI.ResetToDefaults);

            return panel;
        }

        private static Button CreateMenuButton(string name, Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject buttonObject = CreateUIObject(name, parent);
            SetRect(buttonObject.GetComponent<RectTransform>(), anchorMin, anchorMax);

            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.78f, 0.04f, 0.09f, 0.95f);

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;

            TextMeshProUGUI text = CreateText("Label", buttonObject.transform, label, 30, TextAlignmentOptions.Center);
            Stretch(text.GetComponent<RectTransform>());
            text.color = Color.white;

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.86f, 0.86f);
            colors.pressedColor = new Color(0.75f, 0.75f, 0.75f);
            button.colors = colors;

            return button;
        }

        private static Slider CreateSlider(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject sliderObject = CreateUIObject(name, parent);
            SetRect(sliderObject.GetComponent<RectTransform>(), anchorMin, anchorMax);

            Slider slider = sliderObject.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;

            GameObject backgroundObject = CreateUIObject("Background", sliderObject.transform);
            SetRect(backgroundObject.GetComponent<RectTransform>(), new Vector2(0f, 0.38f), new Vector2(1f, 0.62f));
            Image background = backgroundObject.AddComponent<Image>();
            background.color = new Color(0.18f, 0.18f, 0.2f, 1f);

            GameObject fillAreaObject = CreateUIObject("Fill Area", sliderObject.transform);
            SetRect(fillAreaObject.GetComponent<RectTransform>(), new Vector2(0f, 0.38f), new Vector2(1f, 0.62f));

            GameObject fillObject = CreateUIObject("Fill", fillAreaObject.transform);
            Stretch(fillObject.GetComponent<RectTransform>());
            Image fill = fillObject.AddComponent<Image>();
            fill.color = new Color(0.8f, 0.04f, 0.09f, 1f);

            GameObject handleAreaObject = CreateUIObject("Handle Slide Area", sliderObject.transform);
            Stretch(handleAreaObject.GetComponent<RectTransform>());

            GameObject handleObject = CreateUIObject("Handle", handleAreaObject.transform);
            RectTransform handleRect = handleObject.GetComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0f, 0.2f);
            handleRect.anchorMax = new Vector2(0f, 0.8f);
            handleRect.sizeDelta = new Vector2(24f, 0f);
            Image handle = handleObject.AddComponent<Image>();
            handle.color = new Color(0.95f, 0.95f, 0.9f, 1f);

            slider.fillRect = fillObject.GetComponent<RectTransform>();
            slider.handleRect = handleRect;
            slider.targetGraphic = handle;
            return slider;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, string text, int fontSize, TextAlignmentOptions alignment)
        {
            GameObject textObject = CreateUIObject(name, parent);
            TextMeshProUGUI textComponent = textObject.AddComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.fontSize = fontSize;
            textComponent.alignment = alignment;
            textComponent.enableWordWrapping = false;
            return textComponent;
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static void CreateBackgroundCameraMarker(Camera camera)
        {
            GameObject marker = new GameObject("MainMenuSceneNotes");
            marker.transform.position = Vector3.zero;
            marker.hideFlags = HideFlags.HideInHierarchy;
        }

        private static void Stretch(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        private static void SetRect(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        private static void AddScenesToBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(MenuScenePath, true)
            };

            if (File.Exists(GameScenePath))
                scenes.Add(new EditorBuildSettingsScene(GameScenePath, true));

            // 保留其它已有 scene，只把主菜单和游戏场景固定放到前面。
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.path == MenuScenePath || scene.path == GameScenePath)
                    continue;

                scenes.Add(scene);
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void SetObject(Object target, string propertyName, Object value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);

            if (property != null)
            {
                property.objectReferenceValue = value;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetString(Object target, string propertyName, string value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);

            if (property != null)
            {
                property.stringValue = value;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetFloat(Object target, string propertyName, float value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);

            if (property != null)
            {
                property.floatValue = value;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetBool(Object target, string propertyName, bool value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);

            if (property != null)
            {
                property.boolValue = value;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetEnum(Object target, string propertyName, string enumName)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);

            if (property != null && property.propertyType == SerializedPropertyType.Enum)
            {
                for (int i = 0; i < property.enumNames.Length; i++)
                {
                    if (property.enumNames[i] != enumName)
                        continue;

                    property.enumValueIndex = i;
                    serializedObject.ApplyModifiedPropertiesWithoutUndo();
                    return;
                }
            }
        }
    }
}
#endif
