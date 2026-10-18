using System.Collections.Generic;
using TMPro;
using UnityEngine;

[ExecuteAlways]
public class HexCoordLabeler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HexGridManager grid;

    [Header("Label Settings")]
    [SerializeField] private float labelHeight = 0.2f;
    [SerializeField] private float fontSize = 4f;
    [SerializeField] private Color textColor = Color.black;
    [SerializeField] private Vector3 labelRotation = new Vector3(90f, 0f, 0f);

    private readonly List<GameObject> spawnedLabels = new List<GameObject>();

    private void OnEnable()
    {
        if (grid == null) grid = FindAnyObjectByType<HexGridManager>();
        Rebuild();
    }

    private void OnDisable()
    {
        Clear();
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this == null || !isActiveAndEnabled) return;
            Rebuild();
        };
#else
        Rebuild();
#endif
    }

    [ContextMenu("Rebuild Labels")]
    public void Rebuild()
    {
        Clear();
        if (grid == null) return;

        foreach (var cell in grid.AllCells())
        {
            Vector3 worldPos = grid.CellToWorld(cell);

            var go = new GameObject($"Label_{cell.q}_{cell.r}");
            go.transform.SetParent(transform);
            go.transform.position = worldPos + Vector3.up * labelHeight;
            go.transform.rotation = Quaternion.Euler(labelRotation);

            var tmp = go.AddComponent<TextMeshPro>();
            tmp.text = $"{cell.q}, {cell.r}";
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = textColor;
            tmp.rectTransform.sizeDelta = new Vector2(1f, 1f);

            // THIS IS THE FIX: Hides the object from the Hierarchy and Scene View selection
            go.hideFlags = HideFlags.HideInHierarchy;

            spawnedLabels.Add(go);
        }
    }

    public void Clear()
    {
        for (int i = spawnedLabels.Count - 1; i >= 0; i--)
        {
            var go = spawnedLabels[i];
            if (go == null) continue;

            if (Application.isPlaying) Destroy(go);
            else DestroyImmediate(go);
        }
        spawnedLabels.Clear();
    }
}