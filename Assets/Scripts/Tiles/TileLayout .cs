using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Grid/Tile Layout 5x5", fileName = "NewTileLayout")]
public class TileLayout : ScriptableObject
{
    public enum TileType { Grass, Path }

    [Header("Grid Settings")]
    [Min(1)] public int gridWidth = 5;
    [Min(1)] public int gridHeight = 5;
    [Min(0.1f)] public float cellSize = 1f;
    public Vector3 origin = Vector3.zero;

    [Header("Core Configuration")]
    public bool hasCore = true;
    public Vector2Int coreCell = new Vector2Int(-1, -1);

    [System.Serializable]
    public struct TileEntry
    {
        public Vector2Int grid;
        public TileType type;
    }

    [Header("Tile Definitions")]
    public List<TileEntry> tiles = new List<TileEntry>();

    [Header("Path Endpoints")]
    public Vector2Int entry = new Vector2Int(-1, -1);
    public List<Vector2Int> exits = new List<Vector2Int>();

    [System.Serializable]
    public struct PathModifiers
    {
        [Min(0)] public float dps;          // daño por segundo
        [Range(0f, 1f)] public float slow;  // 0..1 (0.3 = 30% más lento)
        [Min(0)] public float stun;         // segundos de stun
    }

    [Header("Path Modifiers (Defaults for the whole tile)")]
    [Tooltip("Útil para tiles 'temáticas': Daño, Slow, Stun. Las básicas quedarán en 0.")]
    public PathModifiers defaultPathMods;

    [System.Serializable]
    public struct PathModOverride
    {
        public Vector2Int grid;   // celda del layout
        public PathModifiers mods; // valores específicos para esa celda
    }

    [Header("Path Modifiers (Overrides por celda)")]
    public List<PathModOverride> perCellOverrides = new List<PathModOverride>();

    // =========================
    // CACHÉS (runtime)
    // =========================
    // Mapa rápido de celdas -> tipo
    private Dictionary<Vector2Int, TileType> _tileMap;
    // Conjunto de celdas Path
    private HashSet<Vector2Int> _pathCells;
    // Overrides por celda
    private Dictionary<Vector2Int, PathModifiers> _modsByCell;

    private void OnEnable() => BuildCaches();
#if UNITY_EDITOR
    private void OnValidate()
    {
        // Sanitizar tamaños y posiciones para evitar errores sutiles
        gridWidth = Mathf.Max(1, gridWidth);
        gridHeight = Mathf.Max(1, gridHeight);

        ClampInside(ref entry);
        for (int i = 0; i < exits.Count; i++) { var e = exits[i]; ClampInside(ref e); exits[i] = e; }
        if (coreCell.x >= 0 || coreCell.y >= 0) { ClampInside(ref coreCell); }

        // Rebuild caches cuando se edita el asset
        BuildCaches();
    }
#endif

    private void ClampInside(ref Vector2Int g)
    {
        if (g.x < 0 || g.y < 0) return; // valor “no seteado”
        g.x = Mathf.Clamp(g.x, 0, gridWidth - 1);
        g.y = Mathf.Clamp(g.y, 0, gridHeight - 1);
    }

    private void BuildCaches()
    {
        _tileMap = _tileMap ?? new Dictionary<Vector2Int, TileType>(tiles.Count);
        _pathCells = _pathCells ?? new HashSet<Vector2Int>();
        _modsByCell = _modsByCell ?? new Dictionary<Vector2Int, PathModifiers>();

        _tileMap.Clear();
        _pathCells.Clear();
        _modsByCell.Clear();

        // Construir mapa y set de paths en O(n)
        for (int i = 0; i < tiles.Count; i++)
        {
            var t = tiles[i];
            if (!IsInside(t.grid)) continue; // ignorar fuera de rango
            _tileMap[t.grid] = t.type;
            if (t.type == TileType.Path) _pathCells.Add(t.grid);
        }

        // Overrides por celda
        for (int i = 0; i < perCellOverrides.Count; i++)
        {
            var ov = perCellOverrides[i];
            if (!IsInside(ov.grid)) continue;
            _modsByCell[ov.grid] = ov.mods;
        }
    }

    // =========================
    // API
    // =========================
    public Vector3 GridToWorld(Vector2Int g)
        => origin + new Vector3(g.x * cellSize, 0f, g.y * cellSize);

    public bool IsInside(Vector2Int g)
        => g.x >= 0 && g.x < gridWidth && g.y >= 0 && g.y < gridHeight;

    // IMPORTANTE: ahora realmente busca por coordenada en el diccionario
    public bool TryGetTile(Vector2Int g, out TileEntry t)
    {
        if (_tileMap != null && _tileMap.TryGetValue(g, out var type))
        {
            t = new TileEntry { grid = g, type = type };
            return true;
        }
        t = default;
        return false;
    }

    public bool IsPath(Vector2Int g)
    {
        if (!IsInside(g)) return false;
        // O(1) si hay caché; fallback a TryGetTile si aún no se construyó
        if (_pathCells != null) return _pathCells.Contains(g);
        return TryGetTile(g, out var t) && t.type == TileType.Path;
    }

    public static readonly Vector2Int[] OrthoDirs = new Vector2Int[]
    {
        Vector2Int.up,
        Vector2Int.right,
        Vector2Int.down,
        Vector2Int.left
    };

    /// <summary>
    /// Obtiene la celda donde colocar el Core.
    /// </summary>
    public Vector2Int GetCoreCell()
    {
        // Si se especificó una celda específica y es válida, usarla
        if (coreCell.x >= 0 && coreCell.y >= 0 &&
            coreCell.x < gridWidth && coreCell.y < gridHeight &&
            IsPath(coreCell))
        {
            return coreCell;
        }

        // Si no, usar la entrada
        if (IsInside(entry) && IsPath(entry))
        {
            return entry;
        }

        // Fallback: buscar cualquier celda path (scan O(n), rápido con _pathCells)
        if (_pathCells != null && _pathCells.Count > 0)
        {
            // devolver la primera arbitraria
            foreach (var c in _pathCells) return c;
        }
        else
        {
            for (int y = 0; y < gridHeight; y++)
                for (int x = 0; x < gridWidth; x++)
                {
                    var cell = new Vector2Int(x, y);
                    if (IsPath(cell)) return cell;
                }
        }

        return new Vector2Int(0, 0); // Último recurso
    }

    public bool AutoDetectEndpoints(out Vector2Int autoEntry, out List<Vector2Int> autoExits)
    {
        autoEntry = new Vector2Int(-1, -1);
        autoExits = new List<Vector2Int>();

        if (tiles == null || tiles.Count == 0) return false;

        var degree1 = new List<Vector2Int>();

        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                var p = new Vector2Int(x, y);
                if (!IsPath(p)) continue;

                int neighPath = 0;
                foreach (var d in OrthoDirs)
                    if (IsPath(p + d)) neighPath++;

                if (neighPath == 1)
                    degree1.Add(p);
            }
        }

        if (degree1.Count == 0) return false;

        if (IsInside(entry) && IsPath(entry))
        {
            autoEntry = entry;
            foreach (var p in degree1)
                if (p != entry && !autoExits.Contains(p)) autoExits.Add(p);
        }
        else
        {
            autoEntry = degree1[0];
            for (int i = 1; i < degree1.Count; i++)
                autoExits.Add(degree1[i]);
        }

        return true;
    }

    public PathModifiers GetPathModifiers(Vector2Int cell)
    {
        if (!IsPath(cell)) return default;

        if (_modsByCell != null && _modsByCell.TryGetValue(cell, out var mods))
            return mods;

        return defaultPathMods;
    }
}
