using System.Collections.Generic;
using UnityEngine;

public sealed class GhostLayoutInstantiator : ILayoutInstantiator
{
    public struct GhostCell
    {
        public Renderer[] renderers;
        public bool isPath;
        public TileLayout.PathModifiers mods;
    }

    private readonly IPrefabSelector prefabs;
    private readonly IOrientationService orientation;
    private readonly Vector3 grassOffset;
    private readonly Vector3 pathOffset;

    public readonly List<GhostCell> LastCells = new();

    public GhostLayoutInstantiator(IPrefabSelector prefabs, IOrientationService orientation, Vector3 grassOffset, Vector3 pathOffset)
    {
        this.prefabs = prefabs;
        this.orientation = orientation;
        this.grassOffset = grassOffset;
        this.pathOffset = pathOffset;
    }

    public int InstantiateLayout(TileLayout layout, Vector3 worldOrigin, int rotSteps, bool flip, Transform parent)
    {
        LastCells.Clear();
        if (layout == null || prefabs == null || orientation == null) return 0;

        Vector2Int? coreSkip = null;
        if (layout.hasCore)
            coreSkip = orientation.ApplyToCell(layout.GetCoreCell(), layout, rotSteps, flip);

        int count = 0;
        var rot = Quaternion.Euler(0f, rotSteps * 90f, 0f);

        for (int y = 0; y < layout.gridHeight; y++)
            for (int x = 0; x < layout.gridWidth; x++)
            {
                var cell = new Vector2Int(x, y);
                bool isPath = layout.IsPath(cell);

                var orientedCell = orientation.ApplyToCell(cell, layout, rotSteps, flip);
                if (coreSkip.HasValue && orientedCell == coreSkip.Value) continue;

                var mods = isPath ? layout.GetPathModifiers(cell) : default;
                GameObject prefab = isPath ? prefabs.ChooseForMods(mods) : prefabs.Grass;
                if (!prefab) continue;

                Vector3 local = orientation.CellToWorldLocal(cell, layout, rotSteps, flip);
                Vector3 offset = isPath ? pathOffset : grassOffset;

                var go = GhostVisuals.InstantiateInert(prefab, worldOrigin + local + offset, rot, parent);
                if (go == null) continue;
                go.name = $"Ghost_{prefab.name}_({x},{y})";

                LastCells.Add(new GhostCell
                {
                    renderers = go.GetComponentsInChildren<Renderer>(true),
                    isPath = isPath,
                    mods = mods
                });
                count++;
            }

        return count;
    }
}
