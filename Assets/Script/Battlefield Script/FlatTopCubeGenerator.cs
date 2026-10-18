using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class FlatTopCubeGenerator : MonoBehaviour
{
    [Header("Region")]
    [SerializeField] private int radius = 3;
    [SerializeField] private float cellSize = 1f;

    [Header("Gaps / Walls")]
    [Tooltip("Fraction of hex radius. Walls are 2x this thick perpendicular to edges.")]
    [Range(0f, 0.49f)]
    [SerializeField] private float gapFraction = 0.1f;
    [SerializeField] private bool generateWalls = true;
    [SerializeField] private float wallHeight = 0.5f;

    [Header("Options")]
    [SerializeField] private bool generateOnEnable = true;

    [Header("Materials (optional)")]
    [SerializeField] private Material topMaterial;
    [SerializeField] private Material wallMaterial;

    [Header("Rendering")]
    [SerializeField] private LayerMask gridLayer;

    private Mesh generatedMesh;
    private Vector3 lastPosition;

    public int Radius { get => radius; set { radius = Mathf.Max(0, value); Generate(); } }
    public float CellSize { get => cellSize; set { cellSize = Mathf.Max(0.0001f, value); Generate(); } }
    public float GapFraction { get => gapFraction; set { gapFraction = Mathf.Clamp(value, 0f, 0.49f); Generate(); } }
    public bool GenerateWalls { get => generateWalls; set { generateWalls = value; Generate(); } }
    public float WallHeight { get => wallHeight; set { wallHeight = Mathf.Max(0f, value); Generate(); } }

    private void OnEnable()
    {
        lastPosition = transform.position;
        EnsureMeshExists();
        if (generateOnEnable) Generate();
    }

    private void OnDisable() => Clear();

    private void OnValidate()
    {
        cellSize = Mathf.Max(0.0001f, cellSize);
        radius = Mathf.Max(0, radius);
        gapFraction = Mathf.Clamp(gapFraction, 0f, 0.49f);
        wallHeight = Mathf.Max(0f, wallHeight);

        if (isActiveAndEnabled)
        {
            Generate();
        }
    }

    public void Generate()
    {
        CreateHexRegionMesh(radius, cellSize, gapFraction, generateWalls, wallHeight, gridLayer);
    }

    public void Clear()
    {
        var mf = GetComponent<MeshFilter>();
        var mc = GetComponent<MeshCollider>();

        if (mf != null && mf.sharedMesh == generatedMesh) mf.sharedMesh = null;
        if (mc != null && mc.sharedMesh == generatedMesh) mc.sharedMesh = null;

        if (generatedMesh != null)
        {
            if (Application.isPlaying) Destroy(generatedMesh);
            else DestroyImmediate(generatedMesh);
            generatedMesh = null;
        }
    }
    private void Update()
    {
        // If the object moves, rebuild the mesh instantly
        if (transform.position != lastPosition)
        {
            lastPosition = transform.position;
            Generate();
        }
    }

    private void CreateHexRegionMesh(int r, float size, float gapFrac, bool makeWalls, float wallH, LayerMask layerMask)
    {
        Clear();

        var centersCube = new List<(int cx, int cy, int cz)>();
        var cellSet = new HashSet<(int cx, int cy, int cz)>();
        for (int cx = -r; cx <= r; cx++)
        {
            int minCy = Math.Max(-r, -cx - r);
            int maxCy = Math.Min(r, -cx + r);
            for (int cy = minCy; cy <= maxCy; cy++)
            {
                int cz = -cx - cy;
                centersCube.Add((cx, cy, cz));
                cellSet.Add((cx, cy, cz));
            }
        }

        if (centersCube.Count == 0) return;

        var vertices = new List<Vector3>();
        var submesh0 = new List<int>();
        var submesh1 = new List<int>();

        Vector3[] outerCorners = HexMatrix.Corners(size, HexGrid.HexOrientation.FlatTop);
        float innerFactor = Mathf.Clamp01(1f - gapFrac);
        float outerFactor = 1f + gapFrac;

        // 1. Top faces (inset hexagons)
        foreach (var c in centersCube)
        {
            // --- REMOVED + transform.position FROM HERE ---
            Vector3 center = CubeToWorldFlatTop(c.cx, c.cy, c.cz, size);

            // We add transform.position.y to the vertex Y so the mesh sits at the correct height
            center.y = transform.position.y;

            int baseIndex = vertices.Count;
            vertices.Add(center);

            for (int s = 0; s < 6; s++)
            {
                Vector3 inner = center + outerCorners[s] * innerFactor;
                inner.y = center.y;
                vertices.Add(inner);
            }

            for (int s = 0; s < 6; s++)
            {
                submesh0.Add(baseIndex);
                submesh0.Add(baseIndex + 1 + ((s + 1) % 6));
                submesh0.Add(baseIndex + 1 + s);
            }
        }

        // 2. Walls
        if (makeWalls && gapFrac > 0f)
        {
            var processedEdges = new HashSet<(long, long)>();
            const float quantize = 1000f;

            // Calculate world-space Y heights for the walls
            float baseY = transform.position.y;
            float topY = baseY + wallH;

            foreach (var c in centersCube)
            {
                // --- REMOVED + transform.position FROM HERE ---
                Vector3 center = CubeToWorldFlatTop(c.cx, c.cy, c.cz, size);
                center.y = baseY;

                for (int s = 0; s < 6; s++)
                {
                    Vector3 eA = center + outerCorners[s];
                    Vector3 eB = center + outerCorners[(s + 1) % 6];
                    eA.y = baseY;
                    eB.y = baseY;

                    // Canonical key by edge midpoint
                    Vector3 mid = (eA + eB) * 0.5f;
                    long kx = (long)Mathf.Round(mid.x * quantize);
                    long kz = (long)Mathf.Round(mid.z * quantize);
                    long key1 = kx * 1000003L + kz;
                    long key2 = kz * 1000003L + kx;
                    long key = key1 < key2 ? key1 : key2;

                    if (!processedEdges.Add((key, key))) continue;

                    // Inner face at innerFactor scale
                    Vector3 iA = center + outerCorners[s] * innerFactor;
                    Vector3 iB = center + outerCorners[(s + 1) % 6] * innerFactor;
                    // Outer face at (1 + gapFrac) scale
                    Vector3 oA = center + outerCorners[s] * outerFactor;
                    Vector3 oB = center + outerCorners[(s + 1) % 6] * outerFactor;

                    iA.y = baseY; iB.y = baseY;
                    oA.y = baseY; oB.y = baseY;

                    // --- Outer Face Quad (faces OUTWARD from cell) ---
                    int o0 = vertices.Count; vertices.Add(oA);
                    int o1 = vertices.Count; vertices.Add(oB);
                    int o2 = vertices.Count; vertices.Add(new Vector3(oB.x, topY, oB.z));
                    int o3 = vertices.Count; vertices.Add(new Vector3(oA.x, topY, oA.z));

                    submesh1.Add(o0); submesh1.Add(o2); submesh1.Add(o1);
                    submesh1.Add(o0); submesh1.Add(o3); submesh1.Add(o2);

                    // --- Inner Face Quad (faces INWARD toward cell) ---
                    int i0 = vertices.Count; vertices.Add(iA);
                    int i1 = vertices.Count; vertices.Add(iB);
                    int i2 = vertices.Count; vertices.Add(new Vector3(iB.x, topY, iB.z));
                    int i3 = vertices.Count; vertices.Add(new Vector3(iA.x, topY, iA.z));

                    submesh1.Add(i0); submesh1.Add(i1); submesh1.Add(i2);
                    submesh1.Add(i0); submesh1.Add(i2); submesh1.Add(i3);

                    // --- Top Rim Quad ---
                    int t0 = vertices.Count; vertices.Add(new Vector3(iA.x, topY, iA.z));
                    int t1 = vertices.Count; vertices.Add(new Vector3(iB.x, topY, iB.z));
                    int t2 = vertices.Count; vertices.Add(new Vector3(oB.x, topY, oB.z));
                    int t3 = vertices.Count; vertices.Add(new Vector3(oA.x, topY, oA.z));

                    submesh1.Add(t0); submesh1.Add(t1); submesh1.Add(t2);
                    submesh1.Add(t0); submesh1.Add(t2); submesh1.Add(t3);

                    // --- Bottom Rim Quad ---
                    int b0 = vertices.Count; vertices.Add(iA);
                    int b1 = vertices.Count; vertices.Add(iB);
                    int b2 = vertices.Count; vertices.Add(oB);
                    int b3 = vertices.Count; vertices.Add(oA);

                    submesh1.Add(b0); submesh1.Add(b2); submesh1.Add(b1);
                    submesh1.Add(b0); submesh1.Add(b3); submesh1.Add(b2);
                }
            }
        }

        generatedMesh = new Mesh { name = "FlatTopCubeRegion_Gapped" };
        generatedMesh.SetVertices(vertices);

        if (submesh1.Count > 0)
        {
            generatedMesh.subMeshCount = 2;
            generatedMesh.SetTriangles(submesh0, 0);
            generatedMesh.SetTriangles(submesh1, 1);
        }
        else
        {
            generatedMesh.subMeshCount = 1;
            generatedMesh.SetTriangles(submesh0, 0);
        }

        generatedMesh.RecalculateNormals();
        generatedMesh.RecalculateBounds();
        generatedMesh.hideFlags = HideFlags.DontSave;

        var mf = GetComponent<MeshFilter>();
        mf.sharedMesh = generatedMesh;

        var mc = GetComponent<MeshCollider>();
        if (mc != null) mc.sharedMesh = generatedMesh;

        var rend = GetComponent<MeshRenderer>();
        if (rend != null)
        {
            Material topMat = topMaterial;
            Material wallMat = wallMaterial;

            if (topMat == null) topMat = new Material(Shader.Find("Standard")) { name = "FlatTop_DefaultMaterial" };

            if (submesh1.Count > 0)
            {
                if (wallMat == null)
                {
                    wallMat = new Material(topMat) { name = "FlatTop_WallMaterial" };
                    if (wallMat.HasProperty("_Color")) wallMat.color = topMat.color * 0.8f;
                }
                rend.sharedMaterials = new Material[] { topMat, wallMat };
            }
            else
            {
                rend.sharedMaterial = topMat;
            }
        }

        int gridLayerIndex = GetLayerIndex(layerMask);
        gameObject.layer = gridLayerIndex;
    }

    private Vector3 CubeToWorldFlatTop(int cx, int cy, int cz, float size)
    {
        float q = cx;
        float r = cz;
        float xPos = size * 1.5f * q;
        float zPos = Mathf.Sqrt(3f) * size * (r + q * 0.5f);
        return new Vector3(xPos, 0f, zPos);
    }

    private int GetLayerIndex(LayerMask layerMask)
    {
        int layerMaskValue = layerMask.value;
        for (int i = 0; i < 32; i++)
            if (((1 << i) & layerMaskValue) != 0) return i;
        return 0;
    }

    private void EnsureMeshExists()
    {
        var mf = GetComponent<MeshFilter>();
        if (mf.sharedMesh == null)
        {
            generatedMesh = new Mesh { name = "FlatTopCubeRegion_Gapped" };
            generatedMesh.hideFlags = HideFlags.DontSave;
            mf.sharedMesh = generatedMesh;
        }
        else
        {
            generatedMesh = mf.sharedMesh;
        }
    }
}