using System;
using UnityEngine;

[Serializable]
public class MapNodeData
{
    public string id;
    public MapNodeType type;
    public int chapter = 1;
    [Tooltip("Position in map-UI space (pixels).")]
    public Vector2 position;
    [Tooltip("IDs of the nodes this node leads to.")]
    public string[] children = Array.Empty<string>();
    public string label;   // optional display override

    public MapNodeData() { }
    public MapNodeData(string id, MapNodeType type, Vector2 pos, string[] children = null)
    {
        this.id = id; this.type = type; position = pos;
        this.children = children ?? Array.Empty<string>();
    }
}