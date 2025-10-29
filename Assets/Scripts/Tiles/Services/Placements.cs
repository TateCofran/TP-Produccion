using UnityEngine;

public sealed class PlacementCalculator : IPlacementCalculator
{
    private readonly float _tileGap;
    private readonly Vector3 _extraOffset;

    public PlacementCalculator(float tileGap, Vector3 extraOffset)
    {
        _tileGap = tileGap; _extraOffset = extraOffset;
    }

    public Vector3 ComputeNewOrigin(
        TileLayout candidate, int rotSteps, Vector2Int entryOriented, Vector2Int dirOut,
        Vector3 prevMin, Vector3 prevMax, Vector3 prevExitWorld)
    {
        float cs = candidate.cellSize;
        Vector3 entryLocal = new Vector3(entryOriented.x * cs, 0f, entryOriented.y * cs);
        Vector3 outWorld = new Vector3(dirOut.x, 0f, dirOut.y) * (cs + _tileGap);
        return (prevExitWorld + outWorld) - entryLocal + _extraOffset;
    }

    public void ComputeAABB(Vector3 origin, int w, int h, float cellSize, out Vector3 min, out Vector3 max)
    {
        Vector3 half = new Vector3(cellSize * 0.5f, 0f, cellSize * 0.5f);
        Vector3 size = new Vector3(w * cellSize, 0f, h * cellSize);
        min = origin - half;
        max = origin - half + size;
    }
}
