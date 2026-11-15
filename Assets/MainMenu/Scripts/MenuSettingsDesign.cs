using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public partial class MenuSettingsController
{
    static readonly Color DesignAmber = new Color(1f, 0.66f, 0.24f);
    static readonly Color DesignText = new Color(0.91f, 0.91f, 0.84f);
    static readonly Color DesignMuted = new Color(0.64f, 0.58f, 0.48f);
    static readonly Color DesignRaised = new Color(0.084f, 0.066f, 0.043f);
    static readonly Color DesignLine = new Color(0.26f, 0.19f, 0.10f);
    static readonly Color DesignGold = new Color(1f, 0.80f, 0.30f);
    readonly GameObject[] designPages = new GameObject[3];
    readonly Image[] tabFaces = new Image[3];
    readonly Image[] tabMarkers = new Image[3];
    readonly Text[] tabTitles = new Text[3];
    readonly Text[] tabDescriptions = new Text[3];
    Text pageTitle, pageSubtitle, previewTitle, previewHint, previewButtonLabel, statusBadge;
    Text displayHint, audioHint;
    int settingsTab;
    MenuSignalPreview signalPreview;
    Button previewButton;

    public Transform SettingsRoot => settingsPanel != null ? settingsPanel.transform : null;

    void BuildSettingsPanel()
    {
        settingsPanel = CreateBlock("Settings Overlay", null, new Color(0.008f, 0.005f, 0.003f, 0.91f), Vector2.one * 0.5f, Vector2.zero, Vector2.zero).gameObject;
        var settingsCanvas = settingsPanel.AddComponent<Canvas>();
        settingsCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        settingsCanvas.sortingOrder = 1000;
        var scaler = settingsPanel.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        settingsPanel.AddComponent<GraphicRaycaster>();
        Stretch(settingsPanel.GetComponent<RectTransform>());
        settingsCanvasGroup = settingsPanel.AddComponent<CanvasGroup>();
        var fitted = MenuReferenceLayout.Create(settingsPanel.transform, "Settings Reference Layout");
        DBlock(fitted, "Soft Shadow", 68, 80, 1790, 944, new Color(0,0,0,0.65f));
        var outer = DBlock(fitted, "Console Chassis", 64, 64, 1792, 952, DesignLine);
        settingsCard = outer.rectTransform;
        settingsCard.pivot = Vector2.one * 0.5f;
        settingsCard.anchoredPosition = new Vector2(960, -540);
        var face = DBlock(outer.transform, "Console Face", 2, 2, 1788, 948, new Color(0.038f,0.029f,0.018f));
        var sidebar = DBlock(face.transform, "Navigation Well", 0, 0, 280, 948, new Color(0.027f,0.019f,0.011f));
        DBlock(face.transform, "Top Accent", 280, 0, 1508, 4, DesignAmber);
        DBlock(face.transform, "Navigation Divider", 280, 0, 1, 948, DesignLine);
        DText(sidebar.transform, "67", 28, 42, 215, 118, 104, DesignAmber, true);
        DText(sidebar.transform, "BUNKER CONTROL", 32, 163, 220, 24, 16, DesignText, true);
        DText(sidebar.transform, "TERMINAL / SYSTEM 067", 32, 194, 220, 20, 12, DesignMuted);
        DBlock(sidebar.transform, "Serial Rule", 32, 236, 216, 1, DesignLine);

        for (int i = 0; i < 3; i++)
        {
            int tab = i;
            var entry = DBlock(sidebar.transform, "Settings Tab " + i, 16, 274+i*104, 248, 88, DesignRaised);
            tabFaces[i] = entry;
            tabMarkers[i] = DBlock(entry.transform, "Active Marker", 0, 0, 3, 88, DesignAmber);
            var button = DesignButton(entry, () => SelectSettingsTab(tab));
            DText(entry.transform, "0"+(i+1), 18, 14, 36, 26, 15, DesignMuted);
            tabTitles[i] = DText(entry.transform, "", 60, 15, 178, 28, 19, DesignText, true);
            tabDescriptions[i] = DText(entry.transform, "", 60, 48, 178, 26, 12, DesignMuted);
        }
        DText(sidebar.transform, "●  LOCAL CONNECTION", 32, 768, 220, 24, 13, DesignGold);
        DText(sidebar.transform, "67 SECONDS\nCONTROL PANEL / v.02", 32, 813, 220, 62, 13, DesignMuted);
        DBlock(sidebar.transform, "Side Bottom Rule", 32, 902, 216, 1, DesignLine);

        DText(face.transform, "SYSTEM / CONFIGURATION", 322, 30, 850, 24, 14, DesignMuted);
        settingsHeader = DText(face.transform, "", 322, 65, 1200, 65, 48, DesignText, true);
        statusBadge = DText(face.transform, "●  ONLINE", 1514, 39, 180, 26, 14, DesignGold);
        var x = DBlock(face.transform, "Close X", 1712, 28, 48, 48, DesignRaised);
        DesignButton(x, HideSettings);
        var cross = DText(x.transform, "×", 0, 0, 48, 48, 30, DesignText);
        cross.alignment = TextAnchor.MiddleCenter;
        DBlock(face.transform, "Header Divider", 322, 150, 1420, 1, DesignLine);
        pageTitle = DText(face.transform, "", 322, 171, 1100, 36, 24, DesignAmber, true);
        pageSubtitle = DText(face.transform, "", 322, 211, 1390, 28, 15, DesignMuted);
        for (int i=0;i<3;i++)
            designPages[i] = DBlock(face.transform, "Settings Page "+i, 322, 266, 1420, 576, Color.clear).gameObject;

        BuildDisplayPage(designPages[0].transform);
        BuildInterfacePage(designPages[1].transform);
        BuildSignalPage(designPages[2].transform);

        DBlock(face.transform, "Footer Divider", 322, 860, 1420, 1, DesignLine);
        saveHint = DText(face.transform, "", 322, 887, 870, 38, 14, DesignMuted);
        var reset = CreateCompactButton("Reset Button", face.transform, new Vector2(0.752f,0.048f), new Vector2(240,56), out resetLabel);
        reset.onClick.AddListener(ResetDefaults);
        var close = CreateCompactButton("Done Button", face.transform, new Vector2(0.911f,0.048f), new Vector2(276,56), out closeLabel);
        close.onClick.AddListener(HideSettings);
        closeLabel.color = new Color(0.065f,0.043f,0.018f);
        close.targetGraphic.color = DesignAmber;

        SelectSettingsTab(0);
        panelOpen = false;
        settingsCanvasGroup.alpha = 0;
        settingsCanvasGroup.blocksRaycasts = false;
        settingsCanvasGroup.interactable = false;
        settingsPanel.SetActive(false);
    }

    void BuildDisplayPage(Transform parent)
    {
        graphicsHeader = DText(parent,"",12,0,700,28,13,DesignMuted);
        CreateSelectorRow(parent,"Quality Row",new Vector2(0.27f,0.85f),out qualityLabel,out qualityValue,()=>ChangeQuality(-1),()=>ChangeQuality(1),740);
        brightnessSlider = CreateSliderRow(parent,"Brightness Row",new Vector2(0.27f,0.65f),out brightnessLabel,out brightnessValue,()=>ChangeBrightness(-0.1f),()=>ChangeBrightness(0.1f),740);
        brightnessSlider.onValueChanged.AddListener(SetBrightness);
        CreateSelectorRow(parent,"FPS Row",new Vector2(0.27f,0.45f),out fpsLabel,out fpsValue,()=>ChangeFps(-1),()=>ChangeFps(1),740);
        CreateToggleRow(parent,"VSync Row",new Vector2(0.27f,0.25f),out vSyncLabel,out vSyncValue,ToggleVSync,740);
        CreateToggleRow(parent,"Fullscreen Row",new Vector2(0.27f,0.05f),out fullscreenLabel,out fullscreenValue,ToggleFullscreen,740);

        var monitor = DBlock(parent,"Display Calibration",812,0,608,365,new Color(0.025f,0.018f,0.010f));
        FrameCorners(monitor.rectTransform);
        DText(monitor.transform,"DISPLAY / CALIBRATION",24,22,560,24,14,DesignMuted);
        var graphic = new GameObject("Calibration Pattern",typeof(RectTransform),typeof(CanvasRenderer)).AddComponent<MenuConsoleGraphic>();
        graphic.transform.SetParent(monitor.transform,false);
        PlaceDesignRect(graphic.rectTransform,24,70,560,230);
        graphic.mode=1;
        graphic.color=DesignAmber;
        graphic.raycastTarget=false;
        displayHint = DText(monitor.transform,"",24,317,560,30,14,DesignMuted);
        var stats = DBlock(parent,"System Telemetry",812,390,608,160,DesignRaised);
        statsHeader=DText(stats.transform,"",24,22,560,26,13,DesignMuted);
        statsValue=DText(stats.transform,"",24,64,560,64,20,DesignGold,true);
    }

    void BuildInterfacePage(Transform parent)
    {
        interfaceHeader=DText(parent,"",12,0,700,28,13,DesignMuted);
        musicVolumeSlider=CreateSliderRow(parent,"Music Row",new Vector2(0.27f,0.83f),out musicLabel,out musicValue,()=>ChangeMusicVolume(-0.1f),()=>ChangeMusicVolume(0.1f),740);
        musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        var lang=DBlock(parent,"Language Panel",12,198,740,210,DesignRaised);
        languageHeader=DText(lang.transform,"",24,20,690,34,19,DesignText);
        CreateLanguageButton(lang.transform,0,"RU",0.18f);
        CreateLanguageButton(lang.transform,1,"UA",0.50f);
        CreateLanguageButton(lang.transform,2,"EN",0.82f);
        audioHint=DText(parent,"",36,438,700,100,17,DesignMuted);
        var audio=DBlock(parent,"Audio Monitor",812,0,608,550,new Color(0.025f,0.018f,0.010f));
        FrameCorners(audio.rectTransform);
        DText(audio.transform,"AUDIO / MONITOR",24,22,560,24,14,DesignMuted);
        DText(audio.transform,"67",24,76,560,132,114,DesignText,true);
        DText(audio.transform,"MENU THEME / STEREO",24,233,560,30,16,DesignAmber);
        var meter=new GameObject("Audio Wave",typeof(RectTransform),typeof(CanvasRenderer)).AddComponent<MenuConsoleGraphic>();
        meter.transform.SetParent(audio.transform,false);
        PlaceDesignRect(meter.rectTransform,24,300,560,140);
        meter.mode=2;
        meter.audioSource=menuMusicSource;
        meter.color=DesignGold;
        meter.raycastTarget=false;
        string format=menuMusicClip!=null?(menuMusicClip.frequency/1000f).ToString("0.0",System.Globalization.CultureInfo.InvariantCulture)+" kHz  /  "+menuMusicClip.channels+" CH  /  LOOP":"STEREO / LOOP";
        DText(audio.transform,format,24,480,560,28,14,DesignMuted);
    }

    void BuildSignalPage(Transform parent)
    {
        effectsHeader=DText(parent,"",12,0,700,28,13,DesignMuted);
        CreateSelectorRow(parent,"Menu Effects Quality Row",new Vector2(0.27f,0.85f),out menuFxQualityLabel,out menuFxQualityValue,()=>ChangeMenuFxQuality(-1),()=>ChangeMenuFxQuality(1),740);
        menuFxIntensitySlider=CreateSliderRow(parent,"Menu Effects Intensity Row",new Vector2(0.27f,0.63f),out menuFxIntensityLabel,out menuFxIntensityValue,()=>ChangeMenuFxIntensity(-0.1f),()=>ChangeMenuFxIntensity(0.1f),740);
        menuFxIntensitySlider.onValueChanged.AddListener(SetMenuFxIntensity);
        CreateToggleRow(parent,"Menu Glitch Toggle Row",new Vector2(0.27f,0.41f),out menuGlitchEnabledLabel,out menuGlitchEnabledValue,ToggleMenuGlitch,740);
        menuGlitchNoiseSlider=CreateSliderRow(parent,"Menu Glitch Noise Row",new Vector2(0.27f,0.19f),out menuGlitchNoiseLabel,out menuGlitchNoiseValue,()=>ChangeMenuGlitchNoise(-0.1f),()=>ChangeMenuGlitchNoise(0.1f),740);
        menuGlitchNoiseSlider.onValueChanged.AddListener(SetMenuGlitchNoise);
        menuFxDescription=DText(parent,"",24,552,730,22,13,DesignMuted);

        var monitor=DBlock(parent,"Signal Preview Monitor",812,0,608,416,new Color(0.023f,0.016f,0.008f));
        FrameCorners(monitor.rectTransform);
        previewTitle=DText(monitor.transform,"",24,21,560,24,14,DesignGold);
        signalPreview=monitor.gameObject.AddComponent<MenuSignalPreview>();
        signalPreview.Build(uiFont);
        previewHint=DText(parent,"",836,433,560,38,14,DesignMuted);
        previewButton=CreateCompactButton("Preview Glitch",parent,new Vector2(0.786f,0.10f),new Vector2(560,62),out previewButtonLabel);
        previewButton.onClick.AddListener(()=> { signalPreview.Trigger(); if(menuEffects!=null) menuEffects.PlayPreviewNoise(); });
    }

    public void SelectSettingsTab(int index)
    {
        settingsTab=Mathf.Clamp(index,0,2);
        for(int i=0;i<3;i++)
        {
            if(designPages[i]!=null) designPages[i].SetActive(i==settingsTab);
            if(tabFaces[i]!=null) tabFaces[i].color=i==settingsTab?new Color(0.18f,0.12f,0.045f):new Color(0.04f,0.029f,0.016f);
            if(tabMarkers[i]!=null) tabMarkers[i].enabled=i==settingsTab;
            if(tabTitles[i]!=null) tabTitles[i].color=i==settingsTab?DesignAmber:DesignText;
        }
        RefreshDesignLabels();
        if(UnityEngine.EventSystems.EventSystem.current!=null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
    }

    void RefreshDesignLabels()
    {
        if(pageTitle==null) return;
        string[][] titles={new[]{"ЭКРАН","ЭКРАН","DISPLAY"},new[]{"ИНТЕРФЕЙС","ІНТЕРФЕЙС","INTERFACE"},new[]{"СИГНАЛ","СИГНАЛ","SIGNAL"}};
        string[][] descriptions={new[]{"Графика и изображение","Графіка та зображення","Graphics & image"},new[]{"Язык и звук","Мова та звук","Language & audio"},new[]{"Эффекты и глитч","Ефекти та глітч","Effects & glitch"}};
        for(int i=0;i<3;i++){tabTitles[i].text=titles[i][(int)language];tabDescriptions[i].text=descriptions[i][(int)language];}
        string[] headings={L("Изображение и производительность","Зображення та продуктивність","Image & performance"),L("Звук и язык интерфейса","Звук та мова інтерфейсу","Audio & interface language"),L("Управление сигналом","Керування сигналом","Signal control")};
        string[] subtitles={L("Настройте изображение под свой экран.","Налаштуйте зображення під свій екран.","Tune the image for your display."),L("Параметры применяются сразу и сохраняются автоматически.","Параметри застосовуються одразу та зберігаються автоматично.","Changes apply immediately and save automatically."),L("Проверьте помехи на мониторе справа перед возвращением в меню.","Перевірте перешкоди на моніторі праворуч перед поверненням до меню.","Test interference on the monitor before returning to the menu.")};
        pageTitle.text=headings[settingsTab];pageSubtitle.text=subtitles[settingsTab];
        displayHint.text=L("Различимы все оттенки? Яркость настроена.","Розрізняєте всі відтінки? Яскравість налаштована.","All shades visible? Your brightness is ready.");
        audioHint.text=L("Музыка создаёт атмосферу убежища.\nЯзык меняет подписи и главное меню.","Музика створює атмосферу сховища.\nМова змінює підписи та головне меню.","Music sets the shelter atmosphere.\nLanguage updates labels and the main menu.");
        previewTitle.text=L("●  МОНИТОР СИГНАЛА / LIVE","●  МОНІТОР СИГНАЛУ / LIVE","●  SIGNAL MONITOR / LIVE");
        previewHint.text=L("Локальный предпросмотр. Настройки остаются читаемыми.","Локальний перегляд. Налаштування залишаються читабельними.","Local preview. Controls remain clear and readable.");
        previewButtonLabel.text=L("▷  ПРОВЕРИТЬ ГЛИТЧ","▷  ПЕРЕВІРИТИ ГЛІТЧ","▷  TEST GLITCH");
        bool effectsOn=menuFxQuality>0 && menuFxIntensity>0.001f;
        previewButton.interactable=effectsOn && menuGlitchEnabled;
        menuFxIntensitySlider.interactable=menuFxQuality>0;
        menuGlitchNoiseSlider.interactable=effectsOn && menuGlitchEnabled;
        if(vSyncValue!=null)vSyncValue.color=vSync?DesignGold:DesignMuted;
        if(fullscreenValue!=null)fullscreenValue.color=fullscreen?DesignGold:DesignMuted;
        if(menuGlitchEnabledValue!=null)menuGlitchEnabledValue.color=menuGlitchEnabled?DesignGold:DesignMuted;
        if(menuFxIntensityValue!=null)menuFxIntensityValue.color=menuFxQuality>0?DesignAmber:DesignMuted;
        if(menuGlitchNoiseValue!=null)menuGlitchNoiseValue.color=effectsOn && menuGlitchEnabled?DesignAmber:DesignMuted;
        if(signalPreview!=null)signalPreview.SetLanguage((int)language);
    }

    string L(string ru,string ua,string en){return language==Language.Ukrainian?ua:language==Language.English?en:ru;}

    void OnDestroy()
    {
        if(settingsPanel != null) Destroy(settingsPanel);
    }

    void CreateSelectorRow(Transform parent,string name,Vector2 anchor,out Text label,out Text value,UnityAction previous,UnityAction next,float rowWidth=650)
    {
        var row=CreateBlock(name,parent,DesignRaised,anchor,new Vector2(rowWidth,94),Vector2.zero);
        label=CreateText("Label",row.transform,"",20,DesignText,new Vector2(0.285f,0.5f),new Vector2(rowWidth*0.50f,60));label.alignment=TextAnchor.MiddleLeft;
        CreateArrowButton(row.transform,new Vector2(0.62f,0.5f),"‹",previous);
        value=CreateText("Value",row.transform,"",20,DesignAmber,new Vector2(0.79f,0.5f),new Vector2(rowWidth*0.23f,60));
        CreateArrowButton(row.transform,new Vector2(0.95f,0.5f),"›",next);
        DBlock(row.transform,"Row Rule",24,93,rowWidth-48,1,DesignLine).raycastTarget=false;
    }

    Slider CreateSliderRow(Transform parent,string name,Vector2 anchor,out Text label,out Text value,UnityAction decrease,UnityAction increase,float rowWidth=650)
    {
        var row=CreateBlock(name,parent,DesignRaised,anchor,new Vector2(rowWidth,104),Vector2.zero);
        label=CreateText("Label",row.transform,"",20,DesignText,new Vector2(0.39f,0.73f),new Vector2(rowWidth*0.71f,32));label.alignment=TextAnchor.MiddleLeft;
        value=CreateText("Value",row.transform,"",20,DesignAmber,new Vector2(0.9f,0.73f),new Vector2(96,32));
        var go=new GameObject(name.Replace(" Row"," Slider"),typeof(RectTransform),typeof(Slider));go.transform.SetParent(row.transform,false);
        var slider=go.GetComponent<Slider>();
        SetRect(slider.transform as RectTransform,new Vector2(0.5f,0.30f),new Vector2(rowWidth-180,38),Vector2.zero);
        var track=CreateBlock("Track",go.transform,DesignLine,Vector2.one*0.5f,new Vector2(rowWidth-180,4),Vector2.zero);track.raycastTarget=false;
        var area=new GameObject("Fill Area",typeof(RectTransform)).GetComponent<RectTransform>();area.SetParent(go.transform,false);Stretch(area);area.offsetMin=new Vector2(0,17);area.offsetMax=new Vector2(0,-17);
        var fill=CreateBlock("Fill",area,DesignAmber,Vector2.one*0.5f,Vector2.zero,Vector2.zero);Stretch(fill.rectTransform);fill.raycastTarget=false;
        var handleArea=new GameObject("Handle Area",typeof(RectTransform)).GetComponent<RectTransform>();handleArea.SetParent(go.transform,false);Stretch(handleArea);
        var handle=CreateBlock("Handle",handleArea,DesignText,Vector2.one*0.5f,new Vector2(10,24),Vector2.zero);
        slider.fillRect=fill.rectTransform;slider.handleRect=handle.rectTransform;slider.targetGraphic=handle;slider.minValue=0;slider.maxValue=1;slider.colors=ButtonColors();
        CreateArrowButton(row.transform,new Vector2(0.06f,0.30f),"−",decrease);CreateArrowButton(row.transform,new Vector2(0.94f,0.30f),"+",increase);
        return slider;
    }

    void CreateToggleRow(Transform parent,string name,Vector2 anchor,out Text label,out Text value,UnityAction action,float rowWidth=650)
    {
        var row=CreateBlock(name,parent,DesignRaised,anchor,new Vector2(rowWidth,94),Vector2.zero);
        label=CreateText("Label",row.transform,"",19,DesignText,new Vector2(0.36f,0.5f),new Vector2(rowWidth*0.65f,64));label.alignment=TextAnchor.MiddleLeft;
        var toggle=CreateBlock("Toggle",row.transform,new Color(0.22f,0.14f,0.055f),new Vector2(0.86f,0.5f),new Vector2(150,46),Vector2.zero);DesignButton(toggle,action);
        value=CreateText("Value",toggle.transform,"",18,DesignGold,Vector2.one*0.5f,new Vector2(138,40));
    }

    void CreateArrowButton(Transform parent,Vector2 anchor,string arrow,UnityAction action)
    {
        var face=CreateBlock("Arrow "+arrow,parent,new Color(0.16f,0.10f,0.04f),anchor,new Vector2(40,42),Vector2.zero);DesignButton(face,action);
        CreateText("Glyph",face.transform,arrow,28,DesignAmber,Vector2.one*0.5f,new Vector2(38,40));
    }

    void CreateLanguageButton(Transform parent,int index,string code,float xAnchor)
    {
        var frame=CreateBlock("Language "+code,parent,DesignLine,new Vector2(xAnchor,0.34f),new Vector2(210,72),Vector2.zero);languageFrames[index]=frame;
        var face=CreateBlock("Face",frame.transform,DesignRaised,Vector2.one*0.5f,new Vector2(206,68),Vector2.zero);languageFaces[index]=face;DesignButton(frame,()=>SetLanguage(index)).targetGraphic=face;
        languageCodes[index]=CreateText("Code",face.transform,code,26,DesignText,Vector2.one*0.5f,new Vector2(190,56));
    }

    Button CreateCompactButton(string name,Transform parent,Vector2 anchor,Vector2 size,out Text label)
    {
        var frame=CreateBlock(name,parent,DesignLine,anchor,size,Vector2.zero);
        var face=CreateBlock("Face",frame.transform,DesignRaised,Vector2.one*0.5f,size-Vector2.one*2,Vector2.zero);
        var button=DesignButton(frame,null);button.targetGraphic=face;
        label=CreateText("Label",face.transform,"",18,DesignAmber,Vector2.one*0.5f,size-new Vector2(20,12));return button;
    }

    Button DesignButton(Image face,UnityAction action)
    {
        var button=face.gameObject.AddComponent<Button>();button.targetGraphic=face;button.colors=ButtonColors();button.navigation=new Navigation{mode=Navigation.Mode.None};if(action!=null)button.onClick.AddListener(action);return button;
    }
    Image DBlock(Transform parent,string name,float x,float y,float w,float h,Color tint)
    {
        var image=CreateBlock(name,parent,tint,Vector2.zero,new Vector2(w,h),Vector2.zero);PlaceDesignRect(image.rectTransform,x,y,w,h);return image;
    }
    Text DText(Transform parent,string value,float x,float y,float w,float h,int size,Color tint,bool bold=false)
    {
        var text=CreateText(value.Length>0?value:"Design Label",parent,value,size,tint,Vector2.zero,new Vector2(w,h));PlaceDesignRect(text.rectTransform,x,y,w,h);text.alignment=TextAnchor.MiddleLeft;text.fontStyle=bold?FontStyle.Bold:FontStyle.Normal;return text;
    }
    static void PlaceDesignRect(RectTransform rect,float x,float y,float w,float h)
    {
        rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(w,h);
    }
    void FrameCorners(RectTransform rect)
    {
        float w=rect.rect.width,h=rect.rect.height;
        foreach(var corner in new[]{new Vector2(0,0),new Vector2(w-18,0),new Vector2(0,h-18),new Vector2(w-18,h-18)})
        {
            DBlock(rect,"Corner",corner.x,corner.y<1?0:h-1,18,1,DesignAmber).raycastTarget=false;
            DBlock(rect,"Corner",corner.x<1?0:w-1,corner.y,1,18,DesignAmber).raycastTarget=false;
        }
    }
}
