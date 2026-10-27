using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ShopUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private TMP_Text moneyLabel;
    [SerializeField] private TMP_Text titleLabel;

    [Header("Containers")]
    [SerializeField] private Transform troopContainer;
    [SerializeField] private Transform itemContainer;
    [SerializeField] private GameObject shopItemPrefab; // The prefab with ShopItemUI

    [Header("Buttons")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button rerollButton;

    private ShopController shop;
    private bool isOpen = false;

    private void Start()
    {
        shop = ShopController.Instance;
        if (shop == null) { Debug.LogError("No ShopController found!"); return; }

        // Hook up events
        shop.OnShopUpdated += RefreshUI;
        EconomyManager.Instance.OnMoneyChanged += HandleMoneyChanged;

        closeButton.onClick.AddListener(ToggleShop);
        rerollButton.onClick.AddListener(() => shop.Reroll());

        // Start hidden
        shopPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (shop != null) shop.OnShopUpdated -= RefreshUI;
        if (EconomyManager.Instance != null) EconomyManager.Instance.OnMoneyChanged -= HandleMoneyChanged;
    }

    private void Update()
    {
        // Press F2 to open the shop for testing
        if (Keyboard.current != null && Keyboard.current.f2Key.wasPressedThisFrame)
            ToggleShop();
    }

    public void ToggleShop()
    {
        isOpen = !isOpen;
        shopPanel.SetActive(isOpen);

        if (isOpen)
        {
            titleLabel.text = "Merchant";
            RefreshUI();
        }
    }

    private void HandleMoneyChanged(int newBalance)
    {
        moneyLabel.text = $"${newBalance}";
        if (isOpen) RefreshUI(); // Update button interactability
    }

    private void RefreshUI()
    {
        moneyLabel.text = $"${EconomyManager.Instance.Money}";

        // Clear old cards
        foreach (Transform child in troopContainer) Destroy(child.gameObject);
        foreach (Transform child in itemContainer) Destroy(child.gameObject);

        // Spawn new cards
        foreach (var troop in shop.CurrentTroops)
        {
            var go = Instantiate(shopItemPrefab, troopContainer);
            var ui = go.GetComponent<ShopItemUI>();
            ui.SetupTroop(troop, shop);
            ui.UpdateInteractable(EconomyManager.Instance.Money, shop.IsDiscountActive());
        }

        foreach (var item in shop.CurrentItems)
        {
            var go = Instantiate(shopItemPrefab, itemContainer);
            var ui = go.GetComponent<ShopItemUI>();
            ui.SetupItem(item, shop);
            ui.UpdateInteractable(EconomyManager.Instance.Money, shop.IsDiscountActive());
        }
    }
}