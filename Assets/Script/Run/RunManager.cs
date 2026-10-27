using UnityEngine;

public class RunManager : MonoBehaviour
{
    public static RunManager Instance { get; private set; }

    [Header("Map Data")]
    [SerializeField] private MapData mapData;

    [Header("Screen Roots")]
    [Tooltip("Everything that belongs to the map screen.")]
    [SerializeField] private GameObject mapRoot;
    [Tooltip("Everything that belongs to the battle scene (HexGridManager, TerrainRenderer, PreparingPanel, HUD).")]
    [SerializeField] private GameObject battleRoot;

    [Header("Map UI")]
    [SerializeField] private MapController mapController;

    [Header("Node Controllers")]
    [SerializeField] private FightNodeController fightController;
    [SerializeField] private ShopNodeController shopController;
    [SerializeField] private BuffNodeController buffController;
    [SerializeField] private BossNodeController bossController;

    [Header("Run Over")]
    [SerializeField] private GameObject runOverRoot;

    public RunState State { get; private set; }

    private IMapNodeController activeController;
    private string pendingNodeId;   // node we're currently inside

    // ==================================================================
    //  LIFECYCLE
    // ==================================================================

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Load or create RunState
        if (SaveSystem.TryLoad(out RunState loaded))
        {
            State = loaded;
            Debug.Log("[RunManager] Loaded existing run.");
        }
        else
        {
            State = new RunState();
            Debug.Log("[RunManager] New run created.");
        }
    }

    private void Start()
    {
        if (runOverRoot != null) runOverRoot.SetActive(false);
        ShowMap();
    }

    // ==================================================================
    //  MAP
    // ==================================================================

    public void ShowMap()
    {
        HideAllNodeControllers();

        if (mapRoot != null) mapRoot.SetActive(true);
        if (battleRoot != null) battleRoot.SetActive(false);
        if (runOverRoot != null) runOverRoot.SetActive(false);

        if (mapController != null)
            mapController.Refresh(mapData, State, OnNodeClicked);
    }

    private void OnNodeClicked(string nodeId)
    {
        var node = mapData.Get(nodeId);
        if (node == null) return;

        pendingNodeId = nodeId;

        if (mapRoot != null) mapRoot.SetActive(false);
        if (battleRoot != null) battleRoot.SetActive(true);

        activeController = GetControllerFor(node.type);
        if (activeController == null)
        {
            Debug.LogWarning($"[RunManager] No controller for node type {node.type}. Skipping.");
            CompleteNode(nodeId, MapNodeResult.Success);
            return;
        }

        activeController.OnComplete += HandleNodeComplete;
        activeController.Enter(node, State);
    }

    private IMapNodeController GetControllerFor(MapNodeType type) => type switch
    {
        MapNodeType.Fight => fightController,
        MapNodeType.Shop => shopController,
        MapNodeType.Buff => buffController,
        MapNodeType.Boss => bossController,
        _ => null
    };

    private void HandleNodeComplete(IMapNodeController controller, MapNodeResult result)
    {
        controller.OnComplete -= HandleNodeComplete;
        activeController = null;

        if (result == MapNodeResult.Fail)
        {
            HandleRunOver();
            return;
        }

        CompleteNode(pendingNodeId, result);
    }

    private void CompleteNode(string nodeId, MapNodeResult result)
    {
        if (!string.IsNullOrEmpty(nodeId) && !State.visitedMapNodeIds.Contains(nodeId))
            State.visitedMapNodeIds.Add(nodeId);

        State.currentMapNodeId = nodeId;
        pendingNodeId = null;

        SaveSystem.Save(State);
        ShowMap();
    }

    private void HideAllNodeControllers()
    {
        if (activeController != null)
        {
            activeController.OnComplete -= HandleNodeComplete;
            activeController = null;
        }
    }

    // ==================================================================
    //  RUN OVER
    // ==================================================================

    private void HandleRunOver()
    {
        State.isRunOver = true;
        SaveSystem.Save(State);

        if (mapRoot != null) mapRoot.SetActive(false);
        if (battleRoot != null) battleRoot.SetActive(false);
        if (runOverRoot != null) runOverRoot.SetActive(true);
    }

    // ==================================================================
    //  PUBLIC HELPERS (for node controllers)
    // ==================================================================

    public void CompleteActiveNode(MapNodeResult result)
    {
        if (activeController == null) return;
        activeController.RaiseComplete(result);
    }
}