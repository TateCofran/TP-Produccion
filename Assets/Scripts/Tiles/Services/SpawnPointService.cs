using System.Collections.Generic;
using UnityEngine;

public sealed class SpawnPointsService : ISpawnPointsService
{
    private readonly IExitRepository _exits;
    private readonly IOrientationService _orientation;
    private readonly IChainRepository _chain;
    private readonly ICoreService _core;
    private readonly float _minDistanceToCore;
    private readonly bool _excludeCoreNeighbors;
    private readonly float _cellSize;

    private readonly Queue<Vector3> _lastUsed = new Queue<Vector3>();
    private readonly int _capacity;
    private int _rr = 0;

    public SpawnPointsService(
        IExitRepository exits,
        IChainRepository chain,
        ICoreService core,
        IOrientationService orientation,
        float cellSize,
        float minDistanceToCore,
        bool excludeCoreNeighbors,
        int lastUsedCapacity = 6)
    {
        _exits = exits;
        _chain = chain;
        _core = core;
        _orientation = orientation;
        _cellSize = Mathf.Max(0.0001f, cellSize);
        _minDistanceToCore = minDistanceToCore;
        _excludeCoreNeighbors = excludeCoreNeighbors;
        _capacity = Mathf.Max(1, lastUsedCapacity);
    }

    public List<Vector3> GetSpawnPoints()
    {
        var list = new List<Vector3>();

        foreach (var tuple in _exits.GetAvailableWorld(_chain.Get))
        {
            var w = tuple.worldPos;
            if (_excludeCoreNeighbors && IsCoreNeighborWorld(w)) continue;

            if (_core.HasCore && _minDistanceToCore > 0f)
            {
                if (Vector3.Distance(new Vector3(w.x, 0f, w.z),
                                     new Vector3(_core.Position.x, 0f, _core.Position.z)) < _minDistanceToCore)
                    continue;
            }

            if (!IsDuplicate(w, list)) list.Add(w);
        }

        // Fallback: usar exits del último tile
        if (list.Count == 0 && _chain.Count > 0)
        {
            var last = _chain.Get(_chain.Count - 1);
            if (last.layout?.exits != null)
            {
                foreach (var ex in last.layout.exits)
                {
                    var world = last.worldOrigin +
                                _orientation.CellToWorldLocal(ex, last.layout, last.rotSteps, last.flipped);
                    if (!IsDuplicate(world, list)) list.Add(world);
                }
            }
        }

        return list;
    }

    public Vector3 GetNextRoundRobin()
    {
        var pts = GetSpawnPoints();
        if (pts.Count == 0) return Vector3.zero;

        var idx = _rr % pts.Count;
        _rr = (_rr + 1) % pts.Count;

        var chosen = pts[idx];
        _lastUsed.Enqueue(chosen);
        while (_lastUsed.Count > _capacity) _lastUsed.Dequeue();
        return chosen;
    }

    private bool IsCoreNeighborWorld(Vector3 w)
    {
        if (!_core.HasCore) return false;
        return Vector3.Distance(new Vector3(w.x, 0f, w.z),
                                new Vector3(_core.Position.x, 0f, _core.Position.z)) < (_cellSize * 1.1f);
    }

    private static bool IsDuplicate(Vector3 w, List<Vector3> list, float threshold = 0.05f)
    {
        foreach (var p in list) if (Vector3.Distance(p, w) < threshold) return true;
        return false;
    }
}
