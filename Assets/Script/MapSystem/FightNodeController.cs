using System;
using UnityEngine;

public class FightNodeController : MonoBehaviour, IMapNodeController
{
    [Header("References")]
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private PreparingController preparingController;

    [Tooltip("The 3D World (HexGrid, etc.)")]
    [SerializeField] private GameObject battleWorldRoot;

    [Tooltip("The Canvas with the End Turn button and Turn text.")]
    [SerializeField] private GameObject battleHUD; // ★ NEW

    [Header("Data")]
    [SerializeField] private FightCatalog fightCatalog;
    [SerializeField] private HexTerrainMap terrainMap;

    public string CurrentNodeId { get; private set; }
    public event Action<IMapNodeController, MapNodeResult> OnComplete;

    public void Enter(MapNodeData node, RunState state)
    {
        CurrentNodeId = node.id;

        // ★ Force the 3D world and Battle Canvas ON
        if (battleWorldRoot != null) battleWorldRoot.SetActive(true);
        if (battleHUD != null) battleHUD.SetActive(true);

        // 1. Pick a random battle based on the node's specific tier
        FightDefinition chosenFight = fightCatalog.GetRandomFight(node.tier);

        if (chosenFight == null)
        {
            Debug.LogError($"[FightNodeController] No fights found for Tier {node.tier}!");
            RaiseComplete(MapNodeResult.Success);
            return;
        }

        // 2. Apply the terrain for this battle
        if (terrainMap != null)
        {
            terrainMap.ApplyTerrainLayout(chosenFight.terrain);
        }

        // 3. Tell PreparingController to load this battle
        if (preparingController != null)
        {
            preparingController.OnFightComplete += HandleFightComplete;
            preparingController.LoadBattle(chosenFight);
        }
    }

    private void HandleFightComplete(MapNodeResult result)
    {
        if (preparingController != null)
            preparingController.OnFightComplete -= HandleFightComplete;

        RaiseComplete(result);
    }

    public void RaiseComplete(MapNodeResult result)
    {
        if (battleHUD != null) battleHUD.SetActive(false); // Hide HUD when leaving fight
        OnComplete?.Invoke(this, result);
    }
}