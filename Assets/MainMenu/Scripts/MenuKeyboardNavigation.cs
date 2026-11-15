using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Tab navigation stays inside the visible modal, including rebuilt terminal pages.</summary>
public sealed class MenuKeyboardNavigation : MonoBehaviour
{
    readonly List<Selectable> controls = new List<Selectable>();

    void Update()
    {
        if (EventSystem.current == null || !Input.GetKeyDown(KeyCode.Tab)) return;
        Transform scope = transform;
        var fade = transform.Find("Menu Fade");
        if (fade != null && fade.gameObject.activeInHierarchy) return;
        var menu = GetComponent<MenuSettingsController>();
        var settings = menu != null ? menu.SettingsRoot : transform.Find("Settings Overlay");
        var terminalOwner=GetComponent<MenuPlaySubmenuController>();
        if(terminalOwner!=null && terminalOwner.IsTransitioning)return;
        var terminal = terminalOwner!=null?terminalOwner.SubmenuRoot:transform.Find("Play Submenu");
        var pause = transform.Find("Pause Overlay");
        if (settings != null && settings.gameObject.activeInHierarchy) scope = settings;
        else if (terminal != null && terminal.gameObject.activeInHierarchy) scope = terminal;
        else if (pause != null && pause.gameObject.activeInHierarchy) scope = pause;

        scope.GetComponentsInChildren(false, controls);
        controls.RemoveAll(c => !c.isActiveAndEnabled || !c.IsInteractable());
        if (controls.Count == 0) return;
        int current = controls.FindIndex(c => c.gameObject == EventSystem.current.currentSelectedGameObject);
        bool reverse = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        int next = current < 0 ? (reverse ? controls.Count - 1 : 0) :
            (current + (reverse ? controls.Count - 1 : 1)) % controls.Count;
        EventSystem.current.SetSelectedGameObject(controls[next].gameObject);
    }
}
