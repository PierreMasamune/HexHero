using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Minimal preparing-stage UI. Lists every UnitInstance in InventoryManager,
/// and clicking a card calls PreparingController.SetSelectedInstance so the
/// player can place that unit on a green-highlighted cell.
/// 
/// Replaces the "real" inventory UI for testing until the friend's shop UI
/// handles preparing-stage placement too.
/// </summary>
public class TestPreparingUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PreparingController preparingController;
    [SerializeField] private Transform cardContainer;
    [SerializeField] private Button cardPrefab;
    [SerializeField] private GameObject emptyMessage;

    [Header("Card Colors")]
    [SerializeField] private Color normalColor = new Color(0.25f, 0.28f, 0.35f, 1f);
    [SerializeField] private Color selectedColor = new Color(1f, 0.85f, 0.15f, 1f);

    private readonly List<Button> spawnedCards = new();
    private readonly Dictionary<Button, UnitInstance> cardToInstance = new();
    private UnitInstance selectedInstance;

    // ==================================================================
    //  LIFECYCLE
    // ==================================================================

    private void OnEnable()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnUnitsChanged += Refresh;

        Refresh();
    }

    private void OnDisable()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnUnitsChanged -= Refresh;
    }

    // ==================================================================
    //  REFRESH
    // ==================================================================

    public void Refresh()
    {
        // Clear old cards
        foreach (var b in spawnedCards)
            if (b != null) Destroy(b.gameObject);
        spawnedCards.Clear();
        cardToInstance.Clear();

        if (InventoryManager.Instance == null || cardPrefab == null || cardContainer == null)
            return;

        // If the selected instance was removed (placed on board, etc.), clear local selection
        if (selectedInstance != null && !InventoryManager.Instance.Units.Contains(selectedInstance))
        {
            selectedInstance = null;
            if (preparingController != null) preparingController.CancelSelection();
        }

        // Instantiate one card per inventory unit
        foreach (var inst in InventoryManager.Instance.Units)
        {
            if (inst == null || inst.definition == null) continue;

            var card = Instantiate(cardPrefab, cardContainer);
            card.gameObject.SetActive(true);
            spawnedCards.Add(card);
            cardToInstance[card] = inst;

            // Label: name + HP
            var label = card.GetComponentInChildren<TMP_Text>();
            if (label != null)
                label.text = $"{inst.definition.unitName}\n{inst.currentHP} / {inst.maxHP} HP";

            // Tint
            var img = card.GetComponent<Image>();
            if (img != null)
                img.color = (inst == selectedInstance) ? selectedColor : normalColor;

            // Click handler — capture the instance in a local so the closure is correct
            var capturedInstance = inst;
            card.onClick.RemoveAllListeners();
            card.onClick.AddListener(() => OnCardClicked(capturedInstance));
        }

        if (emptyMessage != null)
            emptyMessage.SetActive(spawnedCards.Count == 0);
    }

    private void OnCardClicked(UnitInstance inst)
    {
        if (selectedInstance == inst)
        {
            // Click same card → deselect
            selectedInstance = null;
            preparingController?.CancelSelection();
        }
        else
        {
            // Click new card → select
            selectedInstance = inst;
            preparingController?.SetSelectedInstance(inst);
        }
        Refresh();
    }
}