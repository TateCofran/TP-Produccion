using System;
using UnityEngine;

public static class PlacementEvents
{
    // ----- TORRETAS -----
    public struct TurretPlacedInfo
    {
        public GameObject turretInstance;   // la instancia creada
        public Vector3 worldPosition;       // dónde se colocó
        public string turretId;             // si usás IDs
    }

    // Invocá esto cuando una torreta se coloque con éxito
    public static event Action<TurretPlacedInfo> OnTurretPlaced;

    public static void RaiseTurretPlaced(TurretPlacedInfo info)
        => OnTurretPlaced?.Invoke(info);


    // ----- TILES / PATH / PIEZAS DE MAPA -----
    public struct TileAppliedInfo
    {
        public Vector2Int tileGridCoord;    // coordenada del tile
        public string tileId;               // si manejás IDs de tile
        public bool expandedGrid;           // útil si fue una expansión
    }

    // Invocá esto cuando un tile se aplique con éxito
    public static event Action<TileAppliedInfo> OnTileApplied;

    public static void RaiseTileApplied(TileAppliedInfo info)
        => OnTileApplied?.Invoke(info);
}
