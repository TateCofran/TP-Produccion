using UnityEngine;

public sealed class SpatialOverlapAdapter : ISpatialOverlapChecker
{
    private readonly ISpatialPartitioner _spatial;
    public SpatialOverlapAdapter(ISpatialPartitioner spatial) { _spatial = spatial; }

    public void Clear() => _spatial?.ClearSpatialGrid();
    public void Add(PlacedTile tile, int tileIndex) => _spatial?.AddToSpatialGrid(tile, tileIndex);
    public bool OverlapsAny(Vector3 min, Vector3 max) => _spatial != null && _spatial.OverlapsAnyOptimized(min, max);
}

public sealed class OrientationAdapter : IOrientationService
{
    private readonly TileOrientationCalculator _calc;
    public OrientationAdapter(TileOrientationCalculator calc) { _calc = calc; }

    public OrientedData GetOrientedData(TileLayout layout, int rotSteps, bool flip)
    {
        var od = _calc.GetOrientedData(layout, rotSteps, flip);
        return new OrientedData { w = od.w, h = od.h, entryOriented = od.entryOriented };
    }

    public void OrientedSize(TileLayout layout, int rotSteps, out int w, out int h)
        => TileOrientationCalculator.OrientedSize(layout, rotSteps, out w, out h);

    public Vector2Int ApplyToCell(Vector2Int cell, TileLayout layout, int rotSteps, bool flip)
        => TileOrientationCalculator.ApplyOrientationToCell(cell, layout, rotSteps, flip);

    public Vector2Int ApplyToDir(Vector2Int dir, int rotSteps, bool flip)
        => TileOrientationCalculator.ApplyOrientationToDir(dir, rotSteps, flip);

    public Vector2Int ApplyInverseToDir(Vector2Int dir, int rotSteps, bool flip)
        => TileOrientationCalculator.ApplyInverseOrientationToDir(dir, rotSteps, flip);

    public Vector3 CellToWorldLocal(Vector2Int cell, TileLayout layout, int rotSteps, bool flip)
        => TileOrientationCalculator.CellToWorldLocal(cell, layout, rotSteps, flip);
}
