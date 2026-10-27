using System;
using UnityEngine;

public class FightNodeController : MonoBehaviour, IMapNodeController
{
    [Header("References")]
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private PreparingController preparingController;
    [SerializeField] private GameObject battleCanvasRoot;

    public string CurrentNodeId { get; private set; }
    public event Action<IMapNodeController, MapNodeResult> OnComplete;

    private MapNodeData node;

    public void Enter(MapNodeData nodeData, RunState state)
    {
        node = nodeData;
        CurrentNodeId = node.id;

        if (battleCanvasRoot != null) battleCanvasRoot.SetActive(true);

        // TurnManager begins in Preparing state — PreparingController drives from there
        if (turnManager != null) turnManager.OnStateChanged += HandleTurnState;
        if (preparingController != null) preparingController.EnterPreparing();
        else Debug.LogWarning("[FightNodeController] No PreparingController — battle cannot start.");
    }

    private void HandleTurnState(TurnState newState)
    {
        if (newState == TurnState.Victory)
        {
            Unsubscribe();
            RaiseComplete(MapNodeResult.Success);
        }
        else if (newState == TurnState.Defeat)
        {
            Unsubscribe();
            RaiseComplete(MapNodeResult.Fail);
        }
    }

    private void Unsubscribe()
    {
        if (turnManager != null) turnManager.OnStateChanged -= HandleTurnState;
    }

    public void RaiseComplete(MapNodeResult result)
    {
        if (battleCanvasRoot != null) battleCanvasRoot.SetActive(false);
        OnComplete?.Invoke(this, result);
    }
}