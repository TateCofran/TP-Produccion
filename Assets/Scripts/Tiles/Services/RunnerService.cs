using System.Collections.Generic;
using UnityEngine;

public sealed class RunnerService : IRunnerService
{
    private readonly IExitRepository _exits;
    private readonly IChainRepository _chain;
    private readonly ICoreService _core;
    private readonly IOrientationService _orientation;
    private readonly GameObject _runnerPrefab;
    private readonly float _yOffset;

    public RunnerService(
        IExitRepository exits,
        IChainRepository chain,
        ICoreService core,
        IOrientationService orientation,
        GameObject runnerPrefab,
        float yOffset)
    {
        _exits = exits;
        _chain = chain;
        _core = core;
        _orientation = orientation;
        _runnerPrefab = runnerPrefab;
        _yOffset = yOffset;
    }

    public void SpawnRunnersAtAllOpenExits()
    {
        foreach (var (_, worldPos) in _exits.GetAvailableWorld(_chain.Get))
            SpawnRunnerFollowingPath(worldPos);
    }

    public bool TryGetRouteExitToCore(Vector3 exitWorldPos, out List<Vector3> route)
    {
        return TryBuildRoute(exitWorldPos, out route);
    }

    // --------- Internos ---------

    private void SpawnRunnerFollowingPath(Vector3 exitWorldPos)
    {
        if (_runnerPrefab == null)
        {
            Debug.LogWarning("[RunnerService] runnerPrefab no asignado.");
            return;
        }

        if (!TryBuildRoute(exitWorldPos, out var path))
        {
            // Fallback: del exit al core en línea recta
            path = new List<Vector3> { exitWorldPos + Vector3.up * _yOffset };
            if (_core.HasCore) path.Add(_core.Position + Vector3.up * _yOffset);
        }

        var go = Object.Instantiate(_runnerPrefab, path[0], Quaternion.identity);
        go.name = "Enemy";

        var mover = go.GetComponent<Enemy>();
        if (!mover) mover = go.AddComponent<Enemy>();
        mover.Init(path);
    }

    private bool TryBuildRoute(Vector3 exitWorld, out List<Vector3> route)
    {
        route = null;
        if (!_core.HasCore) return false;

        float cs = (_chain.Count > 0 && _chain.Get(0).layout) ? _chain.Get(0).layout.cellSize : 1f;

        // Construir grafo de celdas path
        var nodes = new HashSet<Vector3Int>();
        var adj = new Dictionary<Vector3Int, List<Vector3Int>>();

        for (int i = 0; i < _chain.Count; i++)
        {
            var pt = _chain.Get(i);
            if (pt.layout == null) continue;

            foreach (var t in pt.layout.tiles)
            {
                if (t.type != TileLayout.TileType.Path) continue;

                var w = pt.worldOrigin + _orientation.CellToWorldLocal(t.grid, pt.layout, pt.rotSteps, pt.flipped);
                var k = WorldToKey(w, cs);
                if (nodes.Add(k)) adj[k] = new List<Vector3Int>();
            }
        }

        var dirs = new Vector3Int[]
        {
            new Vector3Int(1,0,0), new Vector3Int(-1,0,0),
            new Vector3Int(0,0,1), new Vector3Int(0,0,-1)
        };

        foreach (var k in nodes)
        {
            foreach (var d in dirs)
            {
                var nb = new Vector3Int(k.x + d.x, 0, k.z + d.z);
                if (nodes.Contains(nb)) adj[k].Add(nb);
            }
        }

        var startKey = WorldToKey(SnapToCell(exitWorld, cs), cs);
        var goalKey = WorldToKey(SnapToCell(_core.Position, cs), cs);

        if (!nodes.Contains(startKey) || !nodes.Contains(goalKey)) return false;

        // BFS
        var q = new Queue<Vector3Int>();
        var prev = new Dictionary<Vector3Int, Vector3Int>();
        var vis = new HashSet<Vector3Int>();

        q.Enqueue(startKey);
        vis.Add(startKey);

        bool found = false;
        while (q.Count > 0)
        {
            var u = q.Dequeue();
            if (u == goalKey) { found = true; break; }

            foreach (var v in adj[u])
            {
                if (vis.Contains(v)) continue;
                vis.Add(v);
                prev[v] = u;
                q.Enqueue(v);
            }
        }

        if (!found) return false;

        // reconstruir camino
        var keys = new List<Vector3Int>();
        for (var cur = goalKey; ;)
        {
            keys.Add(cur);
            if (cur == startKey) break;
            cur = prev[cur];
        }
        keys.Reverse();

        route = new List<Vector3>(keys.Count);
        foreach (var k in keys)
        {
            var w = KeyToWorld(k, cs);
            w.y = _core.Position.y + _yOffset;
            route.Add(w);
        }

        return route.Count > 0;
    }

    private static Vector3 SnapToCell(Vector3 w, float cs)
    {
        float x = Mathf.Round(w.x / cs) * cs;
        float z = Mathf.Round(w.z / cs) * cs;
        return new Vector3(x, w.y, z);
    }

    private static Vector3Int WorldToKey(Vector3 w, float cs)
        => new Vector3Int(Mathf.RoundToInt(w.x / cs), 0, Mathf.RoundToInt(w.z / cs));

    private static Vector3 KeyToWorld(Vector3Int k, float cs)
        => new Vector3(k.x * cs, 0f, k.z * cs);
}
