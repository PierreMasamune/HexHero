using UnityEngine;
using UnityEngine.SceneManagement;

public class RunManager : MonoBehaviour
{
    public static RunManager Instance { get; private set; }

    [Header("Map Data")]
    [SerializeField] private MapData mapData;

    [Header("Screen Roots")]
    [SerializeField] private GameObject mapRoot;
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
    private string pendingNodeId;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

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

    [ContextMenu("Reset Run (Delete Save & Reset State)")]
    public void ResetRunFromEditor()
    {
        SaveSystem.DeleteSave();
        State = new RunState();
        Debug.Log("[RunManager] Run has been reset.");
        if (Application.isPlaying) SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

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

        // Only show battle root for Fight or Boss nodes
        bool isBattleNode = (node.type == MapNodeType.Fight || node.type == MapNodeType.Boss);
        if (battleRoot != null) battleRoot.SetActive(isBattleNode);

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

        // Increment depth for progression tracking
        State.runDepth++;

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

    private void HandleRunOver()
    {
        State.isRunOver = true;
        SaveSystem.Save(State);

        if (mapRoot != null) mapRoot.SetActive(false);
        if (battleRoot != null) battleRoot.SetActive(false);
        if (runOverRoot != null) runOverRoot.SetActive(true);
    }

    public void CompleteActiveNode(MapNodeResult result)
    {
        if (activeController == null) return;
        activeController.RaiseComplete(result);
    }
}