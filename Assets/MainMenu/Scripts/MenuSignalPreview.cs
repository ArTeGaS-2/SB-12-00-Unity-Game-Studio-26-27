using UnityEngine;
using UnityEngine.UI;

/// <summary>Isolated signal test; opening settings never glitches the controls themselves.</summary>
public sealed class MenuSignalPreview : MonoBehaviour
{
    Material overlayMaterial;
    Text stateLabel;
    Image overlay;
    int quality;
    int language;
    float intensity;
    bool glitches;
    float testUntil;
    float nextDemo;
    float seed;
    float seedAt;

    public void Build(Font font)
    {
        var viewport=new GameObject("Signal Viewport",typeof(RectTransform),typeof(Image),typeof(RectMask2D));
        viewport.transform.SetParent(transform,false);
        Place(viewport.GetComponent<RectTransform>(),24,64,560,280);
        viewport.GetComponent<Image>().color=new Color(0.032f,0.021f,0.009f);
        var grid=new GameObject("Shelter Feed",typeof(RectTransform),typeof(CanvasRenderer)).AddComponent<MenuConsoleGraphic>();
        grid.transform.SetParent(viewport.transform,false);Stretch(grid.rectTransform);grid.color=new Color(0.95f,0.65f,0.22f);grid.raycastTarget=false;
        var label=new GameObject("Feed Serial",typeof(RectTransform),typeof(CanvasRenderer)).AddComponent<Text>();
        label.transform.SetParent(viewport.transform,false);Place(label.rectTransform,16,10,520,24);label.font=font;label.fontSize=13;label.color=new Color(0.85f,0.72f,0.48f);label.text="CAM 067   /   SHELTER ACCESS   /   TX-4";label.raycastTarget=false;

        var shader=Resources.Load<Shader>("Shaders/IndustrialMenuOverlay");
        overlay=new GameObject("Preview Interference",typeof(RectTransform),typeof(CanvasRenderer)).AddComponent<Image>();
        overlay.transform.SetParent(viewport.transform,false);Stretch(overlay.rectTransform);overlay.raycastTarget=false;
        if(shader!=null)
        {
            overlayMaterial=new Material(shader);
            overlayMaterial.SetColor("_ChromaTint",new Color(1f,0.80f,0.30f));
            overlayMaterial.SetColor("_NoiseTint",new Color(0.028f,0.018f,0.008f));
            overlay.material=overlayMaterial;
        }
        stateLabel=new GameObject("Signal State",typeof(RectTransform),typeof(CanvasRenderer)).AddComponent<Text>();
        stateLabel.transform.SetParent(transform,false);Place(stateLabel.rectTransform,24,361,560,28);stateLabel.font=font;stateLabel.fontSize=17;stateLabel.color=new Color(1f,0.80f,0.30f);stateLabel.raycastTarget=false;
        nextDemo=Time.unscaledTime+3.5f;
    }

    public void Configure(int mode,float strength,bool enabled,float noise)
    {
        quality=mode;intensity=strength;glitches=enabled;
        if(quality==0 || intensity<=0.001f || !glitches)testUntil=0;
    }
    public void SetLanguage(int index){language=index;}
    public void Trigger()
    {
        if(quality==0 || intensity<=0.001f || !glitches)return;
        testUntil=Time.unscaledTime+0.85f;seed=Random.Range(1f,1000f);nextDemo=Time.unscaledTime+7f;
    }
    void Update()
    {
        if(overlayMaterial==null)return;
        if(Time.unscaledTime>=nextDemo){Trigger();nextDemo=Time.unscaledTime+7f;}
        bool active=Time.unscaledTime<testUntil;
        if(Time.unscaledTime>=seedAt){seedAt=Time.unscaledTime+0.07f;seed+=1;}
        overlayMaterial.SetFloat("_FxIntensity",intensity);
        overlayMaterial.SetFloat("_FxQuality",quality==0?0:quality==1?0.62f:1);
        overlayMaterial.SetFloat("_GlitchAmount",active?Mathf.Clamp01((testUntil-Time.unscaledTime)*4)*Mathf.Lerp(0.35f,1,intensity):0);
        overlayMaterial.SetFloat("_GlitchSeed",seed);
        overlayMaterial.SetFloat("_EffectTime",Time.unscaledTime);
        if(stateLabel!=null)
        {
            string[] stable={"●  СИГНАЛ СТАБИЛЕН","●  СИГНАЛ СТАБІЛЬНИЙ","●  SIGNAL STABLE"};
            string[] disturbed={"!  ПОТЕРЯ СИНХРОНИЗАЦИИ","!  ВТРАТА СИНХРОНІЗАЦІЇ","!  SYNC LOST"};
            stateLabel.text=(active?disturbed:stable)[Mathf.Clamp(language,0,2)];
            stateLabel.color=active?new Color(1,0.50f,0.16f):new Color(1f,0.80f,0.30f);
        }
    }
    void OnDestroy(){if(overlayMaterial!=null)Destroy(overlayMaterial);}
    static void Place(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
    static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
}
