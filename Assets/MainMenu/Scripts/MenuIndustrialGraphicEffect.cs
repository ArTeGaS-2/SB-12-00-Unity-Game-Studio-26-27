using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MenuIndustrialGraphicEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
{
    static readonly int FxIntensityId = Shader.PropertyToID("_FxIntensity");
    static readonly int FxQualityId = Shader.PropertyToID("_FxQuality");
    static readonly int HoverId = Shader.PropertyToID("_Hover");
    static readonly int PressedId = Shader.PropertyToID("_Pressed");

    Image targetImage;
    Material originalMaterial;
    Material runtimeMaterial;
    bool logoMode;
    bool hovered;
    bool pressed;
    bool selected;
    Selectable selectable;
    float hoverBlend;
    float pressBlend;
    float intensity;
    float qualityStrength;

    public void Initialize(Image image, bool useLogoPulse)
    {
        if (image == null) return;
        if (targetImage == image && runtimeMaterial != null)
        {
            logoMode |= useLogoPulse;
            return;
        }

        RestoreMaterial();
        targetImage = image;
        selectable = GetComponent<Selectable>();
        logoMode = useLogoPulse;
        originalMaterial = targetImage.material;

        var shader = Resources.Load<Shader>("Shaders/IndustrialMenuSurface");
        if (shader == null)
        {
            Debug.LogWarning("Industrial menu surface shader was not found.", this);
            return;
        }

        runtimeMaterial = new Material(shader) { name = targetImage.name + " Industrial FX (Runtime)" };
        targetImage.material = runtimeMaterial;
        ApplyMaterialValues();
    }

    public void SetEffect(float newIntensity, float newQualityStrength)
    {
        intensity = Mathf.Clamp01(newIntensity);
        qualityStrength = Mathf.Clamp01(newQualityStrength);
        ApplyMaterialValues();
    }

    void Update()
    {
        if (runtimeMaterial == null) return;

        bool interactive = selectable == null || selectable.IsInteractable();
        float desiredHover = logoMode
            ? 0.30f + Mathf.Sin(Time.unscaledTime * 1.15f) * 0.08f
            : interactive && (hovered || selected) ? 1f : 0f;
        float desiredPress = interactive && pressed ? 1f : 0f;
        float blend = 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime);
        hoverBlend = Mathf.Lerp(hoverBlend, desiredHover, blend);
        pressBlend = Mathf.Lerp(pressBlend, desiredPress, blend);
        runtimeMaterial.SetFloat(HoverId, hoverBlend);
        runtimeMaterial.SetFloat(PressedId, pressBlend);
    }

    public void OnPointerEnter(PointerEventData eventData) { hovered = true; }
    public void OnPointerExit(PointerEventData eventData) { hovered = false; pressed = false; }
    public void OnPointerDown(PointerEventData eventData) { if (eventData.button == PointerEventData.InputButton.Left) pressed = true; }
    public void OnPointerUp(PointerEventData eventData) { pressed = false; }
    public void OnSelect(BaseEventData eventData) { selected = true; }
    public void OnDeselect(BaseEventData eventData) { selected = false; pressed = false; }

    void OnDisable()
    {
        hovered = false;
        selected = false;
        pressed = false;
        hoverBlend = logoMode ? 0.3f : 0f;
        pressBlend = 0f;
        ApplyMaterialValues();
    }

    void OnDestroy()
    {
        RestoreMaterial();
    }

    void ApplyMaterialValues()
    {
        if (runtimeMaterial == null) return;
        runtimeMaterial.SetFloat(FxIntensityId, intensity);
        runtimeMaterial.SetFloat(FxQualityId, qualityStrength);
        runtimeMaterial.SetFloat(HoverId, hoverBlend);
        runtimeMaterial.SetFloat(PressedId, pressBlend);
    }

    void RestoreMaterial()
    {
        if (targetImage != null && targetImage.material == runtimeMaterial)
            targetImage.material = originalMaterial;
        if (runtimeMaterial != null) Destroy(runtimeMaterial);
        runtimeMaterial = null;
    }
}
