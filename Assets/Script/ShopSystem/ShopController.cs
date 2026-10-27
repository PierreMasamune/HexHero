using System;
using System.Collections.Generic;
using UnityEngine;

public class ShopController : MonoBehaviour
{
    public static ShopController Instance { get; private set; }

    [Header("Shop Settings")]
    [SerializeField] private ShopCatalog catalog;
    [SerializeField] private int rerollCost = 20;

    // Stock state
    public List<UnitDefinition> CurrentTroops { get; private set; } = new();
    public List<ItemDefinition> CurrentItems { get; private set; } = new();

    // Discount state
    private bool discountActive = false;

    public event Action OnShopUpdated;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        GenerateStock();
    }

    // ==================================================================
    //  STOCK GENERATION
    // ==================================================================

    public void GenerateStock()
    {
        if (catalog == null)
        {
            Debug.LogError("[Shop] No Catalog assigned!");
            return;
        }

        CurrentTroops.Clear();
        CurrentItems.Clear();

        // Pick random troops
        for (int i = 0; i < catalog.troopSlots; i++)
        {
            if (catalog.troopPool.Count > 0)
            {
                var pick = catalog.troopPool[UnityEngine.Random.Range(0, catalog.troopPool.Count)];
                CurrentTroops.Add(pick);
            }
        }

        // Pick random items
        for (int i = 0; i < catalog.itemSlots; i++)
        {
            if (catalog.itemPool.Count > 0)
            {
                var pick = catalog.itemPool[UnityEngine.Random.Range(0, catalog.itemPool.Count)];
                CurrentItems.Add(pick);
            }
        }

        OnShopUpdated?.Invoke();
    }

    public void Reroll()
    {
        if (!EconomyManager.Instance.TrySpend(rerollCost))
        {
            Debug.Log("[Shop] Not enough money to reroll.");
            return;
        }

        GenerateStock();
        Debug.Log($"[Shop] Rerolled stock for ${rerollCost}.");
    }

    // ==================================================================
    //  BUYING
    // ==================================================================

    public void BuyItem(ItemDefinition item)
    {
        int cost = discountActive ? 0 : item.baseCost;

        if (!EconomyManager.Instance.TrySpend(cost))
        {
            Debug.Log("[Shop] Not enough money.");
            return;
        }

        ItemManager.Instance.Add(item, 1);
        ConsumeDiscountVoucherIfActive(); // If we used it, clear the flag

        CurrentItems.Remove(item);
        OnShopUpdated?.Invoke();

        Debug.Log($"[Shop] Bought {item.itemName} for ${cost}.");
    }

    public void BuyTroop(UnitDefinition troop)
    {
        int cost = discountActive ? 0 : troop.recruitCost;

        if (!EconomyManager.Instance.TrySpend(cost))
        {
            Debug.Log("[Shop] Not enough money.");
            return;
        }

        // Add to inventory. (Returns false if unit cap is reached)
        if (InventoryManager.Instance.TryAddUnit(troop, out _))
        {
            ConsumeDiscountVoucherIfActive();
            CurrentTroops.Remove(troop);
            OnShopUpdated?.Invoke();
            Debug.Log($"[Shop] Bought {troop.unitName} for ${cost}.");
        }
        else
        {
            // Refund if inventory was full
            EconomyManager.Instance.Earn(cost);
            Debug.Log("[Shop] Unit cap reached! Could not buy.");
        }
    }

    // ==================================================================
    //  DISCOUNT VOUCHER (Called by ItemEffect)
    // ==================================================================

    public void ActivateDiscountVoucher()
    {
        discountActive = true;
        OnShopUpdated?.Invoke();
    }

    public bool ConsumeDiscountVoucherIfActive()
    {
        if (!discountActive) return false;
        discountActive = false;
        OnShopUpdated?.Invoke();
        Debug.Log("[Shop] Discount Voucher consumed!");
        return true;
    }

    public bool IsDiscountActive() => discountActive;
}