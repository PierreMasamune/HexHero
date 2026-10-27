using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Map_Chapter1", menuName = "HexHero/Map Data")]
public class MapData : ScriptableObject
{
    public List<MapNodeData> nodes = new();

    public MapNodeData Get(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        foreach (var n in nodes) if (n.id == id) return n;
        return null;
    }

    public IEnumerable<MapNodeData> GetChildren(string id)
    {
        var node = Get(id);
        if (node == null) yield break;
        foreach (var childId in node.children)
        {
            var c = Get(childId);
            if (c != null) yield return c;
        }
    }

    public MapNodeData GetStartNode()
    {
        foreach (var n in nodes) if (n.type == MapNodeType.Start) return n;
        return null;
    }

    /// <summary>All nodes with no parent — used if the map has no Start node.</summary>
    public List<MapNodeData> GetRootNodes()
    {
        var hasParent = new HashSet<string>();
        foreach (var n in nodes)
            foreach (var c in n.children)
                hasParent.Add(c);

        var roots = new List<MapNodeData>();
        foreach (var n in nodes)
            if (!hasParent.Contains(n.id)) roots.Add(n);
        return roots;
    }
}