using UnityEngine;
using UnityEngine.Serialization;

public sealed class WorldPickup : MonoBehaviour
{
    [SerializeField] private string displayName;
    [SerializeField] private Sprite icon;
    [SerializeField, TextArea, FormerlySerializedAs("characteristics")] private string description;
    [SerializeField, TextArea, FormerlySerializedAs("bonuses")] private string effect;
    [SerializeField, TextArea] private string action;
    [SerializeField, TextArea] private string nutrition;

    private bool collected;

    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? gameObject.name : displayName;
    public Sprite Icon => icon;
    public string Description => description;
    public string Effect => effect;
    public string Action => action;
    public string Nutrition => nutrition;

    public bool TryCollect(InventoryUI inventory)
    {
        if (collected || inventory == null || icon == null || !inventory.TryAddItem(icon, DisplayName, description, effect, action, nutrition))
            return false;

        collected = true;
        if (Application.isPlaying)
            Destroy(gameObject);
        else
            DestroyImmediate(gameObject);
        return true;
    }
}
