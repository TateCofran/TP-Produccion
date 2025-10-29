using System.Collections.Generic;
using UnityEngine;

public sealed class SpatialPartitioner : MonoBehaviour, ISpatialPartitioner
{
    [SerializeField] private float spatialCellSize = 10f;

    // celdas -> índices de tiles
    private readonly Dictionary<Vector2Int, List<int>> _spatialGrid = new();
    // índice de tile -> AABB
    private readonly Dictionary<int, (Vector3 min, Vector3 max)> _aabbs = new();

    public void AddToSpatialGrid(PlacedTile tile, int index)
    {
        // guardar AABB para consultas rápidas
        _aabbs[index] = (tile.aabbMin, tile.aabbMax);

        var minCell = WorldToSpatialCell(tile.aabbMin);
        var maxCell = WorldToSpatialCell(tile.aabbMax);

        for (int x = minCell.x; x <= maxCell.x; x++)
        {
            for (int y = minCell.y; y <= maxCell.y; y++)
            {
                var key = new Vector2Int(x, y);
                if (!_spatialGrid.TryGetValue(key, out var list))
                {
                    list = new List<int>(4);
                    _spatialGrid[key] = list;
                }
                list.Add(index);
            }
        }
    }

    public bool OverlapsAnyOptimized(Vector3 nMin, Vector3 nMax)
    {
        var minCell = WorldToSpatialCell(nMin);
        var maxCell = WorldToSpatialCell(nMax);

        // evitar chequear el mismo índice varias veces
        var checkedSet = new HashSet<int>();

        for (int x = minCell.x; x <= maxCell.x; x++)
        {
            for (int y = minCell.y; y <= maxCell.y; y++)
            {
                var key = new Vector2Int(x, y);
                if (!_spatialGrid.TryGetValue(key, out var indices)) continue;

                for (int i = 0; i < indices.Count; i++)
                {
                    int idx = indices[i];
                    if (!checkedSet.Add(idx)) continue; // ya comprobado

                    if (_aabbs.TryGetValue(idx, out var aabb))
                    {
                        if (AabbOverlap(nMin, nMax, aabb.min, aabb.max))
                            return true;
                    }
                }
            }
        }
        return false;
    }

    public void ClearSpatialGrid()
    {
        _spatialGrid.Clear();
        _aabbs.Clear();
    }

    private Vector2Int WorldToSpatialCell(Vector3 worldPos)
    {
        float s = Mathf.Max(0.0001f, spatialCellSize);
        return new Vector2Int(
            Mathf.FloorToInt(worldPos.x / s),
            Mathf.FloorToInt(worldPos.z / s)
        );
    }

    private static bool AabbOverlap(Vector3 aMin, Vector3 aMax, Vector3 bMin, Vector3 bMax)
    {
        if (aMax.x <= bMin.x || aMin.x >= bMax.x) return false;
        if (aMax.z <= bMin.z || aMin.z >= bMax.z) return false;
        return true;
    }
}
