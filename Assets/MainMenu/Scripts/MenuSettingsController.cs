using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public partial class MenuSettingsController : MonoBehaviour
{
    const string LanguagePrefKey = "MenuLanguageV2";
    const string QualityPrefKey = "MenuQualityV1";
    const string BrightnessPrefKey = "MenuBrightnessV1";
    // V2 resets the value once because the first audio import left a silent 0% preference behind.
    const string MusicVolumePrefKey = "MenuMusicVolumeV2";
    const string FpsPrefKey = "MenuFpsLimitV1";
    const string VSyncPrefKey = "MenuVSyncV1";
    const string FullscreenPrefKey = "MenuFullscreenV1";
    const string MenuFxQualityPrefKey = "MenuFxQualityV1";
    const string MenuFxIntensityPrefKey = "MenuFxIntensityV1";
    const string MenuGlitchEnabledPrefKey = "MenuGlitchEnabledV1";
    const string MenuGlitchNoisePrefKey = "MenuGlitchNoiseV1";
    static readonly int[] FpsOptions = { 30, 60, 120, 144, -1 };

    public Image menuLogo;
    public Image playButtonImage;
    public Image settingsButtonImage;
    public Image exitButtonImage;
    public Button settingsButton;
    public Font uiFont;

    [Header("Menu audio")]
    public AudioClip menuMusicClip;
    public AudioSource menuMusicSource;

    [Header("Russian art")]
    public Sprite russianLogo;
    public Sprite russianPlay;
    public Sprite russianSettings;
    public Sprite russianExit;

    [Header("Ukrainian art")]
    public Sprite ukrainianLogo;
    public Sprite ukrainianPlay;
    public Sprite ukrainianSettings;
    public Sprite ukrainianExit;

    [Header("English art")]
    public Sprite englishLogo;
    public Sprite englishPlay;
    public Sprite englishSettings;
    public Sprite englishExit;

    enum Language { Russian, Ukrainian, English }

    Language language;
    int qualityPreset;
    int fpsOption;
    float brightness;
    float musicVolume;
    bool vSync;
    bool fullscreen;
    int menuFxQuality;
    float menuFxIntensity;
    bool menuGlitchEnabled;
    float menuGlitchNoise;

    GameObject settingsPanel;
    RectTransform settingsCard;
    CanvasGroup settingsCanvasGroup;
    Text settingsHeader;
    Text graphicsHeader;
    Text interfaceHeader;
    Text effectsHeader;
    Text qualityLabel;
    Text qualityValue;
    Text brightnessLabel;
    Text brightnessValue;
    Text musicLabel;
    Text musicValue;
    Text fpsLabel;
    Text fpsValue;
    Text vSyncLabel;
    Text vSyncValue;
    Text languageHeader;
    Text fullscreenLabel;
    Text fullscreenValue;
    Text menuFxQualityLabel;
    Text menuFxQualityValue;
    Text menuFxIntensityLabel;
    Text menuFxIntensityValue;
    Text menuGlitchEnabledLabel;
    Text menuGlitchEnabledValue;
    Text menuGlitchNoiseLabel;
    Text menuGlitchNoiseValue;
    Text menuFxDescription;
    Text statsHeader;
    Text statsValue;
    Text closeLabel;
    Text resetLabel;
    Text saveHint;
    Slider brightnessSlider;
    Slider musicVolumeSlider;
    Slider menuFxIntensitySlider;
    Slider menuGlitchNoiseSlider;
    readonly Image[] languageFrames = new Image[3];
    readonly Image[] languageFaces = new Image[3];
    readonly Text[] languageCodes = new Text[3];
    bool panelOpen;
    MenuPlaySubmenuController playSubmenu;
    MenuVisualEffectsController menuEffects;

    Volume menuVolume;
    ColorAdjustments colorAdjustments;
    Color baseAmbient;
    float baseExposure;
    bool brightnessSourceCached;
    float fpsSampleTime;
    int fpsSampleFrames;
    int displayedFps;

    void Start()
    {
        if (GetComponent<MenuKeyboardNavigation>() == null) gameObject.AddComponent<MenuKeyboardNavigation>();
        LoadSettings();
        var scaler = GetComponent<CanvasScaler>();
        if (scaler != null) scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        SetupMenuMusic();
        CacheBrightnessSource();
        ApplyRuntimeSettings();
        BuildSettingsPanel();
        SetupPlaySubmenu();
        SetupMenuEffects();
        ApplyLanguage();
        if (settingsButton != null) settingsButton.onClick.AddListener(ShowSettings);
        if (exitButtonImage != null && exitButtonImage.TryGetComponent<Button>(out var exit))
            exit.onClick.AddListener(QuitGame);
    }

    void QuitGame()
    {
        SaveSettings();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void Update()
    {
        UpdateFpsCounter();
        if (signalPreview != null) signalPreview.Configure(menuFxQuality, menuFxIntensity, menuGlitchEnabled, menuGlitchNoise);
        if (settingsPanel == null || settingsCanvasGroup == null || settingsCard == null) return;
        if (panelOpen && Input.GetKeyDown(KeyCode.Escape)) HideSettings();

        float blend = 1f - Mathf.Exp(-13f * Time.unscaledDeltaTime);
        settingsCanvasGroup.alpha = Mathf.Lerp(settingsCanvasGroup.alpha, panelOpen ? 1f : 0f, blend);
        settingsCard.localScale = Vector3.Lerp(settingsCard.localScale, panelOpen ? Vector3.one : Vector3.one * 0.96f, blend);
        if (!panelOpen && settingsCanvasGroup.alpha < 0.01f) settingsPanel.SetActive(false);
    }

    void LoadSettings()
    {
        language = (Language)Mathf.Clamp(PlayerPrefs.GetInt(LanguagePrefKey, 0), 0, 2);
        qualityPreset = Mathf.Clamp(PlayerPrefs.GetInt(QualityPrefKey, 1), 0, 2);
        brightness = Mathf.Clamp01(PlayerPrefs.GetFloat(BrightnessPrefKey, 0.5f));
        musicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolumePrefKey, 0.55f));
        fpsOption = Mathf.Clamp(PlayerPrefs.GetInt(FpsPrefKey, 1), 0, FpsOptions.Length - 1);
        vSync = PlayerPrefs.GetInt(VSyncPrefKey, 0) == 1;
        fullscreen = PlayerPrefs.GetInt(FullscreenPrefKey, Screen.fullScreen ? 1 : 0) == 1;
        menuFxQuality = Mathf.Clamp(PlayerPrefs.GetInt(MenuFxQualityPrefKey, 1), 0, 2);
        menuFxIntensity = Mathf.Clamp01(PlayerPrefs.GetFloat(MenuFxIntensityPrefKey, 0.55f));
        menuGlitchEnabled = PlayerPrefs.GetInt(MenuGlitchEnabledPrefKey, 1) == 1;
        menuGlitchNoise = Mathf.Clamp01(PlayerPrefs.GetFloat(MenuGlitchNoisePrefKey, 0.20f));
    }

    void SaveSettings()
    {
        PlayerPrefs.SetInt(LanguagePrefKey, (int)language);
        PlayerPrefs.SetInt(QualityPrefKey, qualityPreset);
        PlayerPrefs.SetFloat(BrightnessPrefKey, brightness);
        PlayerPrefs.SetFloat(MusicVolumePrefKey, musicVolume);
        PlayerPrefs.SetInt(FpsPrefKey, fpsOption);
        PlayerPrefs.SetInt(VSyncPrefKey, vSync ? 1 : 0);
        PlayerPrefs.SetInt(FullscreenPrefKey, fullscreen ? 1 : 0);
        PlayerPrefs.SetInt(MenuFxQualityPrefKey, menuFxQuality);
        PlayerPrefs.SetFloat(MenuFxIntensityPrefKey, menuFxIntensity);
        PlayerPrefs.SetInt(MenuGlitchEnabledPrefKey, menuGlitchEnabled ? 1 : 0);
        PlayerPrefs.SetFloat(MenuGlitchNoisePrefKey, menuGlitchNoise);
        PlayerPrefs.Save();
    }

    void ApplyRuntimeSettings()
    {
        ApplyQuality();
        ApplyBrightness();
        ApplyMusicVolume();
        ApplyFrameSettings();
        ApplyFullscreen();
        ApplyMenuEffects();
    }

    void ApplyQuality()
    {
        MenuRenderQuality.Apply(qualityPreset);
    }

    void CacheBrightnessSource()
    {
        if (brightnessSourceCached) return;
        baseAmbient = RenderSettings.ambientLight;
        menuVolume = FindFirstObjectByType<Volume>();
        if (menuVolume != null && menuVolume.profile != null && menuVolume.profile.TryGet(out ColorAdjustments adjustments))
        {
            colorAdjustments = adjustments;
            baseExposure = colorAdjustments.postExposure.value;
        }
        brightnessSourceCached = true;
    }

    void ApplyBrightness()
    {
        CacheBrightnessSource();
        float offset = (brightness - 0.5f) * 2.2f;
        if (colorAdjustments != null) colorAdjustments.postExposure.Override(baseExposure + offset);
        RenderSettings.ambientLight = baseAmbient * Mathf.Lerp(0.55f, 1.35f, brightness);
    }

    void SetupMenuMusic()
    {
        if (menuMusicSource == null) menuMusicSource = GetComponent<AudioSource>();
        if (menuMusicSource == null) menuMusicSource = gameObject.AddComponent<AudioSource>();
        if (menuMusicClip == null) menuMusicClip = Resources.Load<AudioClip>("Audio/Menu/MenuTheme");

        menuMusicSource.enabled = true;
        menuMusicSource.clip = menuMusicClip;
        menuMusicSource.playOnAwake = false;
        menuMusicSource.loop = true;
        menuMusicSource.spatialBlend = 0f;
        menuMusicSource.dopplerLevel = 0f;
        menuMusicSource.priority = 64;
        menuMusicSource.mute = false;
        menuMusicSource.ignoreListenerPause = true;
        ApplyMusicVolume();

        if (menuMusicClip == null)
        {
            Debug.LogError("Menu music was not found at Resources/Audio/Menu/MenuTheme.", this);
            return;
        }

        StartCoroutine(StartMenuMusicWhenLoaded());
    }

    IEnumerator StartMenuMusicWhenLoaded()
    {
        if (menuMusicClip.loadState == AudioDataLoadState.Unloaded)
            menuMusicClip.LoadAudioData();

        float timeout = 8f;
        while (menuMusicClip.loadState == AudioDataLoadState.Loading && timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (menuMusicClip.loadState == AudioDataLoadState.Failed)
        {
            Debug.LogError("Unity could not decode the menu music audio data.", this);
            yield break;
        }

        if (!menuMusicSource.isPlaying)
            menuMusicSource.Play();

        yield return null;
        if (!menuMusicSource.isPlaying)
            Debug.LogError("Menu music AudioSource did not start playback.", this);
    }

    void ApplyMusicVolume()
    {
        if (menuMusicSource != null) menuMusicSource.volume = musicVolume;
    }

    void ApplyFrameSettings()
    {
        Application.runInBackground = true;
        QualitySettings.vSyncCount = vSync ? 1 : 0;
        Application.targetFrameRate = vSync ? -1 : FpsOptions[fpsOption];
    }

    void ApplyMenuEffects()
    {
        if (menuEffects != null) menuEffects.SetSettings(menuFxQuality, menuFxIntensity, menuGlitchEnabled, menuGlitchNoise);
    }

    void ApplyFullscreen()
    {
#if !UNITY_EDITOR
        Screen.fullScreenMode = fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        Screen.fullScreen = fullscreen;
#endif
    }

    void ApplyLanguage()
    {
        if (language == Language.Ukrainian)
        {
            ApplySprite(menuLogo, ukrainianLogo);
            ApplySprite(playButtonImage, ukrainianPlay);
            ApplySprite(settingsButtonImage, ukrainianSettings);
            ApplySprite(exitButtonImage, ukrainianExit);
        }
        else if (language == Language.English)
        {
            ApplySprite(menuLogo, englishLogo);
            ApplySprite(playButtonImage, englishPlay);
            ApplySprite(settingsButtonImage, englishSettings);
            ApplySprite(exitButtonImage, englishExit);
        }
        else
        {
            ApplySprite(menuLogo, russianLogo);
            ApplySprite(playButtonImage, russianPlay);
            ApplySprite(settingsButtonImage, russianSettings);
            ApplySprite(exitButtonImage, russianExit);
        }
        RefreshSettingsPanel();
        if (playSubmenu != null) playSubmenu.RefreshLanguage();
    }

    public int CurrentLanguageIndex => (int)language;

    void SetupPlaySubmenu()
    {
        if (playButtonImage == null) return;
        var playButton = playButtonImage.GetComponent<Button>();
        if (playButton == null) return;

        playSubmenu = GetComponent<MenuPlaySubmenuController>();
        if (playSubmenu == null) playSubmenu = gameObject.AddComponent<MenuPlaySubmenuController>();
        playSubmenu.Initialize(this, playButton, uiFont);
    }

    void SetupMenuEffects()
    {
        menuEffects = GetComponent<MenuVisualEffectsController>();
        if (menuEffects == null) menuEffects = gameObject.AddComponent<MenuVisualEffectsController>();
        menuEffects.Initialize(transform, settingsPanel != null ? settingsPanel.transform : null, menuLogo);
        ApplyMenuEffects();
    }

    public void RefreshMenuEffects()
    {
        if (menuEffects != null) menuEffects.RefreshButtons();
    }

    static void ApplySprite(Image image, Sprite sprite)
    {
        if (image == null || sprite == null) return;
        image.sprite = sprite;
        image.preserveAspect = true;
        var hover = image.GetComponent<MenuButtonHover>();
        if (hover != null && hover.highlightGraphic is Image highlight)
        {
            highlight.sprite = sprite;
            highlight.preserveAspect = true;
        }
    }

    void SetLanguage(int index)
    {
        language = (Language)Mathf.Clamp(index, 0, 2);
        SaveSettings();
        ApplyLanguage();
    }

    void ChangeQuality(int direction)
    {
        qualityPreset = Wrap(qualityPreset + direction, 3);
        ApplyQuality();
        ApplyFrameSettings();
        SaveSettings();
        RefreshSettingsPanel();
    }

    void ChangeFps(int direction)
    {
        fpsOption = Wrap(fpsOption + direction, FpsOptions.Length);
        vSync = false;
        ApplyFrameSettings();
        SaveSettings();
        RefreshSettingsPanel();
    }

    void ToggleVSync()
    {
        vSync = !vSync;
        ApplyFrameSettings();
        SaveSettings();
        RefreshSettingsPanel();
    }

    void ToggleFullscreen()
    {
        fullscreen = !fullscreen;
        ApplyFullscreen();
        SaveSettings();
        RefreshSettingsPanel();
    }

    void SetBrightness(float value)
    {
        brightness = Mathf.Clamp01(value);
        ApplyBrightness();
        PlayerPrefs.SetFloat(BrightnessPrefKey, brightness);
        PlayerPrefs.Save();
        RefreshSettingsPanel();
    }

    void ChangeBrightness(float amount)
    {
        SetBrightness(Mathf.Round(Mathf.Clamp01(brightness + amount) * 10f) / 10f);
    }

    void SetMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp01(value);
        ApplyMusicVolume();
        PlayerPrefs.SetFloat(MusicVolumePrefKey, musicVolume);
        PlayerPrefs.Save();
        RefreshSettingsPanel();
    }

    void ChangeMusicVolume(float amount)
    {
        SetMusicVolume(Mathf.Round(Mathf.Clamp01(musicVolume + amount) * 10f) / 10f);
    }

    void ChangeMenuFxQuality(int direction)
    {
        menuFxQuality = Wrap(menuFxQuality + direction, 3);
        ApplyMenuEffects();
        SaveSettings();
        RefreshSettingsPanel();
    }

    void SetMenuFxIntensity(float value)
    {
        menuFxIntensity = Mathf.Clamp01(value);
        ApplyMenuEffects();
        PlayerPrefs.SetFloat(MenuFxIntensityPrefKey, menuFxIntensity);
        PlayerPrefs.Save();
        RefreshSettingsPanel();
    }

    void ChangeMenuFxIntensity(float amount)
    {
        SetMenuFxIntensity(Mathf.Round(Mathf.Clamp01(menuFxIntensity + amount) * 10f) / 10f);
    }

    void ToggleMenuGlitch()
    {
        menuGlitchEnabled = !menuGlitchEnabled;
        ApplyMenuEffects();
        SaveSettings();
        RefreshSettingsPanel();
    }

    void SetMenuGlitchNoise(float value)
    {
        menuGlitchNoise = Mathf.Clamp01(value);
        ApplyMenuEffects();
        PlayerPrefs.SetFloat(MenuGlitchNoisePrefKey, menuGlitchNoise);
        PlayerPrefs.Save();
        RefreshSettingsPanel();
    }

    void ChangeMenuGlitchNoise(float amount)
    {
        SetMenuGlitchNoise(Mathf.Round(Mathf.Clamp01(menuGlitchNoise + amount) * 10f) / 10f);
    }

    void ResetDefaults()
    {
        qualityPreset = 1;
        brightness = 0.5f;
        musicVolume = 0.55f;
        fpsOption = 1;
        vSync = false;
        fullscreen = true;
        menuFxQuality = 1;
        menuFxIntensity = 0.55f;
        menuGlitchEnabled = true;
        menuGlitchNoise = 0.20f;
        ApplyRuntimeSettings();
        SaveSettings();
        RefreshSettingsPanel();
    }

    static int Wrap(int value, int count)
    {
        if (value < 0) return count - 1;
        if (value >= count) return 0;
        return value;
    }

    void ShowSettings()
    {
        if (settingsPanel == null) return;
        settingsPanel.SetActive(true);
        settingsPanel.transform.SetAsLastSibling();
        panelOpen = true;
        if (UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        settingsCanvasGroup.alpha = 0f;
        settingsCanvasGroup.blocksRaycasts = true;
        settingsCanvasGroup.interactable = true;
        settingsCard.localScale = Vector3.one * 0.96f;
        RefreshSettingsPanel();
    }

    void HideSettings()
    {
        panelOpen = false;
        if (settingsCanvasGroup == null) return;
        settingsCanvasGroup.blocksRaycasts = false;
        settingsCanvasGroup.interactable = false;
        if (UnityEngine.EventSystems.EventSystem.current != null && settingsButton != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(settingsButton.gameObject);
    }

    void RefreshSettingsPanel()
    {
        if (settingsHeader == null) return;
        int current = (int)language;
        string[] settingsTitles = { "НАСТРОЙКИ", "НАЛАШТУВАННЯ", "SETTINGS" };
        string[] graphicsTitles = { "ГРАФИКА", "ГРАФІКА", "GRAPHICS" };
        string[] interfaceTitles = { "ИНТЕРФЕЙС", "ІНТЕРФЕЙС", "INTERFACE" };
        string[] effectsTitles = { "ЭФФЕКТЫ И ГЛИТЧ", "ЕФЕКТИ ТА ГЛІТЧ", "EFFECTS & GLITCH" };
        string[] qualityWords = { "КАЧЕСТВО", "ЯКІСТЬ", "QUALITY" };
        string[][] qualityValues = { new[] { "БЫСТРО", "БАЛАНС", "ВЫСОКОЕ" }, new[] { "ШВИДКО", "БАЛАНС", "ВИСОКА" }, new[] { "FAST", "BALANCED", "HIGH" } };
        string[] brightnessWords = { "ЯРКОСТЬ", "ЯСКРАВІСТЬ", "BRIGHTNESS" };
        string[] musicWords = { "ГРОМКОСТЬ МУЗЫКИ", "ГУЧНІСТЬ МУЗИКИ", "MUSIC VOLUME" };
        string[] fpsWords = { "ЛИМИТ FPS", "ЛІМІТ FPS", "FPS LIMIT" };
        string[] unlimitedWords = { "БЕЗ ЛИМИТА", "БЕЗ ЛІМІТУ", "UNLIMITED" };
        string[] vSyncWords = { "ВЕРТИКАЛЬНАЯ СИНХРОНИЗАЦИЯ", "ВЕРТИКАЛЬНА СИНХРОНІЗАЦІЯ", "VERTICAL SYNC" };
        string[] languageWords = { "ЯЗЫК ИНТЕРФЕЙСА", "МОВА ІНТЕРФЕЙСУ", "INTERFACE LANGUAGE" };
        string[] fullscreenWords = { "ПОЛНОЭКРАННЫЙ РЕЖИМ", "ПОВНОЕКРАННИЙ РЕЖИМ", "FULLSCREEN MODE" };
        string[] menuFxQualityWords = { "РЕЖИМ ЭФФЕКТОВ", "РЕЖИМ ЕФЕКТІВ", "EFFECT MODE" };
        string[][] menuFxQualityValues = { new[] { "ВЫКЛ", "МЯГКИЕ", "КИНО" }, new[] { "ВИМК", "М'ЯКІ", "КІНО" }, new[] { "OFF", "SUBTLE", "CINEMATIC" } };
        string[] menuFxIntensityWords = { "СИЛА ЭФФЕКТОВ", "СИЛА ЕФЕКТІВ", "EFFECT STRENGTH" };
        string[] menuGlitchEnabledWords = { "ГЛИТЧ ЭКРАНА", "ГЛІТЧ ЕКРАНА", "SCREEN GLITCH" };
        string[] menuGlitchNoiseWords = { "ШУМ ГЛИТЧА", "ШУМ ГЛІТЧА", "GLITCH NOISE" };
        string[] menuFxDescriptions =
        {
            "ГЛИТЧ: 10-15 СЕК // ТОЛЬКО МЕНЮ",
            "ГЛІТЧ: 10-15 СЕК // ЛИШЕ МЕНЮ",
            "GLITCH: 10-15 SEC // MENU ONLY"
        };
        string[] enabledWords = { "ВКЛ", "УВІМК", "ON" };
        string[] disabledWords = { "ВЫКЛ", "ВИМК", "OFF" };
        string[] statsWords = { "ТЕКУЩИЕ ХАРАКТЕРИСТИКИ", "ПОТОЧНІ ХАРАКТЕРИСТИКИ", "CURRENT PERFORMANCE" };
        string[] resolutionWords = { "РАЗРЕШЕНИЕ", "РОЗДІЛЬНІСТЬ", "RESOLUTION" };
        string[] resetWords = { "СБРОСИТЬ", "СКИНУТИ", "RESET" };
        string[] closeWords = { "ГОТОВО", "ГОТОВО", "DONE" };
        string[] saveWords = { "ИЗМЕНЕНИЯ СОХРАНЯЮТСЯ АВТОМАТИЧЕСКИ", "ЗМІНИ ЗБЕРІГАЮТЬСЯ АВТОМАТИЧНО", "CHANGES ARE SAVED AUTOMATICALLY" };

        RefreshDesignLabels();
        settingsHeader.text = settingsTitles[current];
        graphicsHeader.text = graphicsTitles[current];
        interfaceHeader.text = interfaceTitles[current];
        effectsHeader.text = effectsTitles[current];
        qualityLabel.text = qualityWords[current];
        qualityValue.text = qualityValues[current][qualityPreset];
        brightnessLabel.text = brightnessWords[current];
        brightnessValue.text = Mathf.RoundToInt(brightness * 100f) + "%";
        musicLabel.text = musicWords[current];
        musicValue.text = Mathf.RoundToInt(musicVolume * 100f) + "%";
        fpsLabel.text = fpsWords[current];
        fpsValue.text = vSync ? "V-SYNC" : FpsOptions[fpsOption] < 0 ? unlimitedWords[current] : FpsOptions[fpsOption].ToString();
        vSyncLabel.text = vSyncWords[current];
        vSyncValue.text = vSync ? enabledWords[current] : disabledWords[current];
        languageHeader.text = languageWords[current];
        fullscreenLabel.text = fullscreenWords[current];
        fullscreenValue.text = fullscreen ? enabledWords[current] : disabledWords[current];
        menuFxQualityLabel.text = menuFxQualityWords[current];
        menuFxQualityValue.text = menuFxQualityValues[current][menuFxQuality];
        menuFxIntensityLabel.text = menuFxIntensityWords[current];
        menuFxIntensityValue.text = Mathf.RoundToInt(menuFxIntensity * 100f) + "%";
        menuGlitchEnabledLabel.text = menuGlitchEnabledWords[current];
        menuGlitchEnabledValue.text = menuGlitchEnabled ? enabledWords[current] : disabledWords[current];
        menuGlitchNoiseLabel.text = menuGlitchNoiseWords[current];
        menuGlitchNoiseValue.text = Mathf.RoundToInt(menuGlitchNoise * 100f) + "%";
        menuFxDescription.text = menuFxDescriptions[current];
        statsHeader.text = statsWords[current];
        statsValue.text = resolutionWords[current] + ": " + Screen.width + " × " + Screen.height + "    |    FPS: " + displayedFps;
        resetLabel.text = resetWords[current];
        closeLabel.text = closeWords[current];
        saveHint.text = saveWords[current];
        if (brightnessSlider != null) brightnessSlider.SetValueWithoutNotify(brightness);
        if (musicVolumeSlider != null) musicVolumeSlider.SetValueWithoutNotify(musicVolume);
        if (menuFxIntensitySlider != null) menuFxIntensitySlider.SetValueWithoutNotify(menuFxIntensity);
        if (menuGlitchNoiseSlider != null) menuGlitchNoiseSlider.SetValueWithoutNotify(menuGlitchNoise);

        for (int i = 0; i < languageFrames.Length; i++)
        {
            bool selected = i == current;
            if (languageFrames[i] != null) languageFrames[i].color = selected ? DesignAmber : DesignLine;
            if (languageFaces[i] != null) languageFaces[i].color = selected ? new Color(0.21f, 0.16f, 0.08f) : DesignRaised;
            if (languageCodes[i] != null) languageCodes[i].color = selected ? DesignAmber : DesignMuted;
        }
    }

    void UpdateFpsCounter()
    {
        fpsSampleTime += Time.unscaledDeltaTime;
        fpsSampleFrames++;
        if (fpsSampleTime < 0.45f) return;
        displayedFps = Mathf.RoundToInt(fpsSampleFrames / Mathf.Max(0.001f, fpsSampleTime));
        fpsSampleTime = 0f;
        fpsSampleFrames = 0;
        if (panelOpen && statsValue != null) RefreshSettingsPanel();
    }

    Image CreateBlock(string name, Transform parent, Color color, Vector2 anchor, Vector2 size, Vector2 offset)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var image = go.AddComponent<Image>();
        image.color = color;
        SetRect(image.rectTransform, anchor, size, offset);
        return image;
    }

    void AddRail(Transform parent, string name, Vector2 anchor, Vector2 size, Color color)
    {
        var rail = CreateBlock(name, parent, color, anchor, size, Vector2.zero);
        rail.raycastTarget = false;
    }

    void AddBolt(Transform parent, Vector2 anchor)
    {
        var bolt = CreateText("Frame Bolt", parent, "●", 28, new Color(0.62f, 0.34f, 0.12f), anchor, new Vector2(38f, 38f));
        bolt.raycastTarget = false;
    }

    Text CreateText(string name, Transform parent, string value, int size, Color color, Vector2 anchor, Vector2 dimensions)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<Text>();
        text.text = value;
        text.raycastTarget = false;
        text.font = uiFont != null ? uiFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        SetRect(text.rectTransform, anchor, dimensions, Vector2.zero);
        return text;
    }

    static ColorBlock ButtonColors()
    {
        var colors = ColorBlock.defaultColorBlock;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.32f, 1.08f, 0.82f, 1f);
        colors.pressedColor = new Color(1.48f, 0.91f, 0.52f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.42f, 0.42f, 0.42f, 0.55f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        return colors;
    }

    static void SetRect(RectTransform rect, Vector2 anchor, Vector2 size, Vector2 offset)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = offset;
        rect.localScale = Vector3.one;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }
}
