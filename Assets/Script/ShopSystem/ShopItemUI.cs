using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopItemUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private TMP_Text priceLabel;
    [SerializeField] private TMP_Text rarityLabel;
    [SerializeField] private Button buyButton;

    // Optional: a colored border for rarity
    [SerializeField] private Image borderImage;

    private ItemDefinition itemDef;
    private UnitDefinition troopDef;
    private ShopController shop;

    public void SetupItem(ItemDefinition def, ShopController shopController)
    {
        itemDef = def;
        troopDef = null;
        shop = shopController;

        nameLabel.text = def.itemName;
        iconImage.sprite = def.icon; // Can be null for now
        iconImage.gameObject.SetActive(def.icon != null);
        priceLabel.text = $"${def.baseCost}";
        rarityLabel.text = def.rarity.ToString();

        if (borderImage != null) borderImage.color = GetRarityColor(def.rarity);

        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(() => shop.BuyItem(def));
    }

    public void SetupTroop(UnitDefinition def, ShopController shopController)
    {
        troopDef = def;
        itemDef = null;
        shop = shopController;

        nameLabel.text = def.unitName;
        iconImage.sprite = def.icon;
        iconImage.gameObject.SetActive(def.icon != null);
        priceLabel.text = $"${def.recruitCost}";
        rarityLabel.text = def.rarity.ToString();

        if (borderImage != null) borderImage.color = GetRarityColor(def.rarity);

        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(() => shop.BuyTroop(def));
    }

    public void UpdateInteractable(int currentMoney, bool discountActive)
    {
        int cost = itemDef != null ? itemDef.baseCost : troopDef.recruitCost;
        if (discountActive) cost = 0;

        buyButton.interactable = (currentMoney >= cost);
    }

    private Color GetRarityColor(Rarity r)
    {
        return r switch
        {
            Rarity.Common => Color.white,
            Rarity.Rare => new Color(0.2f, 0.6f, 1f), // Blue
            Rarity.Epic => new Color(0.7f, 0.2f, 0.9f), // Purple
            Rarity.Legendary => new Color(1f, 0.6f, 0f), // Orange
            _ => Color.gray
        };
    }
}