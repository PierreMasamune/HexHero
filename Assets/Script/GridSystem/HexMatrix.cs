using UnityEngine;

public class HexMatrix : MonoBehaviour
{
    public static float OuterRadius (float hexSize)
    {
        return hexSize;
    }

    public static float InnerRadius(float hexSize)
    {
        return hexSize * 0.866025404f;
    }

    public static Vector3[] Corners(float hexSize, HexGrid.HexOrientation orientation)
    {
        Vector3[] corners = new Vector3[6];
        for (int i = 0; i < 6; i++)
        {
            corners[i] = Corner(hexSize, orientation, i);
        }
        return corners;
    }

    public static Vector3 Corner(float hexSize, HexGrid.HexOrientation orientation, int cornerIndex)
    {
        float angleDeg = 60f * cornerIndex;
        if (orientation == HexGrid.HexOrientation.PointyTop)
        {
            angleDeg += 30f;
        }
        Vector3 corner = new Vector3(hexSize * Mathf.Cos(Mathf.Deg2Rad * angleDeg), 
            0f, 
            hexSize * Mathf.Sin(Mathf.Deg2Rad * angleDeg)
            );
        return corner;
    }

    public static Vector3 Center(float hexSize,int x, int z, HexGrid.HexOrientation orientation)
    {

        Vector3 centerPostition;
        if(orientation == HexGrid.HexOrientation.PointyTop)
        {
            centerPostition.x = (x + z * 0.5f - z / 2) * (InnerRadius(hexSize) * 2f);
            centerPostition.y = 0f;
            centerPostition.z = z * (OuterRadius(hexSize) * 1.5f);
        }
        else
        {
            centerPostition.x = (x) * (OuterRadius(hexSize) * 1.5f);
            centerPostition.y = 0f;
            centerPostition.z = (z + x * 0.5f - x / 2) * (InnerRadius(hexSize) * 2f);
        }
        return centerPostition;
    }

    public static Vector3 OffsetToCube(int x, int z, HexGrid.HexOrientation orientation)
    {
        int cubeX;
        int cubeZ;

        if (orientation == HexGrid.HexOrientation.PointyTop)
        {
            cubeX = x - (z - (z & 1)) / 2;
            cubeZ = z;
        }
        else
        {
            cubeX = x;
            cubeZ = z - (x - (x & 1)) / 2;
        }

        return new Vector3(cubeX, -cubeX - cubeZ, cubeZ);
    }


}
