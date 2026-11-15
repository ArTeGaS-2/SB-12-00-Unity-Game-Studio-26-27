using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed partial class GameplayPauseMenuController : MonoBehaviour
{
    const string MainMenuScene = "MainMenu";
    const string ReturnToSubmenuKey = "MenuOpenSubmenuOnLoadV1";
    const string LanguagePrefKey = "MenuLanguageV2";
    const string QualityPrefKey = "MenuQualityV1";
    const string BrightnessPrefKey = "MenuBrightnessV1";
    const string MusicVolumePrefKey = "MenuMusicVolumeV2";
    const string FpsPrefKey = "MenuFpsLimitV1";
    const string FullscreenPrefKey = "MenuFullscreenV1";

    static readonly int[] FpsOptions = { 30, 60, 120, 144, -1 };
    static readonly Color Amber = new Color(1f, 0.59f, 0.055f, 1f);
    static readonly Color HotAmber = new Color(1f, 0.78f, 0.18f, 1f);
    static readonly Color Rust = new Color(0.62f, 0.19f, 0.018f, 1f);
    static readonly Color Dark = new Color(0.018f, 0.012f, 0.009f, 0.98f);
    static readonly Color Raised = new Color(0.065f, 0.038f, 0.021f, 1f);
    static readonly Color Muted = new Color(0.72f, 0.46f, 0.24f, 1f);

    Font uiFont;
    GameObject pauseOverlay;
    GameObject pausePage;
    GameObject settingsPage;
    Text qualityValue;
    Text brightnessValue;
    Text musicValue;
    Text fpsValue;
    Text fullscreenValue;
    Slider brightnessSlider;
    Slider musicSlider;
    Image brightnessShade;
    CanvasGroup transitionFade;

    int qualityPreset;
    int fpsOption;
    float brightness;
    float musicVolume;
    bool fullscreen;
    bool paused;
    bool leaving;

    void Awake()
    {
        Application.runInBackground = true;
        uiFont = Resources.Load<Font>("Fonts/RobotoMono-Regular") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        gameplayAmbient = RenderSettings.ambientLight;
        EnsureCamera();
        EnsureEventSystem();
        LoadSettings();
        BuildInterface();
        ApplyAllSettings();
        SetPaused(false);
    }

    void Update()
    {
        AnimatePause();
        if (leaving || !Input.GetKeyDown(KeyCode.Escape)) return;

        if (settingsPage != null && settingsPage.activeSelf)
            ShowPausePage();
        else
            SetPaused(!paused);
    }

    void OnDestroy()
    {
        if(paused && !leaving)RestorePausedState();
    }

    void EnsureCamera()
    {
        if (Camera.main != null) return;

        var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        var camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.011f, 0.008f, 0.007f, 1f);
        camera.transform.position = new Vector3(0f, 0f, -10f);
    }

    void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        eventSystem.transform.SetParent(transform, false);
    }

    void LoadSettings()
    {
        qualityPreset = Mathf.Clamp(PlayerPrefs.GetInt(QualityPrefKey, 1), 0, 2);
        brightness = Mathf.Clamp01(PlayerPrefs.GetFloat(BrightnessPrefKey, 0.5f));
        musicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolumePrefKey, 0.55f));
        fpsOption = Mathf.Clamp(PlayerPrefs.GetInt(FpsPrefKey, 1), 0, FpsOptions.Length - 1);
        fullscreen = PlayerPrefs.GetInt(FullscreenPrefKey, Screen.fullScreen ? 1 : 0) == 1;
        vSync=PlayerPrefs.GetInt("MenuVSyncV1",0)==1;
    }

    void SaveSettings()
    {
        PlayerPrefs.SetInt(QualityPrefKey, qualityPreset);
        PlayerPrefs.SetFloat(BrightnessPrefKey, brightness);
        PlayerPrefs.SetFloat(MusicVolumePrefKey, musicVolume);
        PlayerPrefs.SetInt(FpsPrefKey, fpsOption);
        PlayerPrefs.SetInt(FullscreenPrefKey, fullscreen ? 1 : 0);
        PlayerPrefs.SetInt("MenuVSyncV1",vSync?1:0);
        PlayerPrefs.Save();
    }

    void BuildInterface()
    {
        var canvasObject = new GameObject("Pause Menu Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        canvasObject.AddComponent<MenuKeyboardNavigation>();
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        scaler.matchWidthOrHeight = 0.5f;

        brightnessShade = CreateImage("Brightness Shade", canvasObject.transform, new Color(0f, 0f, 0f, 0f));
        Stretch(brightnessShade.rectTransform);
        brightnessShade.raycastTarget = false;

        pauseHudButton = CreateButton(
            "Stop Pause Button",
            canvasObject.transform,
            new Vector2(0f, 1f),
            new Vector2(255f, 72f),
            new Vector2(30f, -30f),
            L("■  СТОП / ПАУЗА", "■  СТОП / ПАУЗА", "■  STOP / PAUSE"),
            () => SetPaused(true),
            new Vector2(0f, 1f));

        pauseOverlay = CreateImage("Pause Overlay", canvasObject.transform, new Color(0f, 0f, 0f, 0.84f)).gameObject;
        Stretch(pauseOverlay.GetComponent<RectTransform>());

        pauseGroup = pauseOverlay.AddComponent<CanvasGroup>();
        AddDecoration(pauseOverlay.transform);

        var frame = CreateImage("Pause Frame", pauseOverlay.transform, Rust);
        Place(frame.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(900f, 840f), Vector2.zero);
        pauseShell = frame.rectTransform;
        var card = CreateImage("Pause Card", frame.transform, Dark);
        Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(886f, 826f), Vector2.zero);

        var title = CreateText("Pause Title", card.transform, L("ПАУЗА", "ПАУЗА", "PAUSED"), 64, HotAmber);
        Place(title.rectTransform, new Vector2(0.5f, 0.88f), new Vector2(650f, 95f), Vector2.zero);
        title.fontStyle = FontStyle.Bold;
        pauseHeading = title;
        var subtitle = CreateText("Pause Subtitle", card.transform, L("УЗЕЛ 67 // УПРАВЛЕНИЕ ОСТАНОВЛЕНО", "ВУЗОЛ 67 // КЕРУВАННЯ ЗУПИНЕНО", "NODE 67 // CONTROL SUSPENDED"), 17, Muted);
        Place(subtitle.rectTransform, new Vector2(0.5f, 0.79f), new Vector2(650f, 40f), Vector2.zero);
        subtitle.fontStyle = FontStyle.Bold;
        pauseSubtitle = subtitle;

        pausePage = CreatePage("Pause Page", card.transform);
        settingsPage = CreatePage("Settings Page", card.transform);

        CreateButton("Continue", pausePage.transform, new Vector2(0.5f, 0.66f), new Vector2(620f, 96f), Vector2.zero,
            L("ПРОДОЛЖИТЬ", "ПРОДОВЖИТИ", "CONTINUE"), () => SetPaused(false));
        CreateButton("Settings", pausePage.transform, new Vector2(0.5f, 0.47f), new Vector2(620f, 96f), Vector2.zero,
            L("НАСТРОЙКИ", "НАЛАШТУВАННЯ", "SETTINGS"), ShowSettingsPage);
        CreateButton("Exit To Submenu", pausePage.transform, new Vector2(0.5f, 0.28f), new Vector2(620f, 96f), Vector2.zero,
            L("ВЫЙТИ В ПОДМЕНЮ", "ВИЙТИ ДО ПІДМЕНЮ", "EXIT TO SUBMENU"), ExitToSubmenu);

        var hint = CreateText("Pause Hint", pausePage.transform, L("ESC — ПРОДОЛЖИТЬ", "ESC — ПРОДОВЖИТИ", "ESC — CONTINUE"), 16, Muted);
        Place(hint.rectTransform, new Vector2(0.5f, 0.11f), new Vector2(570f, 38f), Vector2.zero);

        BuildSettingsPage();

        var fadeImage = CreateImage("Scene Transition", canvasObject.transform, Color.black);
        Stretch(fadeImage.rectTransform);
        transitionFade = fadeImage.gameObject.AddComponent<CanvasGroup>();
        transitionFade.alpha = 0f;
        transitionFade.blocksRaycasts = false;
        fadeImage.raycastTarget = true;

        pauseOverlay.SetActive(false);
        settingsPage.SetActive(false);
    }

    void BuildSettingsPage()
    {
        CreateSettingRow(settingsPage.transform, 0.64f, L("КАЧЕСТВО", "ЯКІСТЬ", "QUALITY"), out qualityValue,
            () => ChangeQuality(-1), () => ChangeQuality(1));

        brightnessSlider = CreateSlider("Brightness", settingsPage.transform, 0.49f, value =>
        {
            brightness = value;
            ApplyBrightness();
            SaveSettings();
            RefreshSettingsLabels();
        });
        CreateSettingLabel(settingsPage.transform, 0.545f, L("ЯРКОСТЬ", "ЯСКРАВІСТЬ", "BRIGHTNESS"), out brightnessValue);

        musicSlider = CreateSlider("Music", settingsPage.transform, 0.36f, value =>
        {
            musicVolume = value;
            ApplyMusicVolume();
            SaveSettings();
            RefreshSettingsLabels();
        });
        CreateSettingLabel(settingsPage.transform, 0.415f, L("ГРОМКОСТЬ", "ГУЧНІСТЬ", "VOLUME"), out musicValue);

        CreateSettingRow(settingsPage.transform, 0.265f, "FPS", out fpsValue,
            () => ChangeFps(-1), () => ChangeFps(1));
        CreateSettingRow(settingsPage.transform, 0.17f, L("ЭКРАН", "ЕКРАН", "DISPLAY"), out fullscreenValue,
            ToggleFullscreen, ToggleFullscreen);

        CreateButton("Back From Settings", settingsPage.transform, new Vector2(0.5f, 0.065f), new Vector2(470f, 58f), Vector2.zero,
            L("НАЗАД К ПАУЗЕ", "НАЗАД ДО ПАУЗИ", "BACK TO PAUSE"), ShowPausePage);
    }

    void AddDecoration(Transform parent)
    {
        var top = CreateImage("Top Rail", parent, new Color(0.7f, 0.22f, 0.015f, 0.8f));
        Place(top.rectTransform, new Vector2(0.5f, 0.93f), new Vector2(1360f, 3f), Vector2.zero);
        var bottom = CreateImage("Bottom Rail", parent, new Color(0.7f, 0.22f, 0.015f, 0.8f));
        Place(bottom.rectTransform, new Vector2(0.5f, 0.07f), new Vector2(1360f, 3f), Vector2.zero);
        var code = CreateText("Node Code", parent, "67 // EMERGENCY CONTROL", 18, new Color(1f, 0.43f, 0.04f, 0.65f));
        Place(code.rectTransform, new Vector2(0.5f, 0.965f), new Vector2(600f, 32f), Vector2.zero);
        code.fontStyle = FontStyle.Bold;
    }

    void SetPaused(bool value)
    {
        if (leaving || paused==value) return;
        if(value)
        {
            resumeTimeScale=Time.timeScale;resumeAudioPause=AudioListener.pause;
            resumeCursorLock=Cursor.lockState;resumeCursorVisible=Cursor.visible;
            Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        }
        else RestorePausedState();
        paused = value;
        if(paused){Time.timeScale=0;AudioListener.pause=true;pauseGroup.alpha=0;pauseShell.localScale=Vector3.one*.975f;}
        pauseOverlay.SetActive(paused);
        if(pauseHudButton!=null)pauseHudButton.gameObject.SetActive(!paused);
        if (paused) ShowPausePage();
    }

    void ShowPausePage()
    {
        pausePage.SetActive(true);
        settingsPage.SetActive(false);
        RefreshPauseHeader(false);
    }

    void ShowSettingsPage()
    {
        pausePage.SetActive(false);
        settingsPage.SetActive(true);
        RefreshPauseHeader(true);
        RefreshSettingsLabels();
    }

    void ChangeQuality(int direction)
    {
        qualityPreset = Wrap(qualityPreset + direction, 3);
        ApplyQuality();
        SaveSettings();
        RefreshSettingsLabels();
    }

    void ChangeFps(int direction)
    {
        fpsOption = Wrap(fpsOption + direction, FpsOptions.Length);
        vSync=false;
        ApplyFrameRate();
        SaveSettings();
        RefreshSettingsLabels();
    }

    void ToggleFullscreen()
    {
        fullscreen = !fullscreen;
        ApplyFullscreen();
        SaveSettings();
        RefreshSettingsLabels();
    }

    void ApplyAllSettings()
    {
        ApplyQuality();
        ApplyBrightness();
        ApplyMusicVolume();
        ApplyFrameRate();
        ApplyFullscreen();
        RefreshSettingsLabels();
    }

    void ApplyQuality()
    {
        MenuRenderQuality.Apply(qualityPreset);
    }

    void ApplyBrightness()
    {
        if (brightnessShade != null)
            brightnessShade.color = new Color(0f, 0f, 0f, Mathf.Lerp(0.42f, 0f, brightness));
        RenderSettings.ambientLight = gameplayAmbient * Mathf.Lerp(0.55f, 1.35f, brightness);
    }

    void ApplyMusicVolume()
    {
        var theme=Resources.Load<AudioClip>("Audio/Menu/MenuTheme");
        foreach (var source in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
            if((theme!=null && source.clip==theme) || (source.loop && source.spatialBlend<0.01f && source.name.IndexOf("music",System.StringComparison.OrdinalIgnoreCase)>=0))source.volume=musicVolume;
    }

    void ApplyFrameRate()
    {
        QualitySettings.vSyncCount = vSync ? 1 : 0;
        Application.targetFrameRate = vSync ? -1 : FpsOptions[fpsOption];
    }

    void ApplyFullscreen()
    {
#if !UNITY_EDITOR
        Screen.fullScreenMode = fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        Screen.fullScreen = fullscreen;
#endif
    }

    void RefreshSettingsLabels()
    {
        if (qualityValue == null) return;
        string[] qualityNames =
        {
            L("НИЗКОЕ", "НИЗЬКА", "LOW"),
            L("СРЕДНЕЕ", "СЕРЕДНЯ", "MEDIUM"),
            L("ВЫСОКОЕ", "ВИСОКА", "HIGH")
        };
        qualityValue.text = qualityNames[qualityPreset];
        brightnessValue.text = Mathf.RoundToInt(brightness * 100f) + "%";
        musicValue.text = Mathf.RoundToInt(musicVolume * 100f) + "%";
        fpsValue.text = vSync ? "V-SYNC" : FpsOptions[fpsOption] < 0 ? L("БЕЗ ЛИМИТА", "БЕЗ ЛІМІТУ", "UNLIMITED") : FpsOptions[fpsOption].ToString();
        fullscreenValue.text = fullscreen ? L("ПОЛНЫЙ", "ПОВНИЙ", "FULLSCREEN") : L("ОКНО", "ВІКНО", "WINDOWED");

        if (brightnessSlider != null) brightnessSlider.SetValueWithoutNotify(brightness);
        if (musicSlider != null) musicSlider.SetValueWithoutNotify(musicVolume);
    }

    void ExitToSubmenu()
    {
        if (!leaving) StartCoroutine(ExitRoutine());
    }

    IEnumerator ExitRoutine()
    {
        leaving = true;
        AudioListener.pause = false;
        Time.timeScale = 1f;
        Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        paused=false;
        PlayerPrefs.SetInt(ReturnToSubmenuKey, 1);
        PlayerPrefs.Save();

        transitionFade.blocksRaycasts = true;
        float elapsed = 0f;
        while (elapsed < 0.45f)
        {
            elapsed += Time.unscaledDeltaTime;
            transitionFade.alpha = Mathf.Clamp01(elapsed / 0.45f);
            yield return null;
        }

        SceneManager.LoadScene(MainMenuScene);
    }

    void CreateSettingRow(Transform parent, float y, string labelText, out Text valueText, UnityEngine.Events.UnityAction previous, UnityEngine.Events.UnityAction next)
    {
        var label = CreateText(labelText + " Label", parent, labelText, 18, Muted);
        Place(label.rectTransform, new Vector2(0.27f, y), new Vector2(230f, 50f), Vector2.zero);
        label.alignment = TextAnchor.MiddleLeft;
        label.fontStyle = FontStyle.Bold;

        CreateButton(labelText + " Previous", parent, new Vector2(0.53f, y), new Vector2(62f, 54f), Vector2.zero, "<", previous);
        valueText = CreateText(labelText + " Value", parent, "", 18, HotAmber);
        Place(valueText.rectTransform, new Vector2(0.70f, y), new Vector2(210f, 50f), Vector2.zero);
        valueText.fontStyle = FontStyle.Bold;
        CreateButton(labelText + " Next", parent, new Vector2(0.88f, y), new Vector2(62f, 54f), Vector2.zero, ">", next);
    }

    void CreateSettingLabel(Transform parent, float y, string labelText, out Text valueText)
    {
        var label = CreateText(labelText + " Label", parent, labelText, 18, Muted);
        Place(label.rectTransform, new Vector2(0.27f, y), new Vector2(230f, 44f), Vector2.zero);
        label.alignment = TextAnchor.MiddleLeft;
        label.fontStyle = FontStyle.Bold;
        valueText = CreateText(labelText + " Value", parent, "", 18, HotAmber);
        Place(valueText.rectTransform, new Vector2(0.84f, y), new Vector2(130f, 44f), Vector2.zero);
        valueText.alignment = TextAnchor.MiddleRight;
        valueText.fontStyle = FontStyle.Bold;
    }

    Slider CreateSlider(string name, Transform parent, float y, UnityEngine.Events.UnityAction<float> changed)
    {
        var background = CreateImage(name + " Background", parent, new Color(0.18f, 0.07f, 0.018f, 1f));
        Place(background.rectTransform, new Vector2(0.60f, y), new Vector2(460f, 12f), Vector2.zero);
        var fillArea = CreateRect(name + " Fill Area", background.transform);
        Stretch(fillArea);
        fillArea.offsetMin = new Vector2(4f, 3f);
        fillArea.offsetMax = new Vector2(-4f, -3f);
        var fill = CreateImage(name + " Fill", fillArea, Amber);
        Stretch(fill.rectTransform);
        var handle = CreateImage(name + " Handle", background.transform, HotAmber);
        Place(handle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(14f, 30f), Vector2.zero);

        var slider = background.gameObject.AddComponent<Slider>();
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.onValueChanged.AddListener(changed);
        return slider;
    }

    Button CreateButton(string name, Transform parent, Vector2 anchor, Vector2 size, Vector2 position, string text, UnityEngine.Events.UnityAction action, Vector2? pivot = null)
    {
        var frame = CreateImage(name, parent, Rust);
        Place(frame.rectTransform, anchor, size, position, pivot ?? new Vector2(0.5f, 0.5f));
        var face = CreateImage("Face", frame.transform, Raised);
        Place(face.rectTransform, new Vector2(0.5f, 0.5f), size - new Vector2(8f, 8f), Vector2.zero);

        var button = frame.gameObject.AddComponent<Button>();
        button.targetGraphic = face;
        button.transition = Selectable.Transition.ColorTint;
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.32f, 1.18f, 0.86f, 1f);
        colors.pressedColor = new Color(0.82f, 0.68f, 0.45f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.fadeDuration = 0.09f;
        button.colors = colors;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.onClick.AddListener(action);

        var label = CreateText("Label", face.transform, text, Mathf.RoundToInt(Mathf.Clamp(size.y * 0.30f, 16f, 27f)), Amber);
        Stretch(label.rectTransform);
        label.fontStyle = FontStyle.Bold;
        label.raycastTarget = false;
        return button;
    }

    GameObject CreatePage(string name, Transform parent)
    {
        var page = new GameObject(name, typeof(RectTransform));
        page.transform.SetParent(parent, false);
        Stretch(page.GetComponent<RectTransform>());
        return page;
    }

    Image CreateImage(string name, Transform parent, Color color)
    {
        var gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        gameObject.transform.SetParent(parent, false);
        var image = gameObject.GetComponent<Image>();
        image.color = color;
        return image;
    }

    Text CreateText(string name, Transform parent, string value, int size, Color color)
    {
        var gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        gameObject.transform.SetParent(parent, false);
        var text = gameObject.GetComponent<Text>();
        text.font = uiFont;
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;
        return text;
    }

    static RectTransform CreateRect(string name, Transform parent)
    {
        var gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        return gameObject.GetComponent<RectTransform>();
    }

    static void Place(RectTransform rect, Vector2 anchor, Vector2 size, Vector2 position, Vector2? pivot = null)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot ?? new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        rect.localScale = Vector3.one;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    string L(string russian, string ukrainian, string english)
    {
        int language = Mathf.Clamp(PlayerPrefs.GetInt(LanguagePrefKey, 0), 0, 2);
        return language == 1 ? ukrainian : language == 2 ? english : russian;
    }

    static int Wrap(int value, int count)
    {
        if (value < 0) return count - 1;
        if (value >= count) return 0;
        return value;
    }
}
