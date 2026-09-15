using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(HexGrid))]
public class HexGridEditor : Editor
{
    private void OnSceneGUI()
    {
        HexGrid hexGrid = (HexGrid)target;

        for (int z = 0; z < hexGrid.Height; z++)
        {
            for (int x = 0; x < hexGrid.Width; x++)
            {
                Vector3 centerPosition = HexMatrix.Center(
                    hexGrid.HexSize,
                    x,
                    z,
                    hexGrid.Orientation) + hexGrid.transform.position;

                Vector3 cubeCoord = HexMatrix.OffsetToCube(x, z, hexGrid.Orientation);
                Handles.Label(centerPosition + Vector3.forward * 0.5f, $"[{x}, {z}]");
                Handles.Label(centerPosition, $"[{cubeCoord.x}, {cubeCoord.y}, {cubeCoord.z}]");
            }
        }
    }
}
