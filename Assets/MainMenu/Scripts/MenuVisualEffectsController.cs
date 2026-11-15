using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(200)]
public sealed class MenuVisualEffectsController : MonoBehaviour
{
    static readonly int FxIntensityId = Shader.PropertyToID("_FxIntensity");
    static readonly int FxQualityId = Shader.PropertyToID("_FxQuality");
    static readonly int GlitchAmountId = Shader.PropertyToID("_GlitchAmount");
    static readonly int GlitchSeedId = Shader.PropertyToID("_GlitchSeed");

    const float MinGlitchDelay = 10f;
    const float MaxGlitchDelay = 15f;
    const int NoiseSampleRate = 22050;

    Transform menuRoot;
    Transform excludedSettingsRoot;
    Image menuLogo;
    Image overlayImage;
    Material overlayMaterial;
    AudioSource glitchAudioSource;
    AudioClip glitchNoiseClip;
    float nextGlitchAt;
    float glitchEndAt;
    float glitchDuration;
    float glitchSeed;
    int quality;
    float intensity;
    bool glitchesEnabled = true;
    float glitchNoiseVolume = 0.20f;
    bool glitchPlaying;
    Text glitchStatus;

    public void Initialize(Transform root, Transform settingsRoot, Image logo)
    {
        menuRoot = root;
        excludedSettingsRoot = settingsRoot;
        menuLogo = logo;
        BuildOverlay();
        AttachLogoEffect();
        ScanMenuButtons();
        ApplySettings();
        ScheduleNextGlitch();
    }

    public void SetSettings(int newQuality, float newIntensity)
    {
        SetSettings(newQuality, newIntensity, glitchesEnabled, glitchNoiseVolume);
    }

    public void SetSettings(int newQuality, float newIntensity, bool enableGlitches, float newGlitchNoiseVolume)
    {
        quality = Mathf.Clamp(newQuality, 0, 2);
        intensity = Mathf.Clamp01(newIntensity);
        glitchesEnabled = enableGlitches;
        glitchNoiseVolume = Mathf.Clamp01(newGlitchNoiseVolume);
        ApplySettings();

        if (!CanGlitch()) FinishGlitch();
        if (nextGlitchAt <= Time.unscaledTime) ScheduleNextGlitch();
    }

    void Update()
    {
        if (menuRoot == null) return;

        KeepOverlayAboveMenu();
        if (overlayMaterial != null) overlayMaterial.SetFloat("_EffectTime", Time.unscaledTime);
        UpdateGlitch();
    }

    public void RefreshButtons()
    {
        ScanMenuButtons();
    }

    void OnDisable()
    {
        FinishGlitch();
    }

    void OnDestroy()
    {
        if (overlayMaterial != null) Destroy(overlayMaterial);
        if (glitchNoiseClip != null) Destroy(glitchNoiseClip);
    }

