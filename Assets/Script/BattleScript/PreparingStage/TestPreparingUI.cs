using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TestPreparingUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PreparingController preparingController;
    [SerializeField] private Transform cardContainer;
    [SerializeField] private Button cardPrefab;
    [SerializeField] private GameObject emptyMessage;

    [Header("Button State (OPTIONAL — assign only if you want auto-disable)")]
    [Tooltip("If assigned, becomes interactable only when ≥1 unit is placed.")]
    [SerializeField] private Button startBattleButton;
    [Tooltip("If assigned, text updates to 'Place units first' when no units placed.")]
    [SerializeField] private TMP_Text startBattleLabel;

    [Header("Card Colors")]
    [SerializeField] private Color normalColor = new Color(0.25f, 0.28f, 0.35f, 1f);
    [SerializeField] private Color selectedColor = new Color(1f, 0.85f, 0.15f, 1f);

    private readonly List<Button> spawnedCards = new();
    private UnitInstance selectedInstance;

    private void OnEnable()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnUnitsChanged += Refresh;
        if (preparingController != null)
            preparingController.OnCanStartBattleChanged += HandleCanStartChanged;

        Refresh();

        // Initial button state
        if (preparingController != null)
            HandleCanStartChanged(preparingController.CanStartBattle);
    }

    private void OnDisable()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnUnitsChanged -= Refresh;
        if (preparingController != null)
            preparingController.OnCanStartBattleChanged -= HandleCanStartChanged;
    }

    public void Refresh()
    {
        foreach (var b in spawnedCards)
            if (b != null) Destroy(b.gameObject);
        spawnedCards.Clear();

        if (InventoryManager.Instance == null || cardPrefab == null || cardContainer == null)
            return;

        if (selectedInstance != null && !InventoryManager.Instance.Units.Contains(selectedInstance))
        {
            selectedInstance = null;
            preparingController?.CancelSelection();
        }

        foreach (var inst in InventoryManager.Instance.Units)
        {
            if (inst == null || inst.definition == null) continue;

            var card = Instantiate(cardPrefab, cardContainer);
            card.gameObject.SetActive(true);
            spawnedCards.Add(card);

            var label = card.GetComponentInChildren<TMP_Text>();
            if (label != null)
                label.text = $"{inst.definition.unitName}\n{inst.currentHP}/{inst.maxHP}";

            var img = card.GetComponent<Image>();
            if (img != null)
                img.color = (inst == selectedInstance) ? selectedColor : normalColor;

            var captured = inst;
            card.onClick.RemoveAllListeners();
            card.onClick.AddListener(() => OnCardClicked(captured));
        }

        if (emptyMessage != null)
            emptyMessage.SetActive(spawnedCards.Count == 0);
    }

    private void OnCardClicked(UnitInstance inst)
    {
        if (selectedInstance == inst)
        {
            selectedInstance = null;
            preparingController?.CancelSelection();
        }
        else
        {
            selectedInstance = inst;
            preparingController?.SetSelectedInstance(inst);
        }
        Refresh();
    }

    private void HandleCanStartChanged(bool canStart)
    {
        if (startBattleButton != null) startBattleButton.interactable = canStart;
        if (startBattleLabel != null)
            startBattleLabel.text = canStart ? "Start Battle" : "Place units first";
    }
}