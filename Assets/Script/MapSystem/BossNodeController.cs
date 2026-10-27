using System;
using UnityEngine;

public class BossNodeController : MonoBehaviour, IMapNodeController
{
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private PreparingController preparingController;
    [SerializeField] private GameObject battleCanvasRoot;
    [Tooltip("Optional — a boss-specific EnemyDefinition to spawn instead of the regular roster.")]
    [SerializeField] private EnemyDefinition bossEnemy;
    [SerializeField] private HexCoord bossSpawnCell = new HexCoord(4, 0);

    public string CurrentNodeId { get; private set; }
    public event Action<IMapNodeController, MapNodeResult> OnComplete;

    public void Enter(MapNodeData node, RunState state)
    {
        CurrentNodeId = node.id;
        if (battleCanvasRoot != null) battleCanvasRoot.SetActive(true);

        // The boss enemy spawn is done via PreparingController.enemySetup — you can
        // pre-configure the boss in that list, or add a public method to PreparingController
        // to inject a boss enemy. For now, this stub assumes enemySetup was authored
        // with the boss in it.

        if (turnManager != null) turnManager.OnStateChanged += HandleTurnState;
        if (preparingController != null) preparingController.EnterPreparing();
    }

    private void HandleTurnState(TurnState newState)
    {
        if (newState == TurnState.Victory) { Unsubscribe(); RaiseComplete(MapNodeResult.Success); }
        else if (newState == TurnState.Defeat) { Unsubscribe(); RaiseComplete(MapNodeResult.Fail); }
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