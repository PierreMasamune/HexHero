using System;
using UnityEngine;

public class HexGrid : MonoBehaviour
{
    [field:SerializeField] public HexOrientation Orientation { get; private set; }
    [field: SerializeField] public int Width { get; private set; }
    [field:SerializeField] public int Height { get; private set; }
    [field:SerializeField] public float HexSize { get; private set; }
    [field: SerializeField] public GameObject HexPreFab { get; private set; }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
   public enum HexOrientation
    {
        PointyTop,
        FlatTop
    }

    private void OnDrawGizmos()
    {
      
        for (int z = 0; z < Height; z++)
        {
            for (int x = 0; x < Width; x++)
            {
                Vector3 centerPosition = HexMatrix.Center(HexSize, x, z, Orientation) + transform.position;
                for(int s =0; s<HexMatrix.Corners(HexSize, Orientation).Length; s++)
                {
                    Gizmos.DrawLine(
                        centerPosition + HexMatrix.Corners(HexSize, Orientation)[s % 6], 
                        centerPosition + HexMatrix.Corners(HexSize, Orientation)[(s+1)%6]
                        );
                }
            }
        }
    }
}

