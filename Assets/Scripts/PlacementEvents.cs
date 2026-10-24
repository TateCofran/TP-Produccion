using System;
using UnityEngine;

public static class PlacementEvents
{
    public struct TurretPlacedInfo
    {
        public GameObject turretInstance;
        public Vector3 worldPosition;
        public string turretId;
    }

    public static event Action<TurretPlacedInfo> OnTurretPlaced;

    public static void RaiseTurretPlaced(TurretPlacedInfo info)
        => OnTurretPlaced?.Invoke(info);

    public struct TileAppliedInfo
    {
        public Vector2Int tileGridCoord;
        public string tileId;
        public bool expandedGrid;
    }

    public static event Action<TileAppliedInfo> OnTileApplied;

    public static void RaiseTileApplied(TileAppliedInfo info)
        => OnTileApplied?.Invoke(info);
}
