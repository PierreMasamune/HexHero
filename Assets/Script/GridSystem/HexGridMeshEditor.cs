    using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(HexGridMeshGenerator))]
public class HexGridMeshEditor : Editor 
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        HexGridMeshGenerator hexGridMeshGenerator = (HexGridMeshGenerator)target;

        if (GUILayout.Button("Create Hex Mesh"))
        {
            hexGridMeshGenerator.CreateHexMesh();
        }
        if (GUILayout.Button("Clear Hex Mesh"))
        {
            hexGridMeshGenerator.ClearHexGridMesh();
        }

    }
}
