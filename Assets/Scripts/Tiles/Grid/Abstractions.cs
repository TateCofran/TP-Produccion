using System.Collections.Generic;
using UnityEngine;

public interface ITileGenerator
{
    void GenerateFirst();
}

public interface ITilePoolManager
{
    GameObject GetPooledObject(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent);
    void ReturnToPool(GameObject prefab, GameObject obj);
    void InitializePools();
}

public interface ISpatialPartitioner
{
    void AddToSpatialGrid(PlacedTile tile, int index);
    bool OverlapsAnyOptimized(Vector3 nMin, Vector3 nMax);
    void ClearSpatialGrid();
}

public interface IOrientationService
{
    OrientedData GetOrientedData(TileLayout layout, int rotSteps, bool flip);
    void OrientedSize(TileLayout layout, int rotSteps, out int w, out int h);
    Vector2Int ApplyToCell(Vector2Int cell, TileLayout layout, int rotSteps, bool flip);
    Vector2Int ApplyToDir(Vector2Int dir, int rotSteps, bool flip);
    Vector2Int ApplyInverseToDir(Vector2Int dir, int rotSteps, bool flip);
    Vector3 CellToWorldLocal(Vector2Int cell, TileLayout layout, int rotSteps, bool flip);
}

public interface IChainRepository
{
    void Add(PlacedTile tile);
    PlacedTile Get(int index);
    int Count { get; }
    IReadOnlyList<PlacedTile> GetAll();
    void Clear();
}

public interface IExitRepository
{
    void Add(ExitRecord exit);
    ExitRecord Get(int index);
    int Count { get; }
    void Clear();
    IEnumerable<int> IndicesDisponibles();
    void MarkUsed(int globalIndex);
    void MarkClosed(int globalIndex, string reason = "EXIT CERRADO");
    int GlobalIndexFromAvailable(int availableIdx);
    int AvailableIndexByLabel(string label);
    void Relabel();
    IEnumerable<(string label, Vector3 worldPos)> GetAvailableWorld(PlacedTileGetter getter);
}

public interface ICoreService
{
    bool HasCore { get; }
    Vector3 Position { get; }
    void SpawnCore(Vector3 pos);
    void ClearCore();
}

public interface ISpawnPointsService
{
    List<Vector3> GetSpawnPoints();
    Vector3 GetNextRoundRobin();
}

public interface ISpatialOverlapChecker
{
    void Clear();
    void Add(PlacedTile tile, int tileIndex);
    bool OverlapsAny(Vector3 min, Vector3 max);
}

public interface IPrefabSelector
{
    GameObject Grass { get; }
    GameObject PathBasic { get; }
    GameObject PathDamage { get; }
    GameObject PathSlow { get; }
    GameObject PathStun { get; }
    GameObject ChooseForMods(float damage, float slow, float stun);
}

public interface ILayoutInstantiator
{
    int InstantiateLayout(TileLayout layout, Vector3 origin, int rotSteps, bool flip, Transform parent);
}

public interface IPlacementCalculator
{
    Vector3 ComputeNewOrigin(
        TileLayout candidate, int rotSteps, Vector2Int entryOriented, Vector2Int dirOut,
        Vector3 prevMin, Vector3 prevMax, Vector3 prevExitWorld);
    void ComputeAABB(Vector3 origin, int w, int h, float cellSize, out Vector3 min, out Vector3 max);
}

public interface ICandidateProvider
{
    IEnumerable<(TileLayout layout, int rot, bool flip)> GetValidCandidates(
        IEnumerable<TileLayout> all, Vector2Int dirOut, bool allowRot, bool allowFlip, IOrientationService orientation);
}

public interface IRunnerService
{
    void SpawnRunnersAtAllOpenExits();

    bool TryGetRouteExitToCore(Vector3 exitWorldPos, out List<Vector3> route);
}