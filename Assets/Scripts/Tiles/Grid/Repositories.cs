using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class ChainRepository : IChainRepository
{
    private readonly List<PlacedTile> _chain = new List<PlacedTile>(64);
    public int Count => _chain.Count;
    public PlacedTile Get(int index) => _chain[index];
    public IReadOnlyList<PlacedTile> GetAll() => _chain;
    public void Clear() => _chain.Clear();
    public void Add(PlacedTile tile) => _chain.Add(tile);
}

public sealed class ExitRepository : IExitRepository
{
    private readonly List<ExitRecord> _exits = new List<ExitRecord>(64);

    public int Count => _exits.Count;
    public ExitRecord Get(int index) => _exits[index];
    public void Clear() => _exits.Clear();
    public void Add(ExitRecord exit) => _exits.Add(exit);

    public IEnumerable<int> IndicesDisponibles()
    {
        for (int i = 0; i < _exits.Count; i++)
            if (!_exits[i].Used && !_exits[i].Closed) yield return i;
    }

    public void MarkUsed(int globalIndex)
    {
        var e = _exits[globalIndex]; e.SetUsed(true); _exits[globalIndex] = e;
    }

    public void MarkClosed(int globalIndex, string reason = "EXIT CERRADO")
    {
        var e = _exits[globalIndex]; e.SetClosed(true); e.label = reason; _exits[globalIndex] = e;
    }

    public int GlobalIndexFromAvailable(int availableIdx)
    {
        int count = 0;
        foreach (var i in IndicesDisponibles())
        {
            if (count == availableIdx) return i;
            count++;
        }
        return -1;
    }

    public int AvailableIndexByLabel(string label)
    {
        int count = 0;
        for (int i = 0; i < _exits.Count; i++)
        {
            var e = _exits[i];
            if (e.Used || e.Closed) continue;
            if (e.label == label) return count;
            count++;
        }
        return -1;
    }

    public void Relabel()
    {
        int n = 0;
        for (int i = 0; i < _exits.Count; i++)
        {
            var e = _exits[i];
            if (e.Used || e.Closed) continue;
            e.label = IndexToLetters(n++);
            _exits[i] = e;
        }
    }

    private readonly IOrientationService _orientation; // dependencia inyectada

    public ExitRepository(IOrientationService orientation)
    {
        _orientation = orientation ?? throw new ArgumentNullException(nameof(orientation));
    }

    public IEnumerable<(string label, Vector3 worldPos)> GetAvailableWorld(PlacedTileGetter placedGetter)
    {
        for (int i = 0; i < _exits.Count; i++)
        {
            var e = _exits[i];
            if (e.Used || e.Closed) continue;

            var pt = placedGetter(e.tileIndex);
            Vector3 w = pt.worldOrigin +
                _orientation.CellToWorldLocal(e.cell, pt.layout, pt.rotSteps, pt.flipped);

            yield return (e.label, w);
        }
    }

    private static string IndexToLetters(int index)
    {
        string s = ""; index += 1;
        while (index > 0)
        {
            int rem = (index - 1) % 26;
            s = (char)('A' + rem) + s;
            index = (index - 1) / 26;
        }
        return s;
    }
}
