using System;
using UnityEngine;

public class BossNodeController : MonoBehaviour, IMapNodeController
{
    [Header("References")]
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private PreparingController preparingController;
    [SerializeField] private GameObject battleCanvasRoot;

    [Header("Boss Data")]
    [Tooltip("Drag the specific handcrafted Boss FightDefinition here.")]
    [SerializeField] private FightDefinition bossFight;
    [SerializeField] private HexTerrainMap terrainMap;

    [Header("Boss Gimmick (Optional)")]
    [Tooltip("Drag a UI panel here (like a Boss Health Bar) to show during the fight.")]
    [SerializeField] private GameObject bossGimmickUI;
    [Tooltip("Drag an AudioSource here to play Boss music.")]
    [SerializeField] private AudioSource bossMusic;

    public string CurrentNodeId { get; private set; }
    public event Action<IMapNodeController, MapNodeResult> OnComplete;

    public void Enter(MapNodeData node, RunState state)
    {
        CurrentNodeId = node.id;
        if (battleCanvasRoot != null) battleCanvasRoot.SetActive(true);

        // 1. Turn on the Boss Gimmick (UI, Music, etc.)
        if (bossGimmickUI != null) bossGimmickUI.SetActive(true);
        if (bossMusic != null) bossMusic.Play();

        // 2. Apply Boss Terrain
        if (bossFight != null && terrainMap != null)
        {
            terrainMap.ApplyTerrainLayout(bossFight.terrain);
        }

        // 3. Load the specific Boss Fight
        if (preparingController != null && bossFight != null)
        {
            preparingController.OnFightComplete += HandleFightComplete;
            preparingController.LoadBattle(bossFight);
        }
        else
        {
            Debug.LogError("[BossNodeController] Missing PreparingController or Boss Fight Definition!");
            RaiseComplete(MapNodeResult.Success);
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
        // Cleanup Boss Gimmick
        if (bossGimmickUI != null) bossGimmickUI.SetActive(false);
        if (bossMusic != null) bossMusic.Stop();

        if (battleCanvasRoot != null) battleCanvasRoot.SetActive(false);

        // If the player loses to the boss, it's a run over.
        OnComplete?.Invoke(this, result);
    }
}