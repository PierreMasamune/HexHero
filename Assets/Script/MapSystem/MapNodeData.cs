using System;
using UnityEngine;

[Serializable]
public class MapNodeData
{
    public string id;
    public MapNodeType type;
    public int chapter = 1;
    public int tier = 1;       // ★ NEW: 1, 2, or 3
    public Vector2 position;
    public string[] children = Array.Empty<string>();
    public string label;

    public MapNodeData() { }
    public MapNodeData(string id, MapNodeType type, Vector2 pos, string[] children = null)
    {
        this.id = id; this.type = type; position = pos;
        this.children = children ?? Array.Empty<string>();
    }
}