    void BuildOverlay()
    {
        if (overlayImage != null || menuRoot == null) return;

        var shader = Resources.Load<Shader>("Shaders/IndustrialMenuOverlay");
        if (shader == null)
        {
            Debug.LogWarning("Industrial menu overlay shader was not found.", this);
            return;
        }

        var overlayObject = new GameObject("Industrial Menu Shader Overlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        overlayObject.transform.SetParent(menuRoot, false);
        overlayImage = overlayObject.GetComponent<Image>();
        overlayImage.color = Color.white;
        overlayImage.raycastTarget = false;
        Stretch(overlayImage.rectTransform);

        overlayMaterial = new Material(shader) { name = "Industrial Menu Overlay (Runtime)" };
        overlayImage.material = overlayMaterial;
        overlayObject.transform.SetAsLastSibling();

        glitchAudioSource = overlayObject.AddComponent<AudioSource>();
        glitchAudioSource.playOnAwake = false;
        glitchAudioSource.loop = false;
        glitchAudioSource.spatialBlend = 0f;
        glitchAudioSource.dopplerLevel = 0f;
        glitchAudioSource.priority = 72;
        glitchAudioSource.ignoreListenerPause = true;
        glitchNoiseClip = CreateWhiteNoiseClip();
        var statusObject = new GameObject("Signal Loss Readout", typeof(RectTransform), typeof(CanvasRenderer));
        statusObject.transform.SetParent(overlayObject.transform, false);
        glitchStatus = statusObject.AddComponent<Text>();
        var owner = GetComponent<MenuSettingsController>();
        glitchStatus.font = owner != null && owner.uiFont != null ? owner.uiFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        glitchStatus.fontSize = 18;
        glitchStatus.color = new Color(0.42f, 0.92f, 0.78f);
        glitchStatus.raycastTarget = false;
        glitchStatus.text = "!  SYNC LOST    /    CHANNEL 067    /    RECONNECTING";
        var statusRect = glitchStatus.rectTransform;
        statusRect.anchorMin = statusRect.anchorMax = new Vector2(0.035f, 0.94f);
        statusRect.pivot = new Vector2(0, 1);
        statusRect.sizeDelta = new Vector2(900, 40);
        statusRect.anchoredPosition = Vector2.zero;
        glitchStatus.gameObject.SetActive(false);
    }

    void AttachLogoEffect()
    {
        if (menuLogo == null) return;
        var effect = menuLogo.GetComponent<MenuIndustrialGraphicEffect>();
        if (effect == null) effect = menuLogo.gameObject.AddComponent<MenuIndustrialGraphicEffect>();
        effect.Initialize(menuLogo, true);
    }

    void ScanMenuButtons()
    {
        if (menuRoot == null) return;

        var buttons = menuRoot.GetComponentsInChildren<Button>(true);
        foreach (var button in buttons)
        {
            if (button == null || IsSettingsControl(button.transform)) continue;
            var image = button.targetGraphic as Image;
            if (image == null) image = button.GetComponent<Image>();
            if (image == null) continue;

            var effect = button.GetComponent<MenuIndustrialGraphicEffect>();
            if (effect == null) effect = button.gameObject.AddComponent<MenuIndustrialGraphicEffect>();
            effect.Initialize(image, false);
            effect.SetEffect(intensity, QualityStrength());
        }
    }

    void ApplySettings()
    {
        float qualityStrength = QualityStrength();
        if (overlayImage != null) overlayImage.gameObject.SetActive(quality > 0 && intensity > 0.001f);
        if (overlayMaterial != null)
        {
            overlayMaterial.SetFloat(FxIntensityId, intensity);
            overlayMaterial.SetFloat(FxQualityId, qualityStrength);
            if (!CanGlitch()) overlayMaterial.SetFloat(GlitchAmountId, 0f);
        }

        if (menuRoot == null) return;
        var effects = menuRoot.GetComponentsInChildren<MenuIndustrialGraphicEffect>(true);
        foreach (var effect in effects)
            effect.SetEffect(intensity, qualityStrength);
    }

    bool IsSettingsControl(Transform candidate)
    {
        return excludedSettingsRoot != null && candidate.IsChildOf(excludedSettingsRoot);
    }

    float QualityStrength()
    {
        if (quality <= 0) return 0f;
        return quality == 1 ? 0.62f : 1f;
    }

    void UpdateGlitch()
    {
        if (!CanGlitch() || SettingsAreVisible())
        {
            if (glitchPlaying) FinishGlitch();
            if (nextGlitchAt <= Time.unscaledTime) ScheduleNextGlitch();
            return;
        }

        if (!glitchPlaying)
        {
            if (Time.unscaledTime >= nextGlitchAt) StartGlitch();
            return;
        }

        if (Time.unscaledTime >= glitchEndAt)
        {
            FinishGlitch();
            ScheduleNextGlitch();
            return;
        }

        float progress = 1f - Mathf.Clamp01((glitchEndAt - Time.unscaledTime) / Mathf.Max(0.01f, glitchDuration));
        float attack = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress * 7f));
        float release = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((1f - progress) * 5f));
        float stutter = Mathf.Lerp(0.28f, 1f, Mathf.PerlinNoise(glitchSeed, Time.unscaledTime * 38f));
        float amount = attack * release * stutter * Mathf.Lerp(0.35f, 1f, intensity);

        if (overlayMaterial != null)
        {
            overlayMaterial.SetFloat(GlitchAmountId, amount);
            overlayMaterial.SetFloat(GlitchSeedId, glitchSeed + Mathf.Floor(Time.unscaledTime * 24f));
        }
    }

    void StartGlitch()
    {
        glitchPlaying = true;
        glitchDuration = Random.Range(0.42f, 0.68f);
        glitchEndAt = Time.unscaledTime + glitchDuration;
        glitchSeed = Random.Range(1f, 1000f);
        if (glitchStatus != null) glitchStatus.gameObject.SetActive(true);

        if (glitchAudioSource != null && glitchNoiseClip != null && glitchNoiseVolume > 0.001f)
        {
            glitchAudioSource.pitch = Random.Range(0.92f, 1.08f);
            glitchAudioSource.PlayOneShot(glitchNoiseClip, Mathf.Lerp(0.025f, 0.22f, glitchNoiseVolume));
        }
    }

    void FinishGlitch()
    {
        glitchPlaying = false;
        if (glitchStatus != null) glitchStatus.gameObject.SetActive(false);
        if (overlayMaterial != null) overlayMaterial.SetFloat(GlitchAmountId, 0f);
        if (glitchAudioSource != null && glitchAudioSource.isPlaying) glitchAudioSource.Stop();
    }

    void ScheduleNextGlitch()
    {
        nextGlitchAt = Time.unscaledTime + Random.Range(MinGlitchDelay, MaxGlitchDelay);
    }

    bool CanGlitch()
    {
        return glitchesEnabled && quality > 0 && intensity > 0.01f && overlayImage != null && overlayImage.gameObject.activeSelf;
    }

    bool SettingsAreVisible()
    {
        if(excludedSettingsRoot != null && excludedSettingsRoot.gameObject.activeInHierarchy)return true;
        var terminal=GetComponent<MenuPlaySubmenuController>();
        return terminal!=null && terminal.SubmenuRoot!=null && terminal.SubmenuRoot.gameObject.activeInHierarchy;
    }

    void KeepOverlayAboveMenu()
    {
        if (overlayImage == null) return;
        // Transitions and dialogs must remain above the decorative shader.
        int target = menuRoot.childCount - 1;
        for (int i = 0; i < menuRoot.childCount; i++)
        {
            var child = menuRoot.GetChild(i);
            if (!child.gameObject.activeSelf || (child != excludedSettingsRoot && child.name != "Menu Fade")) continue;
            int before = i - (overlayImage.transform.GetSiblingIndex() < i ? 1 : 0);
            target = Mathf.Min(target, before);
        }
        if (overlayImage.transform.GetSiblingIndex() != target) overlayImage.transform.SetSiblingIndex(target);
    }

    AudioClip CreateWhiteNoiseClip()
    {
        const float duration = 0.56f;
        int sampleCount = Mathf.CeilToInt(NoiseSampleRate * duration);
        var samples = new float[sampleCount];
        uint state = 0x67C0FFEEu;
        float filtered = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)NoiseSampleRate;
            float normalized = time / duration;
            float envelope = Mathf.Sin(Mathf.PI * Mathf.Clamp01(normalized));
            envelope *= Mathf.Lerp(1f, 0.35f, normalized);

            state = state * 1664525u + 1013904223u;
            float raw = (((state >> 8) & 0xFFFFu) / 32767.5f) - 1f;
            filtered = Mathf.Lerp(filtered, raw, 0.42f);
            float digitalGate = ((i / 118) & 1) == 0 ? 1f : 0.42f;
            samples[i] = Mathf.Clamp(filtered * envelope * digitalGate * 0.72f, -1f, 1f);
        }

        var clip = AudioClip.Create("Menu Glitch White Noise", sampleCount, 1, NoiseSampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    public void TriggerGlitchNow()
    {
        if (CanGlitch() && !SettingsAreVisible()) StartGlitch();
    }

    public void PlayPreviewNoise()
    {
        if (!glitchesEnabled || quality == 0 || intensity <= 0.001f || glitchNoiseVolume <= 0.001f) return;
        if (glitchAudioSource != null && glitchNoiseClip != null)
            glitchAudioSource.PlayOneShot(glitchNoiseClip, Mathf.Lerp(0f, 0.15f, glitchNoiseVolume));
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
