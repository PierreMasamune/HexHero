using System;
using UnityEngine;

[Serializable]
public struct HexCoord : IEquatable<HexCoord>
{
    public int q;
    public int r;

    public static readonly HexCoord Zero = new HexCoord(0, 0);

    public HexCoord(int q, int r) { this.q = q; this.r = r; }

    public int S => -q - r;

    private static readonly HexCoord[] Directions =
    {
        new HexCoord( 1,  0), new HexCoord( 1, -1), new HexCoord( 0, -1),
        new HexCoord(-1,  0), new HexCoord(-1,  1), new HexCoord( 0,  1)
    };

    public static HexCoord Direction(int i) => Directions[((i % 6) + 6) % 6];
    public HexCoord Neighbor(int dir) => new HexCoord(q + Directions[dir].q, r + Directions[dir].r);

    public static int Distance(HexCoord a, HexCoord b)
    {
        int dq = a.q - b.q;
        int dr = a.r - b.r;
        int ds = a.S - b.S;
        return (Mathf.Abs(dq) + Mathf.Abs(dr) + Mathf.Abs(ds)) / 2;
    }

    // Uses the SAME formula as FlatTopCubeGenerator.CubeToWorldFlatTop.
    // cx = q, cz = r.
    public Vector3 ToWorld(float size, float y = 0f)
    {
        float x = size * 1.5f * q;
        float z = Mathf.Sqrt(3f) * size * (r + q * 0.5f);
        return new Vector3(x, y, z);
    }

    public static HexCoord FromWorld(Vector3 world, float size, Vector3 origin = default)
    {
        Vector3 p = world - origin;
        float qf = p.x / (1.5f * size);
        float rf = p.z / (Mathf.Sqrt(3f) * size) - qf * 0.5f;
        return CubeRound(qf, rf);
    }

    private static HexCoord CubeRound(float qf, float rf)
    {
        float sf = -qf - rf;
        int q = Mathf.RoundToInt(qf);
        int r = Mathf.RoundToInt(rf);
        int s = Mathf.RoundToInt(sf);

        float qd = Mathf.Abs(q - qf);
        float rd = Mathf.Abs(r - rf);
        float sd = Mathf.Abs(s - sf);

        if (qd > rd && qd > sd) q = -r - s;
        else if (rd > sd) r = -q - s;

        return new HexCoord(q, r);
    }

    public bool Equals(HexCoord other) => q == other.q && r == other.r;
    public override bool Equals(object obj) => obj is HexCoord h && Equals(h);
    public override int GetHashCode() => (q * 397) ^ r;
    public override string ToString() => $"({q}, {r})";
}