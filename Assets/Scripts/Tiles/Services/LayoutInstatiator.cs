using System.Collections.Generic;
using UnityEngine;

public sealed class LayoutInstantiator : ILayoutInstantiator
{
    private readonly IPrefabSelector _prefabs;
    private readonly IOrientationService _orientation;
    private readonly ITilePoolManager _pool;
    private readonly Vector3 _grassOffset;
    private readonly Vector3 _pathOffset;

    public LayoutInstantiator(
        IPrefabSelector prefabs,
        IOrientationService orientation,
        ITilePoolManager pool,
        Vector3 grassOffset,
        Vector3 pathOffset)
    {
        _prefabs = prefabs; _orientation = orientation; _pool = pool;
        _grassOffset = grassOffset; _pathOffset = pathOffset;
    }

    public int InstantiateLayout(TileLayout layout, Vector3 worldOrigin, int rotSteps, bool flip, Transform parent)
    {
        var map = new Dictionary<Vector2Int, TileLayout.TileType>(layout.tiles.Count);
        foreach (var t in layout.tiles)
        {
            if (t.grid.x < 0 || t.grid.x >= layout.gridWidth || t.grid.y < 0 || t.grid.y >= layout.gridHeight) continue;
            map[t.grid] = t.type;
        }

        Vector2Int? coreSkip = null;
        if (layout.hasCore)
        {
            var coreCell = layout.GetCoreCell();
            coreSkip = _orientation.ApplyToCell(coreCell, layout, rotSteps, flip);
        }

        int count = 0;

        for (int y = 0; y < layout.gridHeight; y++)
            for (int x = 0; x < layout.gridWidth; x++)
            {
                var cell = new Vector2Int(x, y);
                if (!map.TryGetValue(cell, out var type)) type = TileLayout.TileType.Grass;

                var orientedCell = _orientation.ApplyToCell(cell, layout, rotSteps, flip);
                if (layout.hasCore && coreSkip.HasValue && orientedCell == coreSkip.Value) continue;

                GameObject prefab = (type == TileLayout.TileType.Path)
                    ? _prefabs.ChooseForMods(layout.GetPathModifiers(cell).dps, layout.GetPathModifiers(cell).slow, layout.GetPathModifiers(cell).stun)
                    : _prefabs.Grass;
                if (!prefab) continue;

                Vector3 local = _orientation.CellToWorldLocal(cell, layout, rotSteps, flip);
                Vector3 offset = (type == TileLayout.TileType.Path) ? _pathOffset : _grassOffset;
                var rot = Quaternion.Euler(0f, rotSteps * 90f, 0f);

                var go = (_pool != null)
                    ? _pool.GetPooledObject(prefab, worldOrigin + local + offset, rot, parent)
                    : Object.Instantiate(prefab, worldOrigin + local + offset, rot, parent);

                go.name = $"{prefab.name}_({x},{y})";
                count++;

                if (type == TileLayout.TileType.Grass) SetupGrassCell(go, layout);
                else SetupPathCell(go, layout, cell);
            }

        return count;
    }

    private static void SetupGrassCell(GameObject go, TileLayout layout)
    {
        go.tag = "Cell";
        if (!go.TryGetComponent<BoxCollider>(out var col)) col = go.AddComponent<BoxCollider>();
        col.size = new Vector3(layout.cellSize, 0.9f, layout.cellSize);
        col.center = new Vector3(0f, 0.05f, 0f);
        if (!go.TryGetComponent<CellSlot>(out _)) go.AddComponent<CellSlot>();
        go.layer = LayerMask.NameToLayer("Cell");
    }

    private static void SetupPathCell(GameObject go, TileLayout layout, Vector2Int cell)
    {
        var mods = layout.GetPathModifiers(cell);
        if (go.TryGetComponent<PathCellEffect>(out var eff)) eff.Setup(mods.dps, mods.slow, mods.stun);
    }
}
