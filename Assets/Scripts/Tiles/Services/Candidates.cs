using System.Collections.Generic;
using UnityEngine;

public sealed class CandidateProvider : ICandidateProvider
{
    private readonly bool _worldPlusZUsesYZeroEdge;

    public CandidateProvider(bool worldPlusZUsesYZeroEdge)
    {
        _worldPlusZUsesYZeroEdge = worldPlusZUsesYZeroEdge;
    }

    public IEnumerable<(TileLayout layout, int rot, bool flip)> GetValidCandidates(
        IEnumerable<TileLayout> all, Vector2Int dirOut, bool allowRot, bool allowFlip, IOrientationService orientation)
    {
        foreach (var candidate in all)
        {
            if (!candidate) continue;

            int rotMax = allowRot ? 4 : 1;
            for (int rot = 0; rot < rotMax; rot++)
            {
                int flipCount = allowFlip ? 2 : 1;
                for (int f = 0; f < flipCount; f++)
                {
                    bool flip = allowFlip && (f == 1);
                    var entry = candidate.entry;
                    if (!candidate.IsInside(entry) || !candidate.IsPath(entry)) continue;

                    var o = orientation.GetOrientedData(candidate, rot, flip);

                    bool okBorde =
                        (dirOut == Vector2Int.right) ? (o.entryOriented.x == 0) :
                        (dirOut == Vector2Int.left) ? (o.entryOriented.x == o.w - 1) :
                        (dirOut == Vector2Int.up) ? (o.entryOriented.y == (_worldPlusZUsesYZeroEdge ? 0 : o.h - 1)) :
                                                       (o.entryOriented.y == (_worldPlusZUsesYZeroEdge ? o.h - 1 : 0));

                    if (okBorde) yield return (candidate, rot, flip);
                }
            }
        }
    }
}
