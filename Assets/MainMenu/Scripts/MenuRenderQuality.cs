using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>Applies the same presets in both menus without modifying project assets.</summary>
public sealed class MenuRenderQuality : MonoBehaviour
{
    static MenuRenderQuality instance;
    UniversalRenderPipelineAsset runtimeAsset;
    RenderPipelineAsset previousOverride;

    public static void Apply(int preset)
    {
        if (instance == null)
        {
            var host = new GameObject("Runtime Render Quality");
            DontDestroyOnLoad(host);
            instance = host.AddComponent<MenuRenderQuality>();
        }
        instance.ApplyPreset(Mathf.Clamp(preset, 0, 2));
    }

    void ApplyPreset(int preset)
    {
        ReleaseAsset();
        int level = Mathf.Min(preset == 0 ? 0 : 1, QualitySettings.names.Length - 1);
        if (level >= 0) QualitySettings.SetQualityLevel(level, true);
        QualitySettings.lodBias = preset == 0 ? 0.8f : preset == 1 ? 1.35f : 2f;
        QualitySettings.globalTextureMipmapLimit = preset == 0 ? 1 : 0;
        QualitySettings.anisotropicFiltering = preset == 0 ? AnisotropicFiltering.Disable :
            preset == 1 ? AnisotropicFiltering.Enable : AnisotropicFiltering.ForceEnable;

        previousOverride = QualitySettings.renderPipeline;
        var source = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (source != null)
        {
            runtimeAsset = Instantiate(source);
            runtimeAsset.name = source.name + " (Runtime Quality)";
            runtimeAsset.hideFlags = HideFlags.DontSave;
            runtimeAsset.msaaSampleCount = preset == 0 ? 1 : preset == 1 ? 2 : 4;
            runtimeAsset.renderScale = 1f;
            runtimeAsset.shadowDistance = preset == 0 ? 18f : preset == 1 ? 40f : 70f;
            QualitySettings.renderPipeline = runtimeAsset;
        }

        foreach (var camera in FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            camera.allowMSAA = preset > 0;
            if (!camera.TryGetComponent<UniversalAdditionalCameraData>(out var data)) continue;
            data.antialiasing = preset == 0 ? AntialiasingMode.None :
                preset == 1 ? AntialiasingMode.FastApproximateAntialiasing : AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality = AntialiasingQuality.High;
        }
    }

    void ReleaseAsset()
    {
        if (runtimeAsset == null) return;
        if (QualitySettings.renderPipeline == runtimeAsset) QualitySettings.renderPipeline = previousOverride;
        Destroy(runtimeAsset);
        runtimeAsset = null;
    }

    void OnDestroy()
    {
        ReleaseAsset();
        if (instance == this) instance = null;
    }
}
