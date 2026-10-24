using System.Collections.Generic;
using UnityEngine;

public sealed class RunnerService : IRunnerService
{
    private readonly IExitRepository _exits;
    private readonly IChainRepository _chain;
    private readonly ICoreService _core;
    private readonly IOrientationService _orientation;
    private readonly TileLinkRepository _links;
    private readonly GameObject _runnerPrefab;
    private readonly float _yOffset;

    private readonly Dictionary<long, int> _nodeIndex = new();
    private readonly List<Vector3> _nodeWorld = new();
    private readonly List<List<int>> _adjacency = new();
    private readonly Dictionary<int, List<Vector3>> _routeCache = new();
    private int[] _next = new int[0];
    private int[] _distance = new int[0];
    private int _goal = -1;

    private int _cachedChainCount = -1;
    private int _cachedLinkCount = -1;
    private Vector3 _cachedCorePosition;
    private float _cellSize = 1f;

    public RunnerService(
        IExitRepository exits,
        IChainRepository chain,
        ICoreService core,
        IOrientationService orientation,
        TileLinkRepository links,
        GameObject runnerPrefab,
        float yOffset)
    {
        _exits = exits;
        _chain = chain;
        _core = core;
        _orientation = orientation;
        _links = links;
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
        route = null;
        if (!_core.HasCore) return false;

        EnsureGraph();

        int start = FindNode(exitWorldPos);
        if (start < 0 || _distance[start] < 0) return false;

        if (_routeCache.TryGetValue(start, out route))
            return true;

        float y = _core.Position.y + _yOffset;
        route = new List<Vector3>(_distance[start] + 1);

        int current = start;
        int guard = _nodeWorld.Count + 1;
        while (guard-- > 0)
        {
            var w = _nodeWorld[current];
            w.y = y;
            route.Add(w);

            if (current == _goal)
            {
                _routeCache[start] = route;
                return true;
            }
            current = _next[current];
        }

        route = null;
        return false;
    }

    public bool TryGetRouteLength(Vector3 worldPos, out int cells)
    {
        cells = -1;
        if (!_core.HasCore) return false;

        EnsureGraph();

        int node = FindNode(worldPos);
        if (node < 0 || _distance[node] < 0) return false;

        cells = _distance[node];
        return true;
    }

    private void SpawnRunnerFollowingPath(Vector3 exitWorldPos)
    {
        if (_runnerPrefab == null)
        {
            Debug.LogWarning("[RunnerService] runnerPrefab is not assigned");
            return;
        }

        if (!TryGetRouteExitToCore(exitWorldPos, out var path))
        {
            path = new List<Vector3> { exitWorldPos + Vector3.up * _yOffset };
            if (_core.HasCore) path.Add(_core.Position + Vector3.up * _yOffset);
        }

        var go = Object.Instantiate(_runnerPrefab, path[0], Quaternion.identity);
        go.name = "Enemy";

        var mover = go.GetComponent<Enemy>();
        if (!mover) mover = go.AddComponent<Enemy>();
        mover.Init(path);
    }

    private static long Key(int tile, Vector2Int cell)
        => ((long)tile << 32) | ((long)(cell.x & 0xFFFF) << 16) | (long)(cell.y & 0xFFFF);

    private int AddNode(int tile, Vector2Int cell, Vector3 world)
    {
        long key = Key(tile, cell);
        if (_nodeIndex.TryGetValue(key, out int existing)) return existing;

        int id = _nodeWorld.Count;
        _nodeIndex[key] = id;
        _nodeWorld.Add(world);
        _adjacency.Add(new List<int>(4));
        return id;
    }

    private void Connect(int a, int b)
    {
        if (a < 0 || b < 0 || a == b) return;
        if (!_adjacency[a].Contains(b)) _adjacency[a].Add(b);
        if (!_adjacency[b].Contains(a)) _adjacency[b].Add(a);
    }

    private int GetNode(int tile, Vector2Int cell)
        => _nodeIndex.TryGetValue(Key(tile, cell), out int id) ? id : -1;

    private void EnsureGraph()
    {
        Vector3 corePos = _core.Position;
        int linkCount = _links != null ? _links.Count : 0;

        if (_chain.Count == _cachedChainCount && linkCount == _cachedLinkCount && corePos == _cachedCorePosition)
            return;

        _cachedChainCount = _chain.Count;
        _cachedLinkCount = linkCount;
        _cachedCorePosition = corePos;

        _routeCache.Clear();
        _nodeIndex.Clear();
        _nodeWorld.Clear();
        _adjacency.Clear();
        _goal = -1;

        _cellSize = (_chain.Count > 0 && _chain.Get(0).layout) ? _chain.Get(0).layout.cellSize : 1f;

        for (int i = 0; i < _chain.Count; i++)
        {
            var pt = _chain.Get(i);
            if (pt.layout == null) continue;

            foreach (var t in pt.layout.tiles)
            {
                if (t.type != TileLayout.TileType.Path) continue;
                var w = pt.worldOrigin + _orientation.CellToWorldLocal(t.grid, pt.layout, pt.rotSteps, pt.flipped);
                AddNode(i, t.grid, w);
            }
        }

        for (int i = 0; i < _chain.Count; i++)
        {
            var pt = _chain.Get(i);
            if (pt.layout == null) continue;

            foreach (var t in pt.layout.tiles)
            {
                if (t.type != TileLayout.TileType.Path) continue;
                int a = GetNode(i, t.grid);
                Connect(a, GetNode(i, t.grid + Vector2Int.right));
                Connect(a, GetNode(i, t.grid + Vector2Int.up));
            }
        }

        for (int i = 0; i < linkCount; i++)
        {
            var l = _links.Get(i);
            Connect(GetNode(l.tileA, l.cellA), GetNode(l.tileB, l.cellB));
        }

        int count = _nodeWorld.Count;
        _next = new int[count];
        _distance = new int[count];
        for (int i = 0; i < count; i++) { _next[i] = -1; _distance[i] = -1; }

        if (!_core.HasCore || count == 0) return;

        _goal = FindNearestNode(corePos, float.MaxValue);
        if (_goal < 0) return;

        var queue = new Queue<int>();
        queue.Enqueue(_goal);
        _distance[_goal] = 0;
        _next[_goal] = _goal;

        while (queue.Count > 0)
        {
            int u = queue.Dequeue();
            var neighbors = _adjacency[u];
            for (int n = 0; n < neighbors.Count; n++)
            {
                int v = neighbors[n];
                if (_distance[v] >= 0) continue;
                _distance[v] = _distance[u] + 1;
                _next[v] = u;
                queue.Enqueue(v);
            }
        }
    }

    private int FindNode(Vector3 world) => FindNearestNode(world, _cellSize * 0.75f);

    private int FindNearestNode(Vector3 world, float maxDistance)
    {
        int best = -1;
        float bestD2 = maxDistance == float.MaxValue ? float.MaxValue : maxDistance * maxDistance;

        for (int i = 0; i < _nodeWorld.Count; i++)
        {
            var p = _nodeWorld[i];
            float dx = p.x - world.x;
            float dz = p.z - world.z;
            float d2 = dx * dx + dz * dz;
            if (d2 <= bestD2)
            {
                bestD2 = d2;
                best = i;
            }
        }

        return best;
    }
}
