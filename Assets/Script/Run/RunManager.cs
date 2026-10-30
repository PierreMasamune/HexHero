using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class RunManager : MonoBehaviour
{
    public static RunManager Instance { get; private set; }

    [Header("Map Data")]
    [SerializeField] private MapData mapData;

    [Header("Screen Roots")]
    [SerializeField] private GameObject mapRoot;
    [SerializeField] private GameObject battleRoot;
    [SerializeField] private GameObject battleHUD;

    [Header("Map UI")]
    [SerializeField] private MapController mapController;
    [SerializeField] private Transform mapTextSpawnParent;

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
        }
        else
        {
            State = new RunState();
        }
    }

    private void Start()
    {
        if (runOverRoot != null) runOverRoot.SetActive(false);
        ShowMap();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame)
        {
            ToggleMapOverlay();
        }
    }

    public void ShowMap()
    {
        HideAllNodeControllers();

        if (mapRoot != null) mapRoot.SetActive(true);
        if (runOverRoot != null) runOverRoot.SetActive(false);

        if (battleRoot != null) battleRoot.SetActive(true);
        if (battleHUD != null) battleHUD.SetActive(false);

        if (mapController != null)
            mapController.Refresh(mapData, State, true, OnNodeClicked);
    }

    public void ToggleMapOverlay()
    {
        bool isMapOpen = mapRoot.activeSelf;
        mapRoot.SetActive(!isMapOpen);

        if (battleHUD != null) battleHUD.SetActive(isMapOpen);

        if (!isMapOpen)
        {
            if (mapController != null)
                mapController.Refresh(mapData, State, false, OnNodeClicked);
        }
    }

    private void OnNodeClicked(string nodeId)
    {
        var node = mapData.Get(nodeId);
        if (node == null) return;

        pendingNodeId = nodeId;

        if (mapRoot != null) mapRoot.SetActive(false);
        if (battleHUD != null) battleHUD.SetActive(true);

        activeController = GetControllerFor(node.type);
        if (activeController == null) return;

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
        State.runDepth++;

        // ★ DEDUCT UPKEEP HERE (After battle cleanup, when units are back in inventory)
        int upkeep = InventoryManager.Instance.TotalUpkeep();
        if (upkeep > 0 && EconomyManager.Instance != null)
        {
            EconomyManager.Instance.TrySpend(upkeep);

            if (mapTextSpawnParent != null)
            {
                FloatingMapText.Spawn(mapTextSpawnParent, Vector2.zero, $"-${upkeep}", Color.red);
            }
        }

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
        if (battleHUD != null) battleHUD.SetActive(false);
        if (runOverRoot != null) runOverRoot.SetActive(true);
        if (battleRoot != null) battleRoot.SetActive(true);
    }

    public void CompleteActiveNode(MapNodeResult result)
    {
        if (activeController == null) return;
        activeController.RaiseComplete(result);
    }

    [ContextMenu("Reset Run (Delete Save & Reset State)")]
    public void ResetRunFromEditor()
    {
        SaveSystem.DeleteSave();
        State = new RunState();
        if (Application.isPlaying) SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}