using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MapNodeView nodePrefab;
    [SerializeField] private RectTransform nodeContainer;
    [SerializeField] private ScrollRect scrollRect;      // ★ NEW: Drag the Scroll View here
    [SerializeField] private RectTransform viewportRect;    // ★ NEW: Drag the Viewport here

    [Header("Connection Lines")]
    [SerializeField] private Image linePrefab;
    [SerializeField] private Color lineVisitedColor = new Color(0.4f, 0.9f, 0.4f, 0.7f);
    [SerializeField] private Color lineAvailableColor = new Color(1f, 1f, 1f, 0.85f);
    [SerializeField] private Color lineLockedColor = new Color(0.4f, 0.4f, 0.4f, 0.4f);
    [SerializeField] private float lineThickness = 4f;

    private MapData mapData;
    private RunState runState;
    private System.Action<string> onNodeClicked;
    private bool isInteractable = true;

    private readonly List<MapNodeView> views = new();
    private readonly List<Image> lines = new();

    // ★ Updated signature with bool isInteractable
    public void Refresh(MapData data, RunState state, bool interactable, System.Action<string> onNodeClick)
    {
        mapData = data;
        runState = state;
        onNodeClicked = onNodeClick;
        isInteractable = interactable;

        Clear();
        if (mapData == null) return;

        var available = ComputeAvailableNodes(state);
        var visited = new HashSet<string>(state.visitedMapNodeIds);
        string current = state.currentMapNodeId;

        foreach (var node in mapData.nodes)
        {
            var stateForNode =
                visited.Contains(node.id) ? MapNodeState.Visited :
                node.id == current ? MapNodeState.Current :
                available.Contains(node.id) ? MapNodeState.Available :
                                              MapNodeState.Locked;

            var view = Instantiate(nodePrefab, nodeContainer);
            view.GetComponent<RectTransform>().anchoredPosition = node.position;

            // Pass the interactable state to the view
            view.Bind(node, stateForNode, isInteractable, onNodeClicked);
            views.Add(view);
        }

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

        // ★ Auto-scroll to the current node
        if (!string.IsNullOrEmpty(current))
            CenterOnNode(current);
        else
            CenterOnNode(mapData.GetStartNode()?.id);
    }

    private void CenterOnNode(string nodeId)
    {
        if (scrollRect == null || viewportRect == null) return;

        var node = mapData.Get(nodeId);
        if (node == null) return;

        // Calculate normalized position (0 to 1) based on container size
        Vector2 contentSize = nodeContainer.rect.size;
        Vector2 viewSize = viewportRect.rect.size;

        float normX = (node.position.x + contentSize.x * 0.5f) / contentSize.x;
        float normY = (node.position.y + contentSize.y * 0.5f) / contentSize.y;

        // Clamp so we don't scroll past the edges
        normX = Mathf.Clamp01(normX);
        normY = Mathf.Clamp01(normY);

        scrollRect.normalizedPosition = new Vector2(normX, normY);
    }

    private void Clear()
    {
        foreach (var v in views) if (v != null) Destroy(v.gameObject);
        views.Clear();
        foreach (var l in lines) if (l != null) Destroy(l.gameObject);
        lines.Clear();
    }

    private HashSet<string> ComputeAvailableNodes(RunState state)
    {
        var result = new HashSet<string>();
        if (string.IsNullOrEmpty(state.currentMapNodeId))
        {
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
            var cur = mapData.Get(state.currentMapNodeId);
            if (cur != null)
                foreach (var c in cur.children) result.Add(c);
        }
        return result;
    }

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