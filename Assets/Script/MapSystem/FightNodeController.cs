using System;
using UnityEngine;

public class FightNodeController : MonoBehaviour, IMapNodeController
{
    [Header("References")]
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private PreparingController preparingController;
    [SerializeField] private GameObject battleCanvasRoot;

    [Header("Data")]
    [SerializeField] private FightCatalog fightCatalog;
    [SerializeField] private HexTerrainMap terrainMap;

    public string CurrentNodeId { get; private set; }
    public event Action<IMapNodeController, MapNodeResult> OnComplete;

    public void Enter(MapNodeData node, RunState state)
    {
        CurrentNodeId = node.id;
        if (battleCanvasRoot != null) battleCanvasRoot.SetActive(true);

        // ★ NEW: Use the node's specific tier instead of the player's depth
        FightDefinition chosenFight = fightCatalog.GetRandomFight(node.tier);

        if (chosenFight == null)
        {
            Debug.LogError($"[FightNodeController] No fights found for Tier {node.tier}!");
            RaiseComplete(MapNodeResult.Success);
            return;
        }

        if (terrainMap != null)
        {
            terrainMap.ApplyTerrainLayout(chosenFight.terrain);
        }

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
        if (battleCanvasRoot != null) battleCanvasRoot.SetActive(false);
        OnComplete?.Invoke(this, result);
    }
}