using System.Collections.Generic;
using UnityEngine;

public struct TileLink
{
    public int tileA;
    public Vector2Int cellA;
    public int tileB;
    public Vector2Int cellB;
}

public sealed class TileLinkRepository
{
    private readonly List<TileLink> _links = new List<TileLink>(64);

    public int Count => _links.Count;
    public TileLink Get(int index) => _links[index];
    public void Clear() => _links.Clear();

    public void Add(int tileA, Vector2Int cellA, int tileB, Vector2Int cellB)
    {
        for (int i = 0; i < _links.Count; i++)
        {
            var l = _links[i];
            bool same = (l.tileA == tileA && l.cellA == cellA && l.tileB == tileB && l.cellB == cellB) ||
                        (l.tileA == tileB && l.cellA == cellB && l.tileB == tileA && l.cellB == cellA);
            if (same) return;
        }

        _links.Add(new TileLink { tileA = tileA, cellA = cellA, tileB = tileB, cellB = cellB });
    }
}
