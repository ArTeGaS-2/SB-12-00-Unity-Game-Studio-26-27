using UnityEngine;

/// <summary>Keeps a designed panel inside its parent at any window aspect ratio.</summary>
[DisallowMultipleComponent]
public sealed class MenuReferenceLayout : MonoBehaviour
{
    public static RectTransform Create(Transform parent, string name)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
        rect.sizeDelta = new Vector2(1920f, 1080f);
        rect.gameObject.AddComponent<MenuReferenceLayout>();
        return rect;
    }

    void OnEnable() { Fit(); }
    void LateUpdate() { Fit(); }

    void Fit()
    {
        var rect = transform as RectTransform;
        var parent = transform.parent as RectTransform;
        if (rect == null || parent == null || parent.rect.width <= 0f || parent.rect.height <= 0f) return;
        float scale = Mathf.Min(parent.rect.width / 1920f, parent.rect.height / 1080f);
        rect.localScale = Vector3.one * scale;
    }
}
