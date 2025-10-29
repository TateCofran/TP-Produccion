using UnityEngine;

public struct PlacedTile
{
    public TileLayout layout;
    public Vector3 worldOrigin;
    public int rotSteps;
    public bool flipped;
    public Vector3 aabbMin;
    public Vector3 aabbMax;
    public Transform parent;
}

public delegate PlacedTile PlacedTileGetter(int index);

public struct ExitRecord
{
    public int tileIndex;
    public Vector2Int cell;
    public int flags;
    public string label;
    public bool Used => (flags & 1) != 0;
    public bool Closed => (flags & 2) != 0;
    public void SetUsed(bool v) { if (v) flags |= 1; else flags &= ~1; }
    public void SetClosed(bool v) { if (v) flags |= 2; else flags &= ~2; }
}

public struct OrientedData
{
    public int w, h;
    public Vector2Int entryOriented;
}
