using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class InventoryUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private Transform itemContainer;
    [SerializeField] private GameObject itemCardPrefab;

    [Header("Battle HUD to Hide")]
    [Tooltip("Drag your Battle Canvas (or the HUD panel) here so it hides while the bag is open.")]
    [SerializeField] private GameObject battleHUD;

    private bool isOpen = false;

    private void Start()
    {
        if (ItemManager.Instance != null)
            ItemManager.Instance.OnInventoryChanged += RefreshUI;

        inventoryPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (ItemManager.Instance != null)
            ItemManager.Instance.OnInventoryChanged -= RefreshUI;
    }

    private void Update()
    {
        // Press I to toggle the inventory
        if (Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame)
        {
            ToggleInventory();
        }
    }

    public void ToggleInventory()
    {
        // 1. Safety check: Don't open during Enemy Turn or Game Over
        if (TurnManager.Instance != null)
        {
            TurnState state = TurnManager.Instance.CurrentState;
            if (state == TurnState.EnemyTurn || state == TurnState.Victory || state == TurnState.Defeat)
            {
                Debug.LogWarning($"[Inventory] Cannot open inventory during {state}!");
                return;
            }
        }

        if (inventoryPanel == null)
        {
            Debug.LogError("[Inventory] The 'inventoryPanel' slot is empty in the Inspector!");
            return;
        }

        isOpen = !isOpen;
        inventoryPanel.SetActive(isOpen);

        // 2. Toggle the Battle HUD visibility
        if (battleHUD != null)
            battleHUD.SetActive(!isOpen);

        if (isOpen)
        {
            RefreshUI();

            // 3. Push the Inventory to the front so it renders above everything else
            inventoryPanel.transform.SetAsLastSibling();
        }
    }

    private void RefreshUI()
    {
        // Clear old cards
        for (int i = itemContainer.childCount - 1; i >= 0; i--)
            Destroy(itemContainer.GetChild(i).gameObject);

        if (ItemManager.Instance == null) return;

        // Spawn a card for every item you own
        foreach (var kvp in ItemManager.Instance.All)
        {
            var def = ItemManager.Instance.GetDefinition(kvp.Key);
            if (def == null) continue;

            var go = Instantiate(itemCardPrefab, itemContainer);
            var ui = go.GetComponent<InventoryItemUI>();
            ui.Setup(def, kvp.Value, this);
        }
    }

    public void OnItemClicked(ItemDefinition def)
    {
        // Close the inventory
        ToggleInventory();

        if (!def.requiresTarget)
        {
            // If it doesn't need a target (like Unit Cap+), use it instantly
            ItemManager.Instance.UseItem(def);
        }
        else
        {
            // Tell the HexSelector to wait for a battlefield click!
            HexSelector.Instance.StartTargetingItem(def);
        }
    }
}