using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public sealed partial class GameplayPauseMenuController
{
    CanvasGroup pauseGroup;
    RectTransform pauseShell;
    Text pauseHeading, pauseSubtitle;
    Button pauseHudButton;
    float resumeTimeScale = 1f;
    bool resumeAudioPause;
    CursorLockMode resumeCursorLock;
    bool resumeCursorVisible;
    bool vSync;
    Color gameplayAmbient;
    public bool IsPaused => paused;

    void RefreshPauseHeader(bool settings)
    {
        pauseHeading.text = settings ? L("НАСТРОЙКИ", "НАЛАШТУВАННЯ", "SETTINGS") : L("ПАУЗА", "ПАУЗА", "PAUSED");
        pauseHeading.fontSize = settings ? 50 : 64;
        pauseSubtitle.text = settings
            ? L("ИЗОБРАЖЕНИЕ И ЗВУК // ИГРА НА ПАУЗЕ", "ЗОБРАЖЕННЯ ТА ЗВУК // ГРА НА ПАУЗІ", "IMAGE & AUDIO // GAME PAUSED")
            : L("УЗЕЛ 67 // УПРАВЛЕНИЕ ОСТАНОВЛЕНО", "ВУЗОЛ 67 // КЕРУВАННЯ ЗУПИНЕНО", "NODE 67 // CONTROL SUSPENDED");
        if(EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    void AnimatePause()
    {
        if(!paused || pauseGroup == null) return;
        float blend = 1f - Mathf.Exp(-16f * Time.unscaledDeltaTime);
        pauseGroup.alpha = Mathf.Lerp(pauseGroup.alpha, 1f, blend);
        pauseShell.localScale = Vector3.Lerp(pauseShell.localScale, Vector3.one, blend);
    }

    void RestorePausedState()
    {
        Time.timeScale = resumeTimeScale;
        AudioListener.pause = resumeAudioPause;
        Cursor.lockState = resumeCursorLock;
        Cursor.visible = resumeCursorVisible;
    }
}
