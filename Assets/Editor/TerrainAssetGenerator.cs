#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

public static class TerrainAssetGenerator
{
    private const string Dir = "Assets/Resources/Terrains";

    [MenuItem("Tools/Hex Hero/Generate Default Terrains")]
    public static void Generate()
    {
        Directory.CreateDirectory(Dir);

        Create("Plains", TerrainType.Plains, 1, 0, 0, new Color(0.72f, 0.68f, 0.42f, 0.35f));
        Create("Forest", TerrainType.Forest, 2, 20, 0, new Color(0.15f, 0.45f, 0.15f, 0.45f));
        Create("Mountain", TerrainType.Mountain, 2, 20, 0, new Color(0.55f, 0.55f, 0.55f, 0.45f));
        Create("Water", TerrainType.Water, -1, 0, 0, new Color(0.20f, 0.45f, 0.85f, 0.55f));
        Create("Vine", TerrainType.Vine, 2, 0, 2, new Color(0.42f, 0.15f, 0.42f, 0.50f));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Terrains] Default terrain assets created in Resources/Terrains.");
    }

    private static void Create(string name, TerrainType type, int moveCost, int dmgRed, int entry, Color color)
    {
        string path = $"{Dir}/{name}.asset";
        var t = AssetDatabase.LoadAssetAtPath<TerrainDefinition>(path);
        if (t == null)
        {
            t = ScriptableObject.CreateInstance<TerrainDefinition>();
            AssetDatabase.CreateAsset(t, path);
        }
        t.terrainName = name;
        t.terrainType = type;
        t.movementCost = moveCost;
        t.damageReductionPercent = dmgRed;
        t.entryDamage = entry;
        t.color = color;
        EditorUtility.SetDirty(t);
    }
}
#endif  