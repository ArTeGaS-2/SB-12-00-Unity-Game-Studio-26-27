using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
{
    public Graphic targetGraphic;
    public Graphic highlightGraphic;
    public float hoverScale = 1.055f;
    public float pressedScale = 1.025f;
    public float animationSpeed = 13f;
    public float highlightAlpha = 0.20f;

    RectTransform rectTransform;
    Vector3 baseScale;
    Color baseColor;
    bool hovered;
    bool pressed;
    bool selected;
    Selectable selectable;

    void Awake()
    {
        rectTransform = transform as RectTransform;
        selectable = GetComponent<Selectable>();
        baseScale = rectTransform != null ? rectTransform.localScale : transform.localScale;
        if (targetGraphic == null) targetGraphic = GetComponent<Graphic>();
        baseColor = targetGraphic != null ? targetGraphic.color : Color.white;
        SetHighlightAlpha(0f);
    }

    void Update()
    {
        bool interactive = selectable == null || selectable.IsInteractable();
        bool active = interactive && (hovered || selected);
        float scaleMultiplier = interactive && pressed ? pressedScale : active ? hoverScale : 1f;
        float blend = 1f - Mathf.Exp(-animationSpeed * Time.unscaledDeltaTime);
        transform.localScale = Vector3.Lerp(transform.localScale, baseScale * scaleMultiplier, blend);

        if (targetGraphic != null)
        {
            Color hoverColor = new Color(1.13f, 1.08f, 0.93f, baseColor.a);
            targetGraphic.color = Color.Lerp(targetGraphic.color, active ? hoverColor : baseColor, blend);
        }

        if (highlightGraphic != null)
        {
            Color color = highlightGraphic.color;
            color.a = Mathf.Lerp(color.a, active ? highlightAlpha : 0f, blend);
            highlightGraphic.color = color;
        }
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
        transform.localScale = baseScale == Vector3.zero ? Vector3.one : baseScale;
        if (targetGraphic != null) targetGraphic.color = baseColor;
        SetHighlightAlpha(0f);
    }

    void SetHighlightAlpha(float alpha)
    {
        if (highlightGraphic == null) return;
        Color color = highlightGraphic.color;
        color.a = alpha;
        highlightGraphic.color = color;
    }
}
