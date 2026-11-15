using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-32000)]
public sealed class GlobalUIButtonClickSound : MonoBehaviour
{
    const int SampleRate = 44100;
    const float ScanInterval = 0.25f;
    const float ClickVolume = 0.28f;
    const float ClickCooldown = 0.045f;

    static GlobalUIButtonClickSound instance;

    AudioSource audioSource;
    AudioClip clickClip;
    float nextScanTime;
    float nextClickTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (instance != null) return;

        var host = new GameObject("Global UI Click Audio");
        DontDestroyOnLoad(host);
        instance = host.AddComponent<GlobalUIButtonClickSound>();
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        Application.runInBackground = true;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = ClickVolume;
        audioSource.ignoreListenerPause = true;
        audioSource.ignoreListenerVolume = true;

        clickClip = CreateMechanicalClick();
        ScanButtons();
    }

    void Update()
    {
        if (Time.unscaledTime < nextScanTime) return;

        nextScanTime = Time.unscaledTime + ScanInterval;
        ScanButtons();
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
        if (clickClip != null) Destroy(clickClip);
    }

    public static void Play()
    {
        if (instance == null || instance.audioSource == null || instance.clickClip == null) return;
        if (Time.unscaledTime < instance.nextClickTime) return;

        instance.nextClickTime = Time.unscaledTime + ClickCooldown;
        instance.audioSource.volume = ClickVolume;
        instance.audioSource.pitch = Random.Range(0.995f, 1.005f);
        instance.audioSource.PlayOneShot(instance.clickClip);
    }

    void ScanButtons()
    {
        var buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var button in buttons)
        {
            if (button == null || button.GetComponent<UIButtonClickSoundBinding>() != null) continue;
            button.gameObject.AddComponent<UIButtonClickSoundBinding>();
        }
    }

    static AudioClip CreateMechanicalClick()
    {
        const float duration = 0.052f;
        int sampleCount = Mathf.CeilToInt(SampleRate * duration);
        var samples = new float[sampleCount];
        uint noiseState = 0x67A11CEu;

        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)SampleRate;
            float softTransient = Mathf.Sin(2f * Mathf.PI * 1080f * time) * Mathf.Exp(-92f * time) * 0.52f;
            float softBody = Mathf.Sin(2f * Mathf.PI * 430f * time) * Mathf.Exp(-58f * time) * 0.34f;

            noiseState = noiseState * 1664525u + 1013904223u;
            float noise = (((noiseState >> 8) & 0xFFFFu) / 32767.5f - 1f) * Mathf.Exp(-105f * time) * 0.065f;

            float impact = time < 0.004f ? (1f - time / 0.004f) * 0.10f : 0f;
            samples[i] = Mathf.Clamp((softTransient + softBody + noise + impact) * 0.46f, -1f, 1f);
        }

        var clip = AudioClip.Create("Soft Industrial UI Click", sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
