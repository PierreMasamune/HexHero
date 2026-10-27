using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryItemUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private TMP_Text countLabel;
    [SerializeField] private Button useButton;

    private string itemId;
    private InventoryUI inventoryUI;

    public void Setup(ItemDefinition def, int count, InventoryUI ui)
    {
        itemId = def.itemId;
        inventoryUI = ui;

        if (nameLabel != null) nameLabel.text = def.itemName;
        if (countLabel != null) countLabel.text = $"x{count}";

        if (iconImage != null)
        {
            iconImage.sprite = def.icon;
            iconImage.gameObject.SetActive(def.icon != null);
        }

        if (useButton != null)
        {
            useButton.onClick.RemoveAllListeners();
            useButton.onClick.AddListener(() => inventoryUI.OnItemClicked(def));
        }
    }
}