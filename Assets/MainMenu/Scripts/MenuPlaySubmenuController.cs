using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class MenuPlaySubmenuController : MonoBehaviour
{
    public Transform SubmenuRoot => submenuRoot != null ? submenuRoot.transform : null;
    public bool IsTransitioning => transitioning;

    const string CharacterPrefKey = "MenuSelectedCharacterV1";
    const string CreditsPrefKey = "MenuStoreCreditsV1";
    const string PurchasesPrefKey = "MenuStorePurchasesV1";
    const string EquippedPrefKey = "MenuEquippedItemV1";
    const string ReturnToSubmenuKey = "MenuOpenSubmenuOnLoadV1";
    const string GameplaySceneName = "MainMap";

    static readonly Color Amber = new Color(1f, 0.60f, 0.055f, 1f);
    static readonly Color HotAmber = new Color(1f, 0.78f, 0.15f, 1f);
    static readonly Color Rust = new Color(0.70f, 0.22f, 0.018f, 1f);
    static readonly Color DarkRust = new Color(0.16f, 0.045f, 0.008f, 1f);
    static readonly Color Panel = new Color(0.032f, 0.021f, 0.015f, 0.995f);
    static readonly Color PanelRaised = new Color(0.060f, 0.034f, 0.018f, 1f);
    static readonly Color Muted = new Color(0.82f, 0.61f, 0.37f, 1f);

    MenuSettingsController owner;
    Button playButton;
    Font uiFont;
    bool initialized;
    bool submenuOpen;
    bool transitioning;

    GameObject submenuRoot;
    CanvasGroup submenuCanvas;
    GameObject fadeRoot;
    CanvasGroup fadeCanvas;
    RectTransform sectionHost;
    GameObject sectionRoot;

    Text terminalTitle;
    Text terminalCode;
    Text creditsText;
    Text statusText;
    Text navTitle;
    Text backLabel;
    readonly Text[] navLabels = new Text[4];
    readonly Image[] navFrames = new Image[4];
    readonly Image[] navFaces = new Image[4];

    int currentSection;
    int previewCharacter;
    int selectedCharacter;
    int credits;
    int purchaseMask;
    int equippedItem;
    bool operationRequested;
    string transientStatus;

    public void Initialize(MenuSettingsController settingsOwner, Button mainPlayButton, Font menuFont)
    {
        if (initialized) return;
        initialized = true;
        owner = settingsOwner;
        playButton = mainPlayButton;
        uiFont = menuFont;

        selectedCharacter = Mathf.Clamp(PlayerPrefs.GetInt(CharacterPrefKey, 0), 0, 2);
        previewCharacter = selectedCharacter;
        credits = Mathf.Max(0, PlayerPrefs.GetInt(CreditsPrefKey, 1250));
        purchaseMask = Mathf.Max(0, PlayerPrefs.GetInt(PurchasesPrefKey, 0));
        equippedItem = Mathf.Clamp(PlayerPrefs.GetInt(EquippedPrefKey, -1), -1, 3);
        if (equippedItem >= 0 && (purchaseMask & (1 << equippedItem)) == 0) equippedItem = -1;

        BuildSubmenu();
        playButton.onClick.AddListener(OpenSubmenu);
        RefreshLanguage();

        if (PlayerPrefs.GetInt(ReturnToSubmenuKey, 0) == 1)
        {
            PlayerPrefs.DeleteKey(ReturnToSubmenuKey);
            PlayerPrefs.Save();
            StartCoroutine(OpenRequestedSubmenu());
        }
    }

    IEnumerator OpenRequestedSubmenu()
    {
        yield return null;
        OpenSubmenu();
    }

    void OnDestroy()
    {
        if (playButton != null) playButton.onClick.RemoveListener(OpenSubmenu);
    }

    void Update()
    {
        if (submenuOpen && !transitioning && Input.GetKeyDown(KeyCode.Escape))
            CloseSubmenu();
    }

    public void RefreshLanguage()
    {
        if (!initialized || terminalTitle == null) return;

        terminalTitle.text = L("Бункерный терминал", "Бункерний термінал", "Bunker terminal").ToUpperInvariant();
        terminalCode.text = L("УЗЕЛ 67 // ПРЕДПОЛЁТНАЯ ПОДГОТОВКА", "ВУЗОЛ 67 // ПЕРЕДПОЛІТНА ПІДГОТОВКА", "NODE 67 // PRE-FLIGHT CONTROL");
        navTitle.text = L("РАЗДЕЛЫ", "РОЗДІЛИ", "SECTIONS");

        string[] labels =
        {
            L("ВЫБОР ПЕРСОНАЖА", "ВИБІР ПЕРСОНАЖА", "CHARACTER SELECT"),
            L("МАГАЗИН", "МАГАЗИН", "STORE"),
            L("СНАРЯЖЕНИЕ", "СПОРЯДЖЕННЯ", "LOADOUT"),
            L("ОПЕРАЦИЯ", "ОПЕРАЦІЯ", "OPERATION")
        };

        for (int i = 0; i < navLabels.Length; i++)
            if (navLabels[i] != null) navLabels[i].text = labels[i];

        backLabel.text = L("<  НАЗАД В МЕНЮ", "<  НАЗАД ДО МЕНЮ", "<  BACK TO MENU");
        RefreshCredits();
        UpdateNavigationState();
        RenderSection();
    }

    void OpenSubmenu()
    {
        if (!initialized || submenuOpen || transitioning) return;
        StartCoroutine(Transition(true));
    }

    void CloseSubmenu()
    {
        if (!submenuOpen || transitioning) return;
        StartCoroutine(Transition(false));
    }

    IEnumerator Transition(bool open)
    {
        transitioning = true;
        if (UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        fadeRoot.SetActive(true);
        fadeRoot.transform.SetAsLastSibling();
        fadeCanvas.alpha = 0f;
        fadeCanvas.blocksRaycasts = true;
        fadeCanvas.interactable = true;

        yield return Fade(0f, 1f, 0.32f);

        if (open)
        {
            currentSection = 0;
            transientStatus = L("КАНАЛ УПРАВЛЕНИЯ ОТКРЫТ", "КАНАЛ КЕРУВАННЯ ВІДКРИТО", "CONTROL CHANNEL ONLINE");
            submenuRoot.SetActive(true);
            submenuRoot.transform.SetAsLastSibling();
            fadeRoot.transform.SetAsLastSibling();
            submenuCanvas.alpha = 1f;
            submenuCanvas.blocksRaycasts = true;
            submenuCanvas.interactable = true;
            submenuOpen = true;
            RefreshLanguage();
        }
        else
        {
            submenuCanvas.blocksRaycasts = false;
            submenuCanvas.interactable = false;
            submenuRoot.SetActive(false);
            submenuOpen = false;
        }

        yield return Fade(1f, 0f, 0.38f);
        fadeCanvas.blocksRaycasts = false;
        fadeCanvas.interactable = false;
        fadeRoot.SetActive(false);
        transitioning = false;
        if (!open && UnityEngine.EventSystems.EventSystem.current != null && playButton != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(playButton.gameObject);
    }

    IEnumerator Fade(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);
            fadeCanvas.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }
        fadeCanvas.alpha = to;
    }

    void BuildSubmenu()
    {
        submenuRoot = CreateBlock("Play Submenu", transform, new Color(0.004f, 0.003f, 0.002f, 0.997f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero).gameObject;
        Stretch(submenuRoot.GetComponent<RectTransform>());
        submenuCanvas = submenuRoot.AddComponent<CanvasGroup>();
        var contentLayout = MenuReferenceLayout.Create(submenuRoot.transform, "Terminal Layout");

        var watermark = CreateText("67 Watermark", contentLayout, "67", 330, new Color(1f, 0.38f, 0.02f, 0.035f), new Vector2(0.79f, 0.51f), new Vector2(660f, 480f));
        watermark.fontStyle = FontStyle.Bold;
        watermark.raycastTarget = false;

        AddRail(contentLayout, "Top Amber Rail", new Vector2(0.5f, 0.975f), new Vector2(1820f, 7f), new Color(1f, 0.43f, 0.02f, 1f));
        AddRail(contentLayout, "Top Dark Rail", new Vector2(0.5f, 0.956f), new Vector2(1820f, 2f), new Color(0.45f, 0.13f, 0.01f, 1f));
        AddRail(contentLayout, "Bottom Rail", new Vector2(0.5f, 0.052f), new Vector2(1820f, 3f), new Color(0.50f, 0.16f, 0.015f, 0.9f));

        terminalTitle = CreateText("Terminal Title", contentLayout, "", 48, HotAmber, new Vector2(0.50f, 0.918f), new Vector2(1180f, 70f));
        terminalTitle.fontStyle = FontStyle.Bold;
        terminalCode = CreateText("Terminal Code", contentLayout, "", 17, Muted, new Vector2(0.50f, 0.868f), new Vector2(1180f, 28f));
        terminalCode.fontStyle = FontStyle.Bold;

        var navFrame = CreateBlock("Navigation Frame", contentLayout, Rust, new Vector2(0.155f, 0.47f), new Vector2(480f, 790f), Vector2.zero);
        var navPanel = CreateBlock("Navigation Panel", navFrame.transform, Panel, new Vector2(0.5f, 0.5f), new Vector2(466f, 776f), Vector2.zero);
        AddRail(navPanel.transform, "Navigation Header Rail", new Vector2(0.5f, 0.885f), new Vector2(405f, 3f), Rust);
        AddBolt(navPanel.transform, new Vector2(0.055f, 0.95f));
        AddBolt(navPanel.transform, new Vector2(0.945f, 0.95f));
        AddBolt(navPanel.transform, new Vector2(0.055f, 0.05f));
        AddBolt(navPanel.transform, new Vector2(0.945f, 0.05f));
        navTitle = CreateText("Navigation Title", navPanel.transform, "", 23, Amber, new Vector2(0.5f, 0.93f), new Vector2(370f, 48f));
        navTitle.fontStyle = FontStyle.Bold;

        float[] navY = { 0.78f, 0.63f, 0.48f, 0.33f };
        for (int i = 0; i < navLabels.Length; i++)
        {
            int captured = i;
            CreateNavButton(navPanel.transform, i, new Vector2(0.5f, navY[i]), () => SetSection(captured));
        }

        var backButton = CreateIndustrialButton("Back Button", navPanel.transform, new Vector2(0.5f, 0.145f), new Vector2(390f, 82f), out backLabel, CloseSubmenu);
        backButton.navigation = NoNavigation();

        var contentFrame = CreateBlock("Content Frame", contentLayout, new Color(0.72f, 0.24f, 0.015f, 1f), new Vector2(0.635f, 0.47f), new Vector2(1320f, 790f), Vector2.zero);
        var contentPanel = CreateBlock("Content Panel", contentFrame.transform, new Color(0.017f, 0.012f, 0.009f, 0.998f), new Vector2(0.5f, 0.5f), new Vector2(1306f, 776f), Vector2.zero);
        AddBolt(contentPanel.transform, new Vector2(0.025f, 0.958f));
        AddBolt(contentPanel.transform, new Vector2(0.975f, 0.958f));
        AddBolt(contentPanel.transform, new Vector2(0.025f, 0.042f));
        AddBolt(contentPanel.transform, new Vector2(0.975f, 0.042f));

        var hostObject = new GameObject("Section Host");
        hostObject.transform.SetParent(contentPanel.transform, false);
        sectionHost = hostObject.AddComponent<RectTransform>();
        sectionHost.anchorMin = new Vector2(0.03f, 0.10f);
        sectionHost.anchorMax = new Vector2(0.97f, 0.96f);
        sectionHost.offsetMin = Vector2.zero;
        sectionHost.offsetMax = Vector2.zero;
        sectionHost.localScale = Vector3.one;

        statusText = CreateText("Terminal Status", contentPanel.transform, "", 16, new Color(0.78f, 0.40f, 0.11f, 1f), new Vector2(0.5f, 0.055f), new Vector2(1120f, 34f));
        statusText.fontStyle = FontStyle.Bold;
        creditsText = CreateText("Credits", contentLayout, "", 19, Amber, new Vector2(0.86f, 0.866f), new Vector2(430f, 34f));
        creditsText.alignment = TextAnchor.MiddleRight;
        creditsText.fontStyle = FontStyle.Bold;

        fadeRoot = CreateBlock("Menu Fade", transform, Color.black, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero).gameObject;
        Stretch(fadeRoot.GetComponent<RectTransform>());
        fadeCanvas = fadeRoot.AddComponent<CanvasGroup>();
        fadeCanvas.alpha = 0f;
        fadeCanvas.blocksRaycasts = false;
        fadeCanvas.interactable = false;

        submenuCanvas.alpha = 1f;
        submenuCanvas.blocksRaycasts = false;
        submenuCanvas.interactable = false;
        submenuRoot.SetActive(false);
        fadeRoot.SetActive(false);
    }

    void CreateNavButton(Transform parent, int index, Vector2 anchor, UnityAction action)
    {
        var frame = CreateBlock("Navigation " + index, parent, new Color(0.39f, 0.11f, 0.008f, 1f), anchor, new Vector2(400f, 92f), Vector2.zero);
        var face = CreateBlock("Button Face", frame.transform, DarkRust, new Vector2(0.5f, 0.5f), new Vector2(390f, 82f), Vector2.zero);
        navFrames[index] = frame;
        navFaces[index] = face;

        var button = frame.gameObject.AddComponent<Button>();
        button.targetGraphic = face;
        button.transition = Selectable.Transition.ColorTint;
        button.colors = ButtonColors();
        button.navigation = NoNavigation();
        button.onClick.AddListener(action);

        navLabels[index] = CreateText("Label", face.transform, "", 21, Amber, new Vector2(0.5f, 0.5f), new Vector2(350f, 62f));
        navLabels[index].fontStyle = FontStyle.Bold;
        navLabels[index].raycastTarget = false;
    }

    void SetSection(int index)
    {
        if (transitioning) return;
        currentSection = Mathf.Clamp(index, 0, 3);
        transientStatus = "";
        UpdateNavigationState();
        RenderSection();
    }

    void UpdateNavigationState()
    {
        for (int i = 0; i < navFrames.Length; i++)
        {
            bool selected = i == currentSection;
            if (navFrames[i] != null) navFrames[i].color = selected ? new Color(1f, 0.45f, 0.025f, 1f) : new Color(0.39f, 0.11f, 0.008f, 1f);
            if (navFaces[i] != null) navFaces[i].color = selected ? new Color(0.20f, 0.065f, 0.010f, 1f) : DarkRust;
            if (navLabels[i] != null) navLabels[i].color = selected ? HotAmber : Amber;
        }
    }

    void RenderSection()
    {
        if (sectionHost == null) return;
        if (sectionRoot != null)
        {
            sectionRoot.SetActive(false);
            Destroy(sectionRoot);
        }

        sectionRoot = CreateBlock("Current Section", sectionHost, new Color(0f, 0f, 0f, 0f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero).gameObject;
        Stretch(sectionRoot.GetComponent<RectTransform>());

        if (currentSection == 0) RenderCharacterSection();
        else if (currentSection == 1) RenderStoreSection();
        else if (currentSection == 2) RenderLoadoutSection();
        else RenderOperationSection();

        statusText.text = string.IsNullOrEmpty(transientStatus)
            ? L("СИСТЕМА ГОТОВА", "СИСТЕМА ГОТОВА", "SYSTEM READY")
            : transientStatus;
        RefreshCredits();
        if (owner != null) owner.RefreshMenuEffects();
    }

    void RenderCharacterSection()
    {
        CreateSectionHeading(
            L("ВЫБОР ПЕРСОНАЖА", "ВИБІР ПЕРСОНАЖА", "CHARACTER SELECT"),
            L("Выберите специалиста для следующего вылета", "Оберіть фахівця для наступного вильоту", "Choose a specialist for the next deployment"));

        string[][] names =
        {
            new[] { "МЕХАНИК", "МЕХАНІК", "MECHANIC" },
            new[] { "РАЗВЕДЧИК", "РОЗВІДНИК", "SCOUT" },
            new[] { "ПОЛЕВОЙ МЕДИК", "ПОЛЬОВИЙ МЕДИК", "FIELD MEDIC" }
        };
        string[][] callSigns =
        {
            new[] { "ПОЗЫВНОЙ: КЛЮЧ", "ПОЗИВНИЙ: КЛЮЧ", "CALLSIGN: WRENCH" },
            new[] { "ПОЗЫВНОЙ: ИСКРА", "ПОЗИВНИЙ: ІСКРА", "CALLSIGN: SPARK" },
            new[] { "ПОЗЫВНОЙ: ПУЛЬС", "ПОЗИВНИЙ: ПУЛЬС", "CALLSIGN: PULSE" }
        };
        string[][] descriptions =
        {
            new[] { "Чинит оборудование быстрее и получает бонус к техническим проверкам.", "Швидше ремонтує обладнання та має бонус до технічних перевірок.", "Repairs equipment faster and gains a bonus on technical checks." },
            new[] { "Быстро перемещается, раньше замечает угрозы и тайники.", "Швидко пересувається, раніше помічає загрози й схованки.", "Moves quickly and spots threats and hidden caches earlier." },
            new[] { "Стабилизирует команду и эффективнее использует медикаменты.", "Стабілізує команду та ефективніше використовує медикаменти.", "Stabilizes the team and uses medical supplies more efficiently." }
        };

        int lang = LanguageIndex();
        var portraitFrame = CreateBlock("Operator Portrait Frame", sectionRoot.transform, Rust, new Vector2(0.245f, 0.46f), new Vector2(450f, 500f), Vector2.zero);
        var portrait = CreateBlock("Operator Portrait", portraitFrame.transform, new Color(0.055f, 0.030f, 0.016f, 1f), new Vector2(0.5f, 0.5f), new Vector2(436f, 486f), Vector2.zero);
        AddRail(portrait.transform, "Scanline Top", new Vector2(0.5f, 0.90f), new Vector2(380f, 2f), Rust);
        AddRail(portrait.transform, "Scanline Bottom", new Vector2(0.5f, 0.16f), new Vector2(380f, 2f), Rust);
        var serial = CreateText("Operator Serial", portrait.transform, "OPERATOR 0" + (previewCharacter + 1), 16, Muted, new Vector2(0.5f, 0.945f), new Vector2(390f, 30f));
        serial.fontStyle = FontStyle.Bold;

        Color operatorColor = previewCharacter == 0 ? Amber : previewCharacter == 1 ? new Color(1f, 0.38f, 0.04f, 1f) : new Color(0.95f, 0.72f, 0.20f, 1f);
        var portraitObject = new GameObject("Operator Illustration", typeof(RectTransform), typeof(CanvasRenderer));
        portraitObject.transform.SetParent(portrait.transform, false);
        var illustration = portraitObject.AddComponent<MenuOperatorPortrait>();
        illustration.role = previewCharacter;
        illustration.color = operatorColor;
        illustration.raycastTarget = false;
        SetRect(illustration.rectTransform, new Vector2(0.5f, 0.54f), new Vector2(340f, 340f), Vector2.zero);
        var name = CreateText("Operator Name", portrait.transform, names[previewCharacter][lang], 28, HotAmber, new Vector2(0.5f, 0.105f), new Vector2(400f, 52f));
        name.fontStyle = FontStyle.Bold;

        CreateSmallArrow(sectionRoot.transform, new Vector2(0.035f, 0.46f), "<", () => MoveCharacter(-1));
        CreateSmallArrow(sectionRoot.transform, new Vector2(0.455f, 0.46f), ">", () => MoveCharacter(1));

        var infoFrame = CreateBlock("Operator Data Frame", sectionRoot.transform, new Color(0.35f, 0.105f, 0.008f, 1f), new Vector2(0.735f, 0.50f), new Vector2(610f, 430f), Vector2.zero);
        var info = CreateBlock("Operator Data", infoFrame.transform, PanelRaised, new Vector2(0.5f, 0.5f), new Vector2(598f, 418f), Vector2.zero);
        var callSign = CreateText("Callsign", info.transform, callSigns[previewCharacter][lang], 22, Amber, new Vector2(0.5f, 0.88f), new Vector2(540f, 44f));
        callSign.fontStyle = FontStyle.Bold;
        var description = CreateText("Description", info.transform, descriptions[previewCharacter][lang], 19, new Color(0.88f, 0.70f, 0.49f, 1f), new Vector2(0.5f, 0.70f), new Vector2(520f, 95f));
        description.alignment = TextAnchor.MiddleLeft;

        int[,] stats = { { 62, 48, 95 }, { 58, 92, 66 }, { 96, 55, 70 } };
        AddStat(info.transform, L("ВЫЖИВАНИЕ", "ВИЖИВАННЯ", "SURVIVAL"), stats[previewCharacter, 0], 0.47f);
        AddStat(info.transform, L("МОБИЛЬНОСТЬ", "МОБІЛЬНІСТЬ", "MOBILITY"), stats[previewCharacter, 1], 0.31f);
        AddStat(info.transform, L("ТЕХНИКА", "ТЕХНІКА", "TECH"), stats[previewCharacter, 2], 0.15f);

        string confirmText = previewCharacter == selectedCharacter
            ? L("ВЫБРАН", "ОБРАНО", "SELECTED")
            : L("ВЫБРАТЬ ПЕРСОНАЖА", "ОБРАТИ ПЕРСОНАЖА", "SELECT CHARACTER");
        CreateIndustrialButton("Confirm Character", sectionRoot.transform, new Vector2(0.735f, 0.105f), new Vector2(520f, 82f), out Text confirmLabel, ConfirmCharacter);
        confirmLabel.text = confirmText;
    }

    void MoveCharacter(int direction)
    {
        previewCharacter = Wrap(previewCharacter + direction, 3);
        transientStatus = L("ДОСЬЕ ОБНОВЛЕНО", "ДОСЬЄ ОНОВЛЕНО", "DOSSIER UPDATED");
        RenderSection();
    }

    void ConfirmCharacter()
    {
        selectedCharacter = previewCharacter;
        PlayerPrefs.SetInt(CharacterPrefKey, selectedCharacter);
        PlayerPrefs.Save();
        transientStatus = L("ПЕРСОНАЖ НАЗНАЧЕН В ЭКИПАЖ", "ПЕРСОНАЖА ПРИЗНАЧЕНО ДО ЕКІПАЖУ", "CHARACTER ASSIGNED TO CREW");
        RenderSection();
    }

    void RenderStoreSection()
    {
        CreateSectionHeading(
            L("МАГАЗИН СНАРЯЖЕНИЯ", "МАГАЗИН СПОРЯДЖЕННЯ", "EQUIPMENT STORE"),
            L("Запасы убежища // цены фиксированы", "Запаси сховища // ціни фіксовані", "Shelter stock // fixed prices"));

        string[][] itemNames =
        {
            new[] { "УСИЛЕННЫЙ ФИЛЬТР", "ПОСИЛЕНИЙ ФІЛЬТР", "REINFORCED FILTER" },
            new[] { "ПОЛЕВАЯ АПТЕЧКА", "ПОЛЬОВА АПТЕЧКА", "FIELD MEDKIT" },
            new[] { "БАТАРЕЯ МК-II", "БАТАРЕЯ МК-II", "BATTERY MK-II" },
            new[] { "БРОНЕПЛАСТИНА", "БРОНЕПЛАСТИНА", "ARMOR PLATE" }
        };
        string[][] itemDescriptions =
        {
            new[] { "+ запас воздуха", "+ запас повітря", "+ air reserve" },
            new[] { "+ восстановление", "+ відновлення", "+ recovery" },
            new[] { "+ время фонаря", "+ час ліхтаря", "+ flashlight time" },
            new[] { "+ защита корпуса", "+ захист корпусу", "+ body protection" }
        };
        int[] prices = { 350, 280, 180, 500 };
        Vector2[] anchors = { new Vector2(0.25f, 0.62f), new Vector2(0.75f, 0.62f), new Vector2(0.25f, 0.25f), new Vector2(0.75f, 0.25f) };
        int lang = LanguageIndex();

        for (int i = 0; i < 4; i++)
        {
            int captured = i;
            bool owned = (purchaseMask & (1 << i)) != 0;
            var frame = CreateBlock("Store Item " + i, sectionRoot.transform, owned ? new Color(0.65f, 0.36f, 0.03f, 1f) : Rust, anchors[i], new Vector2(535f, 205f), Vector2.zero);
            var card = CreateBlock("Card", frame.transform, owned ? new Color(0.09f, 0.055f, 0.018f, 1f) : PanelRaised, new Vector2(0.5f, 0.5f), new Vector2(523f, 193f), Vector2.zero);
            var itemId = CreateText("Item ID", card.transform, "ITEM 0" + (i + 1), 14, Muted, new Vector2(0.16f, 0.84f), new Vector2(145f, 28f));
            itemId.alignment = TextAnchor.MiddleLeft;
            var iconObject = new GameObject("Equipment Icon", typeof(RectTransform), typeof(CanvasRenderer));
            iconObject.transform.SetParent(card.transform, false);
            var icon = iconObject.AddComponent<MenuEquipmentIcon>();
            icon.item = i;
            icon.color = Amber;
            icon.raycastTarget = false;
            SetRect(icon.rectTransform, new Vector2(0.12f, 0.60f), new Vector2(76f, 76f), Vector2.zero);
            var itemName = CreateText("Item Name", card.transform, itemNames[i][lang], 21, HotAmber, new Vector2(0.59f, 0.64f), new Vector2(382f, 50f));
            itemName.fontStyle = FontStyle.Bold;
            var itemDescription = CreateText("Item Description", card.transform, itemDescriptions[i][lang], 17, new Color(0.84f, 0.64f, 0.41f, 1f), new Vector2(0.30f, 0.39f), new Vector2(280f, 35f));
            itemDescription.alignment = TextAnchor.MiddleLeft;

            string action = owned
                ? L("КУПЛЕНО", "ПРИДБАНО", "OWNED")
                : prices[i] + " CR";
            var button = CreateIndustrialButton("Buy Item", card.transform, new Vector2(0.78f, 0.23f), new Vector2(190f, 58f), out Text buyLabel, () => BuyItem(captured));
            buyLabel.text = action;
            buyLabel.fontSize = 18;
            button.interactable = !owned && credits >= prices[i];
            if (!owned && credits < prices[i]) buyLabel.text = L("НЕ ХВАТАЕТ CR", "БРАКУЄ CR", "NEED MORE CR");
        }
    }

    void BuyItem(int index)
    {
        int[] prices = { 350, 280, 180, 500 };
        if ((purchaseMask & (1 << index)) != 0)
        {
            transientStatus = L("ПРЕДМЕТ УЖЕ КУПЛЕН", "ПРЕДМЕТ УЖЕ ПРИДБАНО", "ITEM ALREADY OWNED");
        }
        else if (credits < prices[index])
        {
            transientStatus = L("НЕДОСТАТОЧНО КРЕДИТОВ", "НЕДОСТАТНЬО КРЕДИТІВ", "INSUFFICIENT CREDITS");
        }
        else
        {
            credits -= prices[index];
            purchaseMask |= 1 << index;
            PlayerPrefs.SetInt(CreditsPrefKey, credits);
            PlayerPrefs.SetInt(PurchasesPrefKey, purchaseMask);
            PlayerPrefs.Save();
            transientStatus = L("ПОКУПКА ДОБАВЛЕНА В СНАРЯЖЕНИЕ", "ПОКУПКУ ДОДАНО ДО СПОРЯДЖЕННЯ", "PURCHASE ADDED TO LOADOUT");
        }
        RenderSection();
    }

    void RenderLoadoutSection()
    {
        CreateSectionHeading(
            L("СНАРЯЖЕНИЕ ЭКИПАЖА", "СПОРЯДЖЕННЯ ЕКІПАЖУ", "CREW LOADOUT"),
            L("Назначьте один активный модуль перед вылетом", "Призначте один активний модуль перед вильотом", "Assign one active module before deployment"));

        string[][] characterNames =
        {
            new[] { "МЕХАНИК", "МЕХАНІК", "MECHANIC" },
            new[] { "РАЗВЕДЧИК", "РОЗВІДНИК", "SCOUT" },
            new[] { "ПОЛЕВОЙ МЕДИК", "ПОЛЬОВИЙ МЕДИК", "FIELD MEDIC" }
        };
        string[][] itemNames =
        {
            new[] { "УСИЛЕННЫЙ ФИЛЬТР", "ПОСИЛЕНИЙ ФІЛЬТР", "REINFORCED FILTER" },
            new[] { "ПОЛЕВАЯ АПТЕЧКА", "ПОЛЬОВА АПТЕЧКА", "FIELD MEDKIT" },
            new[] { "БАТАРЕЯ МК-II", "БАТАРЕЯ МК-II", "BATTERY MK-II" },
            new[] { "БРОНЕПЛАСТИНА", "БРОНЕПЛАСТИНА", "ARMOR PLATE" }
        };
        int lang = LanguageIndex();

        var crewFrame = CreateBlock("Crew Card Frame", sectionRoot.transform, Rust, new Vector2(0.22f, 0.47f), new Vector2(400f, 470f), Vector2.zero);
        var crewCard = CreateBlock("Crew Card", crewFrame.transform, PanelRaised, new Vector2(0.5f, 0.5f), new Vector2(388f, 458f), Vector2.zero);
        var crewLabel = CreateText("Crew Label", crewCard.transform, L("АКТИВНЫЙ ОПЕРАТОР", "АКТИВНИЙ ОПЕРАТОР", "ACTIVE OPERATOR"), 17, Muted, new Vector2(0.5f, 0.88f), new Vector2(350f, 34f));
        crewLabel.fontStyle = FontStyle.Bold;
        var crewPortraitObject = new GameObject("Crew Portrait", typeof(RectTransform), typeof(CanvasRenderer));
        crewPortraitObject.transform.SetParent(crewCard.transform, false);
        var crewPortrait = crewPortraitObject.AddComponent<MenuOperatorPortrait>();
        crewPortrait.role = selectedCharacter;
        crewPortrait.color = Amber;
        crewPortrait.raycastTarget = false;
        SetRect(crewPortrait.rectTransform, new Vector2(0.5f, 0.56f), new Vector2(265f, 265f), Vector2.zero);
        var crewName = CreateText("Crew Name", crewCard.transform, characterNames[selectedCharacter][lang], 27, HotAmber, new Vector2(0.5f, 0.25f), new Vector2(350f, 58f));
        crewName.fontStyle = FontStyle.Bold;
        var ready = CreateText("Crew Ready", crewCard.transform, L("СТАТУС: ГОТОВ", "СТАТУС: ГОТОВИЙ", "STATUS: READY"), 16, new Color(0.82f, 0.54f, 0.18f, 1f), new Vector2(0.5f, 0.10f), new Vector2(340f, 32f));

        var slotsFrame = CreateBlock("Loadout Slots", sectionRoot.transform, new Color(0.38f, 0.12f, 0.01f, 1f), new Vector2(0.70f, 0.47f), new Vector2(660f, 500f), Vector2.zero);
        var slots = CreateBlock("Slots", slotsFrame.transform, Panel, new Vector2(0.5f, 0.5f), new Vector2(648f, 488f), Vector2.zero);
        for (int i = 0; i < 4; i++)
        {
            int captured = i;
            bool owned = (purchaseMask & (1 << i)) != 0;
            bool equipped = equippedItem == i;
            float y = 0.83f - i * 0.21f;
            var slot = CreateBlock("Slot " + i, slots.transform, equipped ? new Color(0.20f, 0.095f, 0.018f, 1f) : new Color(0.055f, 0.031f, 0.018f, 1f), new Vector2(0.5f, y), new Vector2(590f, 86f), Vector2.zero);
            var slotId = CreateText("Slot ID", slot.transform, "0" + (i + 1), 25, equipped ? HotAmber : Muted, new Vector2(0.08f, 0.5f), new Vector2(60f, 54f));
            slotId.fontStyle = FontStyle.Bold;
            var itemName = CreateText("Slot Name", slot.transform, owned ? itemNames[i][lang] : L("ПУСТОЙ СЛОТ", "ПОРОЖНІЙ СЛОТ", "EMPTY SLOT"), 18, owned ? Amber : new Color(0.43f, 0.28f, 0.18f, 1f), new Vector2(0.40f, 0.5f), new Vector2(330f, 56f));
            itemName.alignment = TextAnchor.MiddleLeft;
            itemName.fontStyle = FontStyle.Bold;

            var equipButton = CreateIndustrialButton("Equip", slot.transform, new Vector2(0.83f, 0.5f), new Vector2(175f, 58f), out Text equipLabel, () => EquipItem(captured));
            equipLabel.text = !owned ? L("НЕТ", "НЕМАЄ", "NONE") : equipped ? L("СНЯТЬ", "ЗНЯТИ", "REMOVE") : L("ВЗЯТЬ", "ВЗЯТИ", "EQUIP");
            equipLabel.fontSize = 16;
            equipButton.interactable = owned;
        }
    }

    void EquipItem(int index)
    {
        if ((purchaseMask & (1 << index)) == 0)
        {
            transientStatus = L("СНАЧАЛА КУПИТЕ ПРЕДМЕТ", "СПОЧАТКУ ПРИДБАЙТЕ ПРЕДМЕТ", "PURCHASE THIS ITEM FIRST");
        }
        else
        {
            equippedItem = equippedItem == index ? -1 : index;
            PlayerPrefs.SetInt(EquippedPrefKey, equippedItem);
            PlayerPrefs.Save();
            transientStatus = equippedItem < 0
                ? L("АКТИВНЫЙ МОДУЛЬ СНЯТ", "АКТИВНИЙ МОДУЛЬ ЗНЯТО", "ACTIVE MODULE REMOVED")
                : L("МОДУЛЬ УСТАНОВЛЕН", "МОДУЛЬ ВСТАНОВЛЕНО", "MODULE EQUIPPED");
        }
        RenderSection();
    }

    void RenderOperationSection()
    {
        CreateSectionHeading(
            L("ОПЕРАЦИЯ: ПОСЛЕДНИЙ ВЫЛЕТ", "ОПЕРАЦІЯ: ОСТАННІЙ ВИЛІТ", "OPERATION: LAST FLIGHT"),
            L("Брифинг миссии // сектор закрыт", "Брифінг місії // сектор закрито", "Mission briefing // restricted sector"));

        var timerFrame = CreateBlock("Countdown Frame", sectionRoot.transform, Rust, new Vector2(0.23f, 0.48f), new Vector2(390f, 430f), Vector2.zero);
        var timer = CreateBlock("Countdown", timerFrame.transform, new Color(0.04f, 0.015f, 0.008f, 1f), new Vector2(0.5f, 0.5f), new Vector2(376f, 416f), Vector2.zero);
        var tLabel = CreateText("T Label", timer.transform, "T–", 34, Muted, new Vector2(0.5f, 0.80f), new Vector2(260f, 54f));
        tLabel.fontStyle = FontStyle.Bold;
        var number = CreateText("Countdown Number", timer.transform, "67", 170, new Color(1f, 0.32f, 0.035f, 1f), new Vector2(0.5f, 0.52f), new Vector2(330f, 205f));
        number.fontStyle = FontStyle.Bold;
        var seconds = CreateText("Seconds", timer.transform, L("СЕКУНД", "СЕКУНД", "SECONDS"), 26, Amber, new Vector2(0.5f, 0.22f), new Vector2(330f, 55f));
        seconds.fontStyle = FontStyle.Bold;

        var briefFrame = CreateBlock("Briefing Frame", sectionRoot.transform, new Color(0.38f, 0.12f, 0.01f, 1f), new Vector2(0.70f, 0.50f), new Vector2(665f, 430f), Vector2.zero);
        var brief = CreateBlock("Briefing", briefFrame.transform, PanelRaised, new Vector2(0.5f, 0.5f), new Vector2(653f, 418f), Vector2.zero);
        AddBriefRow(brief.transform, L("ЦЕЛЬ", "ЦІЛЬ", "OBJECTIVE"), L("ДОСТАВИТЬ КОНТЕЙНЕР", "ДОСТАВИТИ КОНТЕЙНЕР", "DELIVER THE CONTAINER"), 0.82f);
        AddBriefRow(brief.transform, L("СЕКТОР", "СЕКТОР", "SECTOR"), "K–17 / 04", 0.63f);
        AddBriefRow(brief.transform, L("УГРОЗА", "ЗАГРОЗА", "THREAT"), L("ВЫСОКАЯ", "ВИСОКА", "HIGH"), 0.44f);
        AddBriefRow(brief.transform, L("ЭКИПАЖ", "ЕКІПАЖ", "CREW"), CharacterNameForBrief(), 0.25f);

        string launchText = operationRequested
            ? L("ОЖИДАНИЕ ИГРОВОЙ СЦЕНЫ", "ОЧІКУВАННЯ ІГРОВОЇ СЦЕНИ", "AWAITING GAME SCENE")
            : L("НАЧАТЬ ОПЕРАЦИЮ", "ПОЧАТИ ОПЕРАЦІЮ", "START OPERATION");
        CreateIndustrialButton("Launch Operation", sectionRoot.transform, new Vector2(0.70f, 0.105f), new Vector2(600f, 84f), out Text launchLabel, RequestOperation);
        launchLabel.text = launchText;
    }

    string CharacterNameForBrief()
    {
        return selectedCharacter == 0 ? L("МЕХАНИК", "МЕХАНІК", "MECHANIC") : selectedCharacter == 1 ? L("РАЗВЕДЧИК", "РОЗВІДНИК", "SCOUT") : L("ПОЛЕВОЙ МЕДИК", "ПОЛЬОВИЙ МЕДИК", "FIELD MEDIC");
    }

    void RequestOperation()
    {
        if (operationRequested || transitioning) return;
        operationRequested = true;
        transientStatus = L("ЗАПУСК ОПЕРАЦИИ...", "ЗАПУСК ОПЕРАЦІЇ...", "LAUNCHING OPERATION...");
        RenderSection();
        StartCoroutine(LaunchGameplayScene());
    }

    IEnumerator LaunchGameplayScene()
    {
        transitioning = true;
        fadeRoot.SetActive(true);
        fadeRoot.transform.SetAsLastSibling();
        fadeCanvas.alpha = 0f;
        fadeCanvas.blocksRaycasts = true;
        fadeCanvas.interactable = true;
        yield return Fade(0f, 1f, 0.5f);
        Time.timeScale = 1f;
        SceneManager.LoadScene(GameplaySceneName);
    }

    void CreateSectionHeading(string title, string subtitle)
    {
        var titleText = CreateText("Section Title", sectionRoot.transform, title, 34, HotAmber, new Vector2(0.5f, 0.94f), new Vector2(1080f, 60f));
        titleText.fontStyle = FontStyle.Bold;
        var subtitleText = CreateText("Section Subtitle", sectionRoot.transform, subtitle, 17, Muted, new Vector2(0.5f, 0.875f), new Vector2(1050f, 34f));
        subtitleText.fontStyle = FontStyle.Bold;
        AddRail(sectionRoot.transform, "Section Divider", new Vector2(0.5f, 0.835f), new Vector2(1080f, 2f), new Color(0.48f, 0.15f, 0.012f, 1f));
    }

    void AddStat(Transform parent, string labelValue, int value, float y)
    {
        var label = CreateText("Stat Label", parent, labelValue, 15, Muted, new Vector2(0.19f, y), new Vector2(200f, 30f));
        label.alignment = TextAnchor.MiddleLeft;
        label.fontStyle = FontStyle.Bold;
        var background = CreateBlock("Stat Background", parent, new Color(0.12f, 0.045f, 0.014f, 1f), new Vector2(0.65f, y), new Vector2(310f, 18f), Vector2.zero);
        var fill = CreateBlock("Stat Fill", background.transform, Amber, new Vector2(0f, 0.5f), new Vector2(292f * value / 100f, 12f), new Vector2(8f, 0f));
        fill.rectTransform.pivot = new Vector2(0f, 0.5f);
        fill.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        fill.rectTransform.anchorMax = new Vector2(0f, 0.5f);
        fill.raycastTarget = false;
        var amount = CreateText("Stat Value", parent, value.ToString(), 15, HotAmber, new Vector2(0.94f, y), new Vector2(60f, 30f));
        amount.fontStyle = FontStyle.Bold;
    }

    void AddBriefRow(Transform parent, string labelValue, string value, float y)
    {
        var label = CreateText("Brief Label", parent, labelValue, 16, Muted, new Vector2(0.22f, y), new Vector2(220f, 38f));
        label.alignment = TextAnchor.MiddleLeft;
        label.fontStyle = FontStyle.Bold;
        var info = CreateText("Brief Value", parent, value, 18, Amber, new Vector2(0.69f, y), new Vector2(350f, 40f));
        info.alignment = TextAnchor.MiddleRight;
        info.fontStyle = FontStyle.Bold;
        AddRail(parent, "Brief Divider", new Vector2(0.5f, y - 0.085f), new Vector2(560f, 1f), new Color(0.28f, 0.09f, 0.01f, 1f));
    }

    void RefreshCredits()
    {
        if (creditsText != null)
            creditsText.text = L("КРЕДИТЫ: ", "КРЕДИТИ: ", "CREDITS: ") + credits.ToString("N0") + " CR";
    }

    string L(string russian, string ukrainian, string english)
    {
        int index = LanguageIndex();
        return index == 1 ? ukrainian : index == 2 ? english : russian;
    }

    int LanguageIndex()
    {
        return owner != null ? Mathf.Clamp(owner.CurrentLanguageIndex, 0, 2) : Mathf.Clamp(PlayerPrefs.GetInt("MenuLanguageV2", 0), 0, 2);
    }

    static int Wrap(int value, int count)
    {
        if (value < 0) return count - 1;
        if (value >= count) return 0;
        return value;
    }

    void CreateSmallArrow(Transform parent, Vector2 anchor, string glyph, UnityAction action)
    {
        CreateIndustrialButton("Character Arrow " + glyph, parent, anchor, new Vector2(70f, 105f), out Text label, action);
        label.text = glyph;
        label.fontSize = 38;
    }

    Button CreateIndustrialButton(string name, Transform parent, Vector2 anchor, Vector2 size, out Text label, UnityAction action)
    {
        var frame = CreateBlock(name, parent, new Color(0.69f, 0.24f, 0.012f, 1f), anchor, size, Vector2.zero);
        var face = CreateBlock("Button Face", frame.transform, DarkRust, new Vector2(0.5f, 0.5f), size - new Vector2(8f, 8f), Vector2.zero);
        var button = frame.gameObject.AddComponent<Button>();
        button.targetGraphic = face;
        button.transition = Selectable.Transition.ColorTint;
        button.colors = ButtonColors();
        button.navigation = NoNavigation();
        button.onClick.AddListener(action);
        label = CreateText("Label", face.transform, "", Mathf.RoundToInt(Mathf.Clamp(size.y * 0.27f, 16f, 24f)), Amber, new Vector2(0.5f, 0.5f), size - new Vector2(20f, 14f));
        label.fontStyle = FontStyle.Bold;
        label.raycastTarget = false;
        return button;
    }

    Image CreateBlock(string name, Transform parent, Color color, Vector2 anchor, Vector2 size, Vector2 offset)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var image = go.AddComponent<Image>();
        image.color = color;
        SetRect(image.rectTransform, anchor, size, offset);
        return image;
    }

    Text CreateText(string name, Transform parent, string value, int fontSize, Color color, Vector2 anchor, Vector2 dimensions)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<Text>();
        text.text = value;
        text.raycastTarget = false;
        text.font = uiFont != null ? uiFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        SetRect(text.rectTransform, anchor, dimensions, Vector2.zero);
        return text;
    }

    void AddRail(Transform parent, string name, Vector2 anchor, Vector2 size, Color color)
    {
        var rail = CreateBlock(name, parent, color, anchor, size, Vector2.zero);
        rail.raycastTarget = false;
    }

    void AddBolt(Transform parent, Vector2 anchor)
    {
        var bolt = CreateText("Frame Bolt", parent, "●", 23, new Color(0.58f, 0.30f, 0.10f, 1f), anchor, new Vector2(34f, 34f));
        bolt.raycastTarget = false;
    }

    static ColorBlock ButtonColors()
    {
        var colors = ColorBlock.defaultColorBlock;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.42f, 1.12f, 0.78f, 1f);
        colors.pressedColor = new Color(1.55f, 0.78f, 0.42f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.42f, 0.42f, 0.42f, 0.48f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.09f;
        return colors;
    }

    static Navigation NoNavigation()
    {
        var navigation = new Navigation { mode = Navigation.Mode.None };
        return navigation;
    }

    static void SetRect(RectTransform rect, Vector2 anchor, Vector2 size, Vector2 offset)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = offset;
        rect.localScale = Vector3.one;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }
}
