using System.Collections.Generic;
using UnityEngine;

public class MapNodeMarker : MonoBehaviour
{
    [Header("Identity")]
    public string id;
    public MapNodeType type;
    public int chapter = 1;

    // ★ NEW: Manually change the tier of the fight here!
    [Tooltip("Tier 1 = Easy, Tier 2 = Medium, Tier 3 = Hard. Determines which fight is picked.")]
    public int tier = 1;

    public string label;

    [Header("Connections")]
    public List<MapNodeMarker> children = new();

    private void OnDrawGizmos()
    {
        Gizmos.color = GizmoColor(type);
        Gizmos.DrawSphere(transform.position, 0.25f);

        if (children == null) return;
        Gizmos.color = Color.white;
        foreach (var child in children)
        {
            if (child == null) continue;
            Gizmos.DrawLine(transform.position, child.transform.position);
        }

#if UNITY_EDITOR
        string displayTier = type == MapNodeType.Fight ? $" (T{tier})" : "";
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.4f,
            string.IsNullOrEmpty(label) ? $"{type}{displayTier}\n({id})" : $"{label}\n({id})");
#endif
    }

    private static Color GizmoColor(MapNodeType t) => t switch
    {
        MapNodeType.Start => Color.gray,
        MapNodeType.Fight => new Color(0.9f, 0.4f, 0.4f),
        MapNodeType.Shop => new Color(0.9f, 0.8f, 0.3f),
        MapNodeType.Buff => new Color(0.4f, 0.7f, 1.0f),
        MapNodeType.Boss => new Color(0.7f, 0.2f, 0.7f),
        _ => Color.white
    };

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(id)) id = $"{type}_{GetInstanceID()}";
    }
}