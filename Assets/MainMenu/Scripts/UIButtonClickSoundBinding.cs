using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class UIButtonClickSoundBinding : MonoBehaviour, IPointerDownHandler, ISubmitHandler
{
    Button button;

    void Awake()
    {
        button = GetComponent<Button>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || !CanPlay()) return;
        GlobalUIButtonClickSound.Play();
    }

    public void OnSubmit(BaseEventData eventData)
    {
        if (CanPlay()) GlobalUIButtonClickSound.Play();
    }

    bool CanPlay()
    {
        if (button == null) button = GetComponent<Button>();
        return button != null && button.IsActive() && button.interactable;
    }
}
