using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class InventoryNavigation : MonoBehaviour
{
    [Header("Slots")]
    public GameObject[] slots = new GameObject[4];

    [Header("Colors")]
    public Color selectedColor = new Color(1f, 0.78f, 0.08f, 1f);
    public Color unselectedColor = new Color(0.62f, 0.38f, 0.03f, 1f);

    private int currentSlotIndex = 0;
    private InventoryUI inventory;

    void Start()
    {
        inventory = GetComponent<InventoryUI>();
        // Проверяем слоты
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
            {
                Debug.LogError($"Слот {i + 1} не назначен! Перетащите слот в инспекторе.");
                return;
            }
        }

        UpdateSlots();
        Debug.Log("Inventory Navigation работает! Нажмите 1-4 для выбора слота.");
    }

    void Update()
    {
        // Для Unity 6 используем Keyboard.current
        if (Keyboard.current == null || (inventory != null && inventory.MenuInputBlocked)) return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame)
            SelectSlot(0);
        else if (Keyboard.current.digit2Key.wasPressedThisFrame)
            SelectSlot(1);
        else if (Keyboard.current.digit3Key.wasPressedThisFrame)
            SelectSlot(2);
        else if (Keyboard.current.digit4Key.wasPressedThisFrame)
            SelectSlot(3);
    }

    void SelectSlot(int index)
    {
        if (index < 0 || index >= slots.Length)
        {
            Debug.LogWarning($"Индекс {index} вне диапазона!");
            return;
        }

        if (slots[index] == null)
        {
            Debug.LogError($"Слот {index + 1} равен null!");
            return;
        }

        currentSlotIndex = index;
        UpdateSlots();
        Debug.Log($"Выбран слот {index + 1}");
    }

    void UpdateSlots()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) continue;

            // Меняем цвет Image компонента
            Image img = slots[i].GetComponent<Image>();
            if (img != null)
            {
                img.color = (i == currentSlotIndex) ? selectedColor : unselectedColor;
            }

            // Если есть дочерний объект "Highlight" - включаем/выключаем
            Transform highlight = slots[i].transform.Find("Highlight");
            if (highlight != null)
            {
                highlight.gameObject.SetActive(i == currentSlotIndex);
            }

            // Обновляем текст номера
            Transform numberText = slots[i].transform.Find("Number");
            if (numberText != null)
            {
                Text text = numberText.GetComponent<Text>();
                if (text != null)
                {
                    text.text = (i + 1).ToString();
                }
            }
        }
    }

    public int GetCurrentSlotIndex()
    {
        return currentSlotIndex;
    }

    public GameObject GetCurrentSlot()
    {
        return slots[currentSlotIndex];
    }
}
