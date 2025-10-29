using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class PathExitTracker : MonoBehaviour
{
    [Header("Grid / Mundo")]
    [Tooltip("Tamaño de cada celda en unidades de mundo.")]
    public float cellSize = 1f;

    [Tooltip("Origen del grid en mundo (celda 0,0).")]
    public Vector3 worldOrigin = Vector3.zero;

    [Tooltip("Altura a la que se dibujan los gizmos.")]
    public float gizmoY = 0.03f;

    [Tooltip("Límites del grid (ancho x alto). Si es 0, se ignora el 'solo en borde'.")]
    public Vector2Int gridSize = new Vector2Int(0, 0);

    [Header("Cálculo de salidas")]
    [Tooltip("Si está activo, considera salida solo a endpoints que estén en el borde del grid.")]
    public bool onlyBorderExits = false;

    [Tooltip("Celda del Core en coordenadas de grid (opcional, para evitar marcarla como salida).")]
    public Vector2Int coreCell = new Vector2Int(int.MinValue, int.MinValue);

    [Header("Historial de spawns")]
    [Tooltip("Cuántas últimas salidas usadas guardamos (para debug/round-robin).")]
    public int lastUsedCapacity = 6;

    [Header("Gizmos")]
    public bool drawGizmos = true;
    public bool drawLabels = true;
    public float sphereRadius = 0.12f;
    public Color exitColor = Color.green;
    public Color recentlyUsedColor = Color.yellow;
    public Color coreColor = new Color(0f, 0.9f, 0.9f);
    public Color gridBoundsColor = new Color(1f, 1f, 1f, 0.25f);

    // --- Estado interno ---
    // Celdas de camino actuales
    private readonly HashSet<Vector2Int> _pathCells = new HashSet<Vector2Int>();

    // Endpoints recalculados
    private readonly List<Vector2Int> _exits = new List<Vector2Int>();

    // Últimas salidas usadas (mundo)
    private readonly Queue<Vector3> _lastUsedWorld = new Queue<Vector3>();

    // Cache de vecinos 4-dir
    private static readonly Vector2Int[] _dirs =
    {
        new Vector2Int( 1, 0),
        new Vector2Int(-1, 0),
        new Vector2Int( 0, 1),
        new Vector2Int( 0,-1),
    };

    // --------------------------------------------------------
    // API PRINCIPAL
    // --------------------------------------------------------

    /// <summary>Registrar una celda de camino.</summary>
    public void RegisterPathCell(Vector2Int gridPos)
    {
        if (_pathCells.Add(gridPos))
            RebuildExits();
    }

    /// <summary>Remover una celda de camino.</summary>
    public void RemovePathCell(Vector2Int gridPos)
    {
        if (_pathCells.Remove(gridPos))
            RebuildExits();
    }

    /// <summary>Resetea por completo y carga un set nuevo de celdas de path.</summary>
    public void ResetAndLoad(IEnumerable<Vector2Int> cells)
    {
        _pathCells.Clear();
        if (cells != null)
        {
            foreach (var c in cells) _pathCells.Add(c);
        }
        RebuildExits();
    }

    /// <summary>Devuelve las salidas en coordenadas de grid.</summary>
    public IReadOnlyList<Vector2Int> GetExitGridPositions() => _exits;

    /// <summary>Devuelve las salidas en mundo (ordenadas como en _exits).</summary>
    public List<Vector3> GetExitWorldPositions()
    {
        var list = new List<Vector3>(_exits.Count);
        foreach (var g in _exits) list.Add(GridToWorld(g));
        return list;
    }

    /// <summary>Marca una salida como usada (para debug/historial).</summary>
    public void MarkExitUsed(Vector3 worldPos)
    {
        _lastUsedWorld.Enqueue(worldPos);
        while (_lastUsedWorld.Count > Mathf.Max(1, lastUsedCapacity))
            _lastUsedWorld.Dequeue();
    }

    /// <summary>Últimas posiciones usadas (mundo) - solo lectura.</summary>
    public IReadOnlyCollection<Vector3> GetLastUsedExits() => _lastUsedWorld;

    /// <summary>
    /// Siguiente salida para round-robin simple (devuelve en mundo).
    /// Podés guardar fuera el índice entre llamadas si querés hacer RR perfecto.
    /// </summary>
    public Vector3 GetNextExitRoundRobin(ref int rrIndex)
    {
        if (_exits.Count == 0) return Vector3.zero;
        if (rrIndex < 0) rrIndex = 0;
        var idx = rrIndex % _exits.Count;
        rrIndex = (rrIndex + 1) % _exits.Count;
        var w = GridToWorld(_exits[idx]);
        MarkExitUsed(w);
        return w;
    }

    /// <summary>
    /// Útil para SpawnManager: devuelve puntos válidos de spawn (mundo) filtrando por distancia al Core si se lo pasás.
    /// </summary>
    public List<Vector3> GetSpawnPoints(float minDistanceToCore = 0f, bool excludeCoreNeighbors = true)
    {
        var result = new List<Vector3>(_exits.Count);
        var coreWorld = (coreCell.x != int.MinValue) ? GridToWorld(coreCell) : (Vector3?)null;

        foreach (var g in _exits)
        {
            // Evitar vecinos inmediatos del Core si se pide
            if (excludeCoreNeighbors && IsCoreNeighbor(g))
                continue;

            var w = GridToWorld(g);
            if (coreWorld.HasValue && minDistanceToCore > 0f)
            {
                if (Vector3.Distance(w, coreWorld.Value) < minDistanceToCore) continue;
            }
            result.Add(w);
        }
        return result;
    }

    // --------------------------------------------------------
    // CÁLCULO DE SALIDAS (ENDPOINTS)
    // --------------------------------------------------------

    private void RebuildExits()
    {
        _exits.Clear();
        if (_pathCells.Count == 0) return;

        foreach (var cell in _pathCells)
        {
            // No contar el Core como salida
            if (cell == coreCell) continue;

            // Grado = cuántos vecinos 4-dir también son path
            int deg = 0;
            for (int i = 0; i < _dirs.Length; i++)
            {
                var n = cell + _dirs[i];
                if (_pathCells.Contains(n)) deg++;
                if (deg > 1) break; // ya no es endpoint
            }

            if (deg == 1) // endpoint
            {
                if (onlyBorderExits && gridSize.x > 0 && gridSize.y > 0)
                {
                    if (!IsBorder(cell)) continue;
                }
                _exits.Add(cell);
            }
        }
    }

    private bool IsBorder(Vector2Int c)
    {
        if (gridSize.x <= 0 || gridSize.y <= 0) return false;
        return (c.x <= 0 || c.y <= 0 || c.x >= gridSize.x - 1 || c.y >= gridSize.y - 1);
    }

    private bool IsCoreNeighbor(Vector2Int c)
    {
        if (coreCell.x == int.MinValue) return false;
        for (int i = 0; i < _dirs.Length; i++)
        {
            if (c + _dirs[i] == coreCell) return true;
        }
        return false;
    }

    // --------------------------------------------------------
    // CONVERSIÓN GRID <-> MUNDO
    // --------------------------------------------------------

    public Vector3 GridToWorld(Vector2Int g)
        => new Vector3(worldOrigin.x + g.x * cellSize, worldOrigin.y + gizmoY, worldOrigin.z + g.y * cellSize);

    public Vector2Int WorldToGrid(Vector3 w)
    {
        var x = Mathf.RoundToInt((w.x - worldOrigin.x) / Mathf.Max(0.0001f, cellSize));
        var y = Mathf.RoundToInt((w.z - worldOrigin.z) / Mathf.Max(0.0001f, cellSize));
        return new Vector2Int(x, y);
    }

    // --------------------------------------------------------
    // UNITY HOOKS
    // --------------------------------------------------------

    private void OnValidate()
    {
        cellSize = Mathf.Max(0.01f, cellSize);
        sphereRadius = Mathf.Max(0.01f, sphereRadius);
        if (lastUsedCapacity < 1) lastUsedCapacity = 1;
        // Recalcular por si cambió configuración
        RebuildExits();
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        // Borde del grid (si está definido)
        if (gridSize.x > 0 && gridSize.y > 0)
        {
            Gizmos.color = gridBoundsColor;
            var size = new Vector3(gridSize.x * cellSize, 0.001f, gridSize.y * cellSize);
            var center = worldOrigin + new Vector3((gridSize.x - 1) * 0.5f * cellSize, gizmoY, (gridSize.y - 1) * 0.5f * cellSize);
            Gizmos.DrawWireCube(center, size);
        }

        // Core
        if (coreCell.x != int.MinValue)
        {
            Gizmos.color = coreColor;
            Gizmos.DrawCube(GridToWorld(coreCell), Vector3.one * sphereRadius * 1.2f);
            if (drawLabels)
                DrawWorldLabel(GridToWorld(coreCell), "CORE");
        }

        // Exits
        Gizmos.color = exitColor;
        for (int i = 0; i < _exits.Count; i++)
        {
            var p = GridToWorld(_exits[i]);
            Gizmos.DrawWireSphere(p, sphereRadius);
            if (drawLabels) DrawWorldLabel(p, $"EXIT {i}");
        }

        // Últimos usados
        Gizmos.color = recentlyUsedColor;
        foreach (var w in _lastUsedWorld)
        {
            Gizmos.DrawSphere(new Vector3(w.x, worldOrigin.y + gizmoY, w.z), sphereRadius * 0.9f);
            if (drawLabels) DrawWorldLabel(w, "used");
        }
    }

    private void DrawWorldLabel(Vector3 pos, string text)
    {
#if UNITY_EDITOR
        UnityEditor.Handles.color = Color.white;
        var style = new GUIStyle(UnityEditor.EditorStyles.boldLabel);
        style.fontSize = 11;
        UnityEditor.Handles.Label(pos + Vector3.up * (sphereRadius * 1.6f), text, style);
#endif
    }

    // --------------------------------------------------------
    // HELPERS OPCIONALES
    // --------------------------------------------------------

    /// <summary>
    /// Reconstruye el set de path a partir de transforms hijos con un tag o layer específico.
    /// Útil si ya tenés instancias en escena y querés sincronizar el tracker.
    /// </summary>
    public void RebuildFromChildren(Transform root, string pathTag = null, int pathLayer = -1)
    {
        _pathCells.Clear();
        if (!root) root = this.transform;

        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            bool include = false;

            if (!string.IsNullOrEmpty(pathTag) && t.CompareTag(pathTag))
                include = true;

            if (pathLayer >= 0 && t.gameObject.layer == pathLayer)
                include = true;

            if (!string.IsNullOrEmpty(pathTag) || pathLayer >= 0)
            {
                if (!include) continue;
            }
            else
            {
                // Si no filtra por tag/layer, omití el propio objeto del tracker
                if (t == this.transform) continue;
            }

            var g = WorldToGrid(t.position);
            _pathCells.Add(g);
        }

        RebuildExits();
    }
}
