using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class ShopUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private TMP_Text moneyLabel;
    [SerializeField] private TMP_Text titleLabel;

    [Header("Containers")]
    [SerializeField] private Transform troopContainer;
    [SerializeField] private Transform itemContainer;
    [SerializeField] private GameObject shopItemPrefab;

    [Header("Buttons")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button rerollButton;

    [Header("Battle HUD to Hide")]
    [Tooltip("Drag your Battle Canvas (or the HUD panel) here so it hides while shopping.")]
    [SerializeField] private GameObject battleHUD;

    private ShopController shop;
    private bool isOpen = false;

    private void Start()
    {
        shop = ShopController.Instance;
        if (shop == null) { Debug.LogError("No ShopController found!"); return; }

        shop.OnShopUpdated += RefreshUI;
        if (EconomyManager.Instance != null)
            EconomyManager.Instance.OnMoneyChanged += HandleMoneyChanged;

        closeButton.onClick.AddListener(ToggleShop);
        rerollButton.onClick.AddListener(() => shop.Reroll());

        shopPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (shop != null) shop.OnShopUpdated -= RefreshUI;
        if (EconomyManager.Instance != null) EconomyManager.Instance.OnMoneyChanged -= HandleMoneyChanged;
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.f2Key.wasPressedThisFrame)
            ToggleShop();
    }

    public void ToggleShop()
    {
        isOpen = !isOpen;
        shopPanel.SetActive(isOpen);

        // Hide or show the battle HUD
        if (battleHUD != null)
            battleHUD.SetActive(!isOpen);

        if (isOpen)
        {
            titleLabel.text = "Merchant";
            RefreshUI();

            // Push the Shop to the front so it renders above everything else
            shopPanel.transform.SetAsLastSibling();
        }
    }

    private void HandleMoneyChanged(int newBalance)
    {
        if (moneyLabel != null) moneyLabel.text = $"${newBalance}";
        if (isOpen) RefreshUI();
    }

    private void RefreshUI()
    {
        if (shop == null || EconomyManager.Instance == null) return;

        moneyLabel.text = $"${EconomyManager.Instance.Money}";

        for (int i = troopContainer.childCount - 1; i >= 0; i--)
            Destroy(troopContainer.GetChild(i).gameObject);
        for (int i = itemContainer.childCount - 1; i >= 0; i--)
            Destroy(itemContainer.GetChild(i).gameObject);

        foreach (var troop in shop.CurrentTroops)
        {
            if (troop == null) continue;
            var go = Instantiate(shopItemPrefab, troopContainer);
            var ui = go.GetComponent<ShopItemUI>();
            if (ui != null)
            {
                ui.SetupTroop(troop, shop);
                ui.UpdateInteractable(EconomyManager.Instance.Money, shop.IsDiscountActive());
            }
        }

        foreach (var item in shop.CurrentItems)
        {
            if (item == null) continue;
            var go = Instantiate(shopItemPrefab, itemContainer);
            var ui = go.GetComponent<ShopItemUI>();
            if (ui != null)
            {
                ui.SetupItem(item, shop);
                ui.UpdateInteractable(EconomyManager.Instance.Money, shop.IsDiscountActive());
            }
        }
    }
}