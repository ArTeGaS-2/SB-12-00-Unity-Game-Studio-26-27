using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public sealed class InventoryUI : MonoBehaviour
{
    private const int Columns = 5;
    private const int Rows = 5;
    private const int AvailableSlots = InventorySlotCount;
    private const int HotbarSlotCount = 4;
    private const int InventorySlotCount = Columns * Rows;
    private const float SlotSize = 38f;
    private const float Gap = 4f;
    private const int UiVersion = 21;
    private const float OpenAnimationDuration = 0.2f;
    private const float OpenAnimationStartScale = 0.82f;

    [SerializeField] private int uiVersion;
    [SerializeField] private Sprite backpackSprite;
    [SerializeField] private Sprite pencilSprite;
    private GameObject inventoryButton;
    private GameObject panel;
    private readonly ItemInfo[] inventoryItems = new ItemInfo[InventorySlotCount];
    private readonly ItemInfo[] hotbarItems = new ItemInfo[HotbarSlotCount];
    private readonly Transform[] inventorySlots = new Transform[InventorySlotCount];
    private readonly Transform[] hotbarSlots = new Transform[HotbarSlotCount];
    private readonly HashSet<Transform> dropTargetSlots = new HashSet<Transform>();
    private bool initialPencilPlaced;
    private GameObject draggedItemObject;
    private Transform draggedItemOriginalParent;
    private Vector3 draggedItemOriginalPosition;
    private int draggedItemIndex;
    private bool draggedItemIsHotbar;
    private bool draggedItemAccepted;
    private float openAnimationProgress;
    private bool isAnimatingOpen;

    private sealed class ItemInfo
    {
        public Sprite Icon;
        public string Name;
        public string Description;
        public string Effect;
        public string Action;
        public string Nutrition;
    }

    private ItemInfo hoveredItem;
    private WorldPickup aimedItem;
    private Font tooltipFont;

    private GameplayPauseMenuController pauseMenu;
    private CursorLockMode previousCursorLock;
    private bool previousCursorVisible;
    public bool MenuInputBlocked => pauseMenu != null && pauseMenu.BlocksGameplayInput;
    public bool GameplayInputBlocked => IsOpen || MenuInputBlocked;

    public bool IsOpen => panel != null && panel.activeSelf;

    public bool TryAddItem(Sprite item)
    {
        return TryAddItem(item, item != null ? item.name : null, null, null, null, null);
    }

    public bool TryAddItem(Sprite item, string displayName, string description, string effect, string action, string nutrition)
    {
        if (item == null)
            return false;

        if (panel == null)
            EnsureUI();

        ItemInfo[] destination = hotbarItems;
        int freeSlot = System.Array.IndexOf(destination, null);
        if (freeSlot < 0)
        {
            destination = inventoryItems;
            freeSlot = System.Array.IndexOf(destination, null, 0, AvailableSlots);
        }

        if (freeSlot < 0)
            return false;

        destination[freeSlot] = new ItemInfo
        {
            Icon = item,
            Name = displayName,
            Description = description,
            Effect = effect,
            Action = action,
            Nutrition = nutrition
        };
        RefreshItemVisuals();
        return true;
    }

    private void OnEnable()
    {
        pauseMenu = GetComponentInParent<GameplayPauseMenuController>();
        EnsureUI();
    }

    private void EnsureUI()
    {
        Transform existingButton = transform.Find("InventoryButton");
        Transform existingPanel = transform.Find("InventoryPanel");
        if (existingButton != null && existingPanel != null && uiVersion == UiVersion)
        {
            inventoryButton = existingButton.gameObject;
            panel = existingPanel.gameObject;
            Button existingButtonComponent = existingButton.GetComponent<Button>();
            if (existingButtonComponent != null)
            {
                existingButtonComponent.onClick.RemoveListener(ToggleInventory);
                existingButtonComponent.onClick.AddListener(ToggleInventory);
            }
            Transform existingCloseButton = existingPanel.Find("CloseButton");
            Button closeButtonComponent = existingCloseButton != null
                ? existingCloseButton.GetComponent<Button>()
                : null;
            if (closeButtonComponent != null)
            {
                closeButtonComponent.onClick.RemoveListener(CloseInventory);
                closeButtonComponent.onClick.AddListener(CloseInventory);
            }
            InitializeItemSlots(existingPanel);
            return;
        }

        if (existingButton != null)
            DestroyObject(existingButton.gameObject);
        if (existingPanel != null)
            DestroyObject(existingPanel.gameObject);

        BuildUI();
    }

    private static void DestroyObject(GameObject objectToDestroy)
    {
        if (Application.isPlaying)
            Destroy(objectToDestroy);
        else
            DestroyImmediate(objectToDestroy);
    }

    private void BuildUI()
    {
        GameObject buttonObject = CreatePanelObject("InventoryButton", transform);
        inventoryButton = buttonObject;
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(1f, 0.5f);
        buttonRect.anchorMax = new Vector2(1f, 0.5f);
        buttonRect.pivot = new Vector2(1f, 0.5f);
        buttonRect.anchoredPosition = new Vector2(-20f, 0f);
        buttonRect.sizeDelta = new Vector2(76f, 76f);

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = new Color(0.95f, 0.62f, 0.04f, 1f);
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(ToggleInventory);
        AddBackpackIcon(buttonObject.transform, backpackSprite);

        panel = CreatePanelObject("InventoryPanel", transform);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 0.5f);
        panelRect.anchorMax = new Vector2(1f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = new Vector2(-190f, 0f);
        panelRect.sizeDelta = new Vector2(320f, 350f);

        Image panelImage = panel.GetComponent<Image>();
        panelImage.color = Color.clear;
        panelImage.raycastTarget = false;
        Outline panelOutline = panel.AddComponent<Outline>();
        panelOutline.effectColor = new Color(0.62f, 0.66f, 0.72f, 0.95f);
        panelOutline.effectDistance = new Vector2(2f, -2f);
        Shadow panelShadow = panel.AddComponent<Shadow>();
        panelShadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
        panelShadow.effectDistance = new Vector2(8f, -8f);

        AddBackpackBackground(panel.transform, backpackSprite);
        GameObject slotsBackground = CreatePanelObject("SlotsBackground", panel.transform);
        Image slotsBackgroundImage = slotsBackground.GetComponent<Image>();
        slotsBackgroundImage.color = Color.clear;
        slotsBackgroundImage.raycastTarget = false;
        Outline slotsBackgroundOutline = slotsBackground.AddComponent<Outline>();
        slotsBackgroundOutline.effectColor = Color.clear;
        slotsBackgroundOutline.effectDistance = new Vector2(1f, -1f);
        RectTransform slotsBackgroundRect = slotsBackground.GetComponent<RectTransform>();
        slotsBackgroundRect.anchorMin = new Vector2(0.5f, 0.5f);
        slotsBackgroundRect.anchorMax = new Vector2(0.5f, 0.5f);
        slotsBackgroundRect.pivot = new Vector2(0.5f, 0.5f);
        slotsBackgroundRect.anchoredPosition = Vector2.zero;
        slotsBackgroundRect.sizeDelta = new Vector2(228f, 228f);

        GameObject gridObject = CreatePanelObject("Slots", panel.transform);
        gridObject.GetComponent<Image>().color = Color.clear;
        gridObject.GetComponent<Image>().raycastTarget = false;
        RectTransform gridRect = gridObject.GetComponent<RectTransform>();
        gridRect.anchorMin = new Vector2(0.5f, 0.5f);
        gridRect.anchorMax = new Vector2(0.5f, 0.5f);
        gridRect.pivot = new Vector2(0.5f, 0.5f);
        gridRect.anchoredPosition = new Vector2(0f, -10f);
        gridRect.sizeDelta = new Vector2(Columns * SlotSize + (Columns - 1) * Gap,
            Rows * SlotSize + (Rows - 1) * Gap);

        GridLayoutGroup grid = gridObject.AddComponent<GridLayoutGroup>();
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = Columns;
        grid.cellSize = new Vector2(SlotSize, SlotSize);
        grid.spacing = new Vector2(Gap, Gap);
        grid.childAlignment = TextAnchor.UpperLeft;

        for (int i = 0; i < Columns * Rows; i++)
        {
            GameObject slot = CreatePanelObject($"Slot_{i + 1}", gridObject.transform);
            inventorySlots[i] = slot.transform;
            AddDropTarget(slot.transform, i, false);
            Image slotImage = slot.GetComponent<Image>();
            bool isAvailable = i < AvailableSlots;
            slotImage.color = isAvailable
                ? new Color(0.84f, 0.61f, 0.06f, 0.96f)
                : new Color(0.48f, 0.42f, 0.22f, 0.9f);
            Outline outline = slot.AddComponent<Outline>();
            outline.effectColor = isAvailable
                ? new Color(1f, 0.92f, 0.38f, 1f)
                : new Color(0.26f, 0.22f, 0.1f, 0.85f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            if (!isAvailable)
                AddLockedMark(slot.transform);
        }

        SetupHotbarSlots();
        PlaceInitialPencil();
        RefreshItemVisuals();
        AddCloseButton(panel.transform, CloseInventory);

        uiVersion = UiVersion;
        panel.SetActive(false);
        inventoryButton.SetActive(true);
        panel.transform.localScale = Vector3.one;
    }

    private void InitializeItemSlots(Transform existingPanel)
    {
        Transform slotsRoot = existingPanel.Find("Slots");
        for (int i = 0; i < inventorySlots.Length; i++)
        {
            inventorySlots[i] = slotsRoot != null ? slotsRoot.Find($"Slot_{i + 1}") : null;
            if (inventorySlots[i] != null)
                AddDropTarget(inventorySlots[i], i, false);
        }

        SetupHotbarSlots();
        PlaceInitialPencil();
        RefreshItemVisuals();
    }

    private void PlaceInitialPencil()
    {
        if (initialPencilPlaced)
            return;

        if (pencilSprite == null)
            pencilSprite = Resources.Load<Sprite>("Pencil");
        if (pencilSprite == null)
            return;

        hotbarItems[0] = new ItemInfo
        {
            Icon = pencilSprite,
            Name = "Олівець",
            Description = "Приладдя для письма й малювання."
        };
        initialPencilPlaced = true;
    }

    private void ToggleInventory()
    {
        if (MenuInputBlocked || panel == null)
            return;

        if (panel.activeSelf)
        {
            CloseInventory();
            return;
        }

        previousCursorLock = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        inventoryButton.SetActive(false);
        panel.transform.localScale = Vector3.one * OpenAnimationStartScale;
        openAnimationProgress = 0f;
        isAnimatingOpen = Application.isPlaying;
        panel.SetActive(true);
    }

    private void Update()
    {
        if (MenuInputBlocked) return;
        if (Application.isPlaying && Keyboard.current != null &&
            Keyboard.current.iKey.wasPressedThisFrame)
        {
            ToggleInventory();
            return;
        }

        if (pauseMenu == null && Application.isPlaying && panel != null && panel.activeSelf &&
            Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CloseInventory();
            return;
        }

        if (isAnimatingOpen && panel != null)
        {
            openAnimationProgress += Time.unscaledDeltaTime / OpenAnimationDuration;
            float easedProgress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(openAnimationProgress));
            panel.transform.localScale = Vector3.one * Mathf.Lerp(OpenAnimationStartScale, 1f, easedProgress);
            if (openAnimationProgress >= 1f)
            {
                panel.transform.localScale = Vector3.one;
                isAnimatingOpen = false;
            }
        }
    }

    public void CloseInventory()
    {
        if (!IsOpen) return;
        if (draggedItemObject != null)
            EndItemDrag(draggedItemObject);
        Cursor.lockState = previousCursorLock;
        Cursor.visible = previousCursorVisible;
        isAnimatingOpen = false;
        hoveredItem = null;
        if (panel != null)
            panel.SetActive(false);
        if (inventoryButton != null)
            inventoryButton.SetActive(true);
    }

    private static GameObject CreatePanelObject(string objectName, Transform parent)
    {
        GameObject objectToCreate = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        objectToCreate.transform.SetParent(parent, false);
        return objectToCreate;
    }

    private void SetupHotbarSlots()
    {
        Transform bar = transform.Find("InventoryBar");
        for (int i = 0; i < HotbarSlotCount; i++)
        {
            hotbarSlots[i] = bar != null ? bar.Find($"Slot{i + 1}") : null;
            if (hotbarSlots[i] != null)
                AddDropTarget(hotbarSlots[i], i, true);
        }
    }

    private void AddDropTarget(Transform slot, int index, bool isHotbar)
    {
        if (!dropTargetSlots.Add(slot))
            return;

        EventTrigger target = slot.GetComponent<EventTrigger>();
        if (target == null)
            target = slot.gameObject.AddComponent<EventTrigger>();
        target.triggers ??= new System.Collections.Generic.List<EventTrigger.Entry>();

        EventTrigger.Entry dropEntry = new EventTrigger.Entry { eventID = EventTriggerType.Drop };
        dropEntry.callback.AddListener(eventData => HandleDrop((PointerEventData)eventData, index, isHotbar));
        target.triggers.Add(dropEntry);
        AddItemEvent(target, EventTriggerType.PointerEnter, eventData =>
        {
            if (draggedItemObject == null)
                hoveredItem = isHotbar ? hotbarItems[index] : inventoryItems[index];
        });
        AddItemEvent(target, EventTriggerType.PointerExit, eventData => hoveredItem = null);
    }

    private void RefreshItemVisuals()
    {
        for (int i = 0; i < inventorySlots.Length; i++)
        {
            if (inventorySlots[i] == null)
                continue;
            RemoveItemVisuals(inventorySlots[i]);
            if (inventoryItems[i] != null)
                AddItemVisual(inventorySlots[i], inventoryItems[i].Icon, false, i);
        }

        for (int i = 0; i < hotbarSlots.Length; i++)
        {
            if (hotbarSlots[i] == null)
                continue;
            RemoveItemVisuals(hotbarSlots[i]);
            if (hotbarItems[i] != null)
                AddItemVisual(hotbarSlots[i], hotbarItems[i].Icon, true, i);
        }
    }

    private static void RemoveItemVisuals(Transform slot)
    {
        for (int i = slot.childCount - 1; i >= 0; i--)
        {
            Transform child = slot.GetChild(i);
            if (child.name != "ItemIcon")
                continue;

            // Destroy is deferred during play, so hide old icons before drawing their replacements.
            child.gameObject.SetActive(false);
            DestroyObject(child.gameObject);
        }
    }

    private void AddItemVisual(Transform slot, Sprite sprite, bool isHotbar, int index)
    {
        GameObject itemObject = new GameObject("ItemIcon", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        itemObject.transform.SetParent(slot, false);
        RectTransform itemRect = itemObject.GetComponent<RectTransform>();
        itemRect.anchorMin = new Vector2(0.5f, 0.5f);
        itemRect.anchorMax = new Vector2(0.5f, 0.5f);
        itemRect.pivot = new Vector2(0.5f, 0.5f);
        itemRect.anchoredPosition = Vector2.zero;
        RectTransform slotRect = slot as RectTransform;
        Vector2 slotSize = isHotbar && slotRect != null
            ? slotRect.rect.size
            : new Vector2(SlotSize, SlotSize);
        if (slotSize.x <= 0f || slotSize.y <= 0f)
            slotSize = new Vector2(SlotSize, SlotSize);
        Vector2 spriteSize = sprite.rect.size;
        if (spriteSize.x <= 0f || spriteSize.y <= 0f)
            spriteSize = new Vector2(1f, 1f);
        float iconScale = Mathf.Min(slotSize.x * 0.82f / spriteSize.x, slotSize.y * 0.82f / spriteSize.y);
        itemRect.sizeDelta = spriteSize * iconScale;

        Image image = itemObject.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = true;
        Outline itemOutline = itemObject.AddComponent<Outline>();
        itemOutline.effectColor = new Color(0f, 0f, 0f, 0.95f);
        itemOutline.effectDistance = new Vector2(2f, -2f);
        CanvasGroup group = itemObject.GetComponent<CanvasGroup>();
        group.blocksRaycasts = true;

        EventTrigger itemEvents = itemObject.AddComponent<EventTrigger>();
        itemEvents.triggers = new System.Collections.Generic.List<EventTrigger.Entry>();
        AddItemEvent(itemEvents, EventTriggerType.BeginDrag,
            eventData => BeginItemDrag(itemObject, index, isHotbar, (PointerEventData)eventData));
        AddItemEvent(itemEvents, EventTriggerType.Drag,
            eventData => DragItem(itemObject, (PointerEventData)eventData));
        AddItemEvent(itemEvents, EventTriggerType.EndDrag,
            eventData => EndItemDrag(itemObject));
        AddItemEvent(itemEvents, EventTriggerType.Drop,
            eventData => HandleDrop((PointerEventData)eventData, index, isHotbar));
    }

    private static void AddItemEvent(EventTrigger trigger, EventTriggerType eventType,
        UnityEngine.Events.UnityAction<BaseEventData> callback)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = eventType };
        entry.callback.AddListener(callback);
        trigger.triggers.Add(entry);
    }

    private void BeginItemDrag(GameObject itemObject, int index, bool isHotbar, PointerEventData eventData)
    {
        hoveredItem = null;
        draggedItemObject = itemObject;
        draggedItemOriginalParent = itemObject.transform.parent;
        draggedItemOriginalPosition = itemObject.transform.localPosition;
        draggedItemIndex = index;
        draggedItemIsHotbar = isHotbar;
        draggedItemAccepted = false;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
            itemObject.transform.SetParent(canvas.transform, true);
        itemObject.transform.SetAsLastSibling();
        itemObject.GetComponent<CanvasGroup>().blocksRaycasts = false;
    }

    private static void DragItem(GameObject itemObject, PointerEventData eventData)
    {
        itemObject.transform.position = eventData.position;
    }

    private void EndItemDrag(GameObject itemObject)
    {
        if (itemObject != draggedItemObject) return;
        CanvasGroup group = itemObject.GetComponent<CanvasGroup>();
        if (draggedItemAccepted)
        {
            itemObject.SetActive(false);
            DestroyObject(itemObject);
            RefreshItemVisuals();
        }
        else
        {
            itemObject.transform.SetParent(draggedItemOriginalParent, false);
            itemObject.transform.localPosition = draggedItemOriginalPosition;
            group.blocksRaycasts = true;
        }

        draggedItemObject = null;
    }

    private void HandleDrop(PointerEventData eventData, int targetIndex, bool targetIsHotbar)
    {
        if (draggedItemObject == null || eventData.pointerDrag != draggedItemObject ||
            (!targetIsHotbar && targetIndex >= AvailableSlots))
            return;

        ItemInfo movingItem = draggedItemIsHotbar ? hotbarItems[draggedItemIndex] : inventoryItems[draggedItemIndex];
        if (movingItem == null)
            return;

        ItemInfo targetItem = targetIsHotbar ? hotbarItems[targetIndex] : inventoryItems[targetIndex];
        if (draggedItemIsHotbar)
            hotbarItems[draggedItemIndex] = targetItem;
        else
            inventoryItems[draggedItemIndex] = targetItem;

        if (targetIsHotbar)
            hotbarItems[targetIndex] = movingItem;
        else
            inventoryItems[targetIndex] = movingItem;

        draggedItemAccepted = true;
    }

    public void ShowAimedPickup(WorldPickup pickup)
    {
        aimedItem = pickup;
    }

    private void OnGUI()
    {
        if (!Application.isPlaying || MenuInputBlocked || Event.current.type != EventType.Repaint)
            return;

        string itemName;
        string description;
        if (hoveredItem != null)
        {
            itemName = hoveredItem.Name;
            description = hoveredItem.Description;
        }
        else if (!IsOpen && aimedItem != null)
        {
            itemName = $"[E] {aimedItem.DisplayName}";
            description = aimedItem.Description;
        }
        else
            return;

        if (tooltipFont == null)
            tooltipFont = Font.CreateDynamicFontFromOSFont("Arial", 16);

        const float margin = 12f;
        const float horizontalPadding = 12f;
        const float verticalPadding = 8f;
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            font = tooltipFont,
            fontSize = 19,
            fontStyle = FontStyle.Bold,
            wordWrap = true,
            normal = { textColor = new Color(1f, 0.85f, 0.35f) }
        };
        GUIContent title = new GUIContent(string.IsNullOrWhiteSpace(itemName) ? "Предмет" : itemName);
        GUIContent details = null;
        GUIStyle descriptionStyle = null;
        float preferredWidth = titleStyle.CalcSize(title).x;
        if (!string.IsNullOrWhiteSpace(description))
        {
            details = new GUIContent(description.Trim());
            descriptionStyle = new GUIStyle(GUI.skin.label)
            {
                font = tooltipFont,
                fontSize = 15,
                wordWrap = true,
                normal = { textColor = Color.white }
            };
            preferredWidth = Mathf.Max(preferredWidth, descriptionStyle.CalcSize(details).x);
        }

        float width = Mathf.Min(Mathf.Ceil(preferredWidth) + 2f * horizontalPadding,
            300f, Screen.width - 2f * margin);
        float contentWidth = width - 2f * horizontalPadding;
        float titleHeight = titleStyle.CalcHeight(title, contentWidth);
        float descriptionHeight = details != null ? descriptionStyle.CalcHeight(details, contentWidth) : 0f;
        float descriptionGap = details != null ? 4f : 0f;
        float height = titleHeight + descriptionGap + descriptionHeight + 2f * verticalPadding;
        Rect box = new Rect(Screen.width - width - margin, margin, width, height);

        Color previousColor = GUI.color;
        GUI.color = new Color(0.12f, 0.13f, 0.16f, 0.96f);
        GUI.DrawTexture(box, Texture2D.whiteTexture);
        GUI.color = previousColor;
        float x = box.x + horizontalPadding;
        float y = box.y + verticalPadding;
        GUI.Label(new Rect(x, y, contentWidth, titleHeight), title, titleStyle);
        if (details != null)
            GUI.Label(new Rect(x, y + titleHeight + descriptionGap, contentWidth, descriptionHeight),
                details, descriptionStyle);
    }

    private static void AddBackpackIcon(Transform parent, Sprite sprite)
    {
        if (sprite != null)
        {
            GameObject imageObject = new GameObject("BackpackImage", typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            RectTransform imageRect = imageObject.GetComponent<RectTransform>();
            imageRect.anchorMin = new Vector2(0.5f, 0.5f);
            imageRect.anchorMax = new Vector2(0.5f, 0.5f);
            imageRect.sizeDelta = new Vector2(62f, 62f);
            Image image = imageObject.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return;
        }

        GameObject icon = new GameObject("BackpackIcon", typeof(RectTransform));
        icon.transform.SetParent(parent, false);
        RectTransform iconRect = icon.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = new Vector2(26f, 0f);
        iconRect.sizeDelta = new Vector2(34f, 36f);

        CreateIconPart("Body", icon.transform, new Vector2(0f, -3f), new Vector2(24f, 25f),
            new Color(0.72f, 0.48f, 0.25f, 1f));
        CreateIconPart("Top", icon.transform, new Vector2(0f, 11f), new Vector2(17f, 8f),
            new Color(0.82f, 0.58f, 0.3f, 1f));
        CreateIconPart("StrapLeft", icon.transform, new Vector2(-12f, 1f), new Vector2(4f, 24f),
            new Color(0.45f, 0.28f, 0.15f, 1f));
        CreateIconPart("StrapRight", icon.transform, new Vector2(12f, 1f), new Vector2(4f, 24f),
            new Color(0.45f, 0.28f, 0.15f, 1f));
    }

    private static void AddBackpackBackground(Transform parent, Sprite sprite)
    {
        if (sprite == null)
            return;

        GameObject backgroundObject = new GameObject("BackpackBackground", typeof(RectTransform), typeof(Image));
        backgroundObject.transform.SetParent(parent, false);
        RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0.5f, 0.5f);
        backgroundRect.anchorMax = new Vector2(0.5f, 0.5f);
        backgroundRect.pivot = new Vector2(0.5f, 0.5f);
        backgroundRect.anchoredPosition = Vector2.zero;
        backgroundRect.sizeDelta = new Vector2(390f, 390f);

        Image backgroundImage = backgroundObject.GetComponent<Image>();
        backgroundImage.sprite = sprite;
        backgroundImage.preserveAspect = true;
        backgroundImage.color = new Color(1f, 1f, 1f, 0.88f);
        backgroundImage.raycastTarget = false;
    }

    private static void CreateIconPart(string name, Transform parent, Vector2 position, Vector2 size, Color color)
    {
        GameObject part = new GameObject(name, typeof(RectTransform), typeof(Image));
        part.transform.SetParent(parent, false);
        RectTransform rect = part.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Image image = part.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
    }

    private static void AddLockedMark(Transform parent)
    {
        GameObject markObject = new GameObject("LockedMark", typeof(RectTransform), typeof(Text));
        markObject.transform.SetParent(parent, false);
        RectTransform rect = markObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(42f, 42f);

        Text mark = markObject.GetComponent<Text>();
        mark.text = "X";
        mark.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        mark.fontSize = 24;
        mark.fontStyle = FontStyle.Bold;
        mark.color = new Color(0.78f, 0.81f, 0.86f, 0.9f);
        mark.alignment = TextAnchor.MiddleCenter;
        mark.raycastTarget = false;
    }

    private static void AddCloseButton(Transform parent, UnityEngine.Events.UnityAction onClick)
    {
        GameObject closeObject = CreatePanelObject("CloseButton", parent);
        RectTransform closeRect = closeObject.GetComponent<RectTransform>();
        closeRect.anchorMin = Vector2.one;
        closeRect.anchorMax = Vector2.one;
        closeRect.pivot = Vector2.one;
        closeRect.anchoredPosition = new Vector2(-8f, -8f);
        closeRect.sizeDelta = new Vector2(30f, 30f);

        Image closeImage = closeObject.GetComponent<Image>();
        closeImage.color = new Color(0.17f, 0.19f, 0.22f, 0.95f);
        Button closeButton = closeObject.AddComponent<Button>();
        closeButton.targetGraphic = closeImage;
        closeButton.onClick.AddListener(onClick);
        Outline closeOutline = closeObject.AddComponent<Outline>();
        closeOutline.effectColor = new Color(0.82f, 0.84f, 0.88f, 0.8f);
        closeOutline.effectDistance = new Vector2(1f, -1f);

        GameObject markObject = new GameObject("X", typeof(RectTransform), typeof(Text));
        markObject.transform.SetParent(closeObject.transform, false);
        RectTransform markRect = markObject.GetComponent<RectTransform>();
        markRect.anchorMin = Vector2.zero;
        markRect.anchorMax = Vector2.one;
        markRect.offsetMin = Vector2.zero;
        markRect.offsetMax = Vector2.zero;
        Text mark = markObject.GetComponent<Text>();
        mark.text = "X";
        mark.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        mark.fontSize = 20;
        mark.fontStyle = FontStyle.Bold;
        mark.color = new Color(0.92f, 0.93f, 0.96f, 1f);
        mark.alignment = TextAnchor.MiddleCenter;
        mark.raycastTarget = false;
    }

}
