using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MapNodeView nodePrefab;
    [SerializeField] private RectTransform nodeContainer;   // where nodes instantiate

    [Header("Connection Lines")]
    [SerializeField] private Image linePrefab;            // simple 1×1 image
    [SerializeField] private Color lineVisitedColor = new Color(0.4f, 0.9f, 0.4f, 0.7f);
    [SerializeField] private Color lineAvailableColor = new Color(1f, 1f, 1f, 0.85f);
    [SerializeField] private Color lineLockedColor = new Color(0.4f, 0.4f, 0.4f, 0.4f);
    [SerializeField] private float lineThickness = 4f;

    private MapData mapData;
    private RunState runState;
    private System.Action<string> onNodeClicked;

    private readonly List<MapNodeView> views = new();
    private readonly List<Image> lines = new();

    // ==================================================================
    //  PUBLIC
    // ==================================================================

    public void Refresh(MapData data, RunState state, System.Action<string> onNodeClick)
    {
        mapData = data;
        runState = state;
        onNodeClicked = onNodeClick;

        Clear();

        if (mapData == null) return;

        // Determine available node IDs
        var available = ComputeAvailableNodes(state);
        var visited = new HashSet<string>(state.visitedMapNodeIds);
        string current = state.currentMapNodeId;

        // Instantiate node views
        foreach (var node in mapData.nodes)
        {
            var stateForNode =
                visited.Contains(node.id) ? MapNodeState.Visited :
                node.id == current ? MapNodeState.Current :
                available.Contains(node.id) ? MapNodeState.Available :
                                              MapNodeState.Locked;

            var view = Instantiate(nodePrefab, nodeContainer);
            view.GetComponent<RectTransform>().anchoredPosition = node.position;
            view.Bind(node, stateForNode, onNodeClicked);
            views.Add(view);
        }

        // Draw connections
        foreach (var node in mapData.nodes)
        {
            foreach (var childId in node.children)
            {
                var child = mapData.Get(childId);
                if (child == null) continue;

                bool parentVisited = visited.Contains(node.id);
                bool childAvailable = available.Contains(child.id);
                bool parentAvailable = available.Contains(node.id);

                Color c =
                    parentVisited ? lineVisitedColor :
                    parentAvailable ? lineAvailableColor :
                                      lineLockedColor;

                DrawLine(node.position, child.position, c);
            }
        }
    }

    private void Clear()
    {
        foreach (var v in views) if (v != null) Destroy(v.gameObject);
        views.Clear();
        foreach (var l in lines) if (l != null) Destroy(l.gameObject);
        lines.Clear();
    }

    // ==================================================================
    //  LOGIC
    // ==================================================================

    /// <summary>Which nodes can the player click right now?</summary>
    private HashSet<string> ComputeAvailableNodes(RunState state)
    {
        var result = new HashSet<string>();

        if (string.IsNullOrEmpty(state.currentMapNodeId))
        {
            // Start of the run: expose root nodes or the children of the Start node.
            var start = mapData.GetStartNode();
            if (start != null)
            {
                foreach (var c in start.children) result.Add(c);
            }
            else
            {
                foreach (var root in mapData.GetRootNodes()) result.Add(root.id);
            }
        }
        else
        {
            // Children of the current node become available
            var cur = mapData.Get(state.currentMapNodeId);
            if (cur != null)
                foreach (var c in cur.children) result.Add(c);
        }

        return result;
    }

    // ==================================================================
    //  LINE DRAWING
    // ==================================================================

    private void DrawLine(Vector2 a, Vector2 b, Color color)
    {
        if (linePrefab == null) return;

        Vector2 delta = b - a;
        float len = delta.magnitude;
        if (len < 0.01f) return;

        var img = Instantiate(linePrefab, nodeContainer);
        var rt = img.GetComponent<RectTransform>();

        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = a + delta * 0.5f;
        rt.sizeDelta = new Vector2(len, lineThickness);

        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        rt.localRotation = Quaternion.Euler(0f, 0f, angle);

        img.color = color;
        lines.Add(img);
    }
}