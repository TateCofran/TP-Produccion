// TileDupeSystem.cs
using System.Collections.Generic;
using UnityEngine;

public class TileDupeSystem : MonoBehaviour, ITileDupeSystem
{
    private Dictionary<string, TileLevelData> tileLevels = new Dictionary<string, TileLevelData>();

    public event System.Action<TileDataSO, TileLevelData> OnTileLevelUp;

    public TileLevelData GetTileLevelData(TileDataSO tile)
    {
        string tileId = GetTileId(tile);
        if (tileLevels.ContainsKey(tileId)) return tileLevels[tileId];
        return new TileLevelData();
    }

    public void AddDupe(TileDataSO tile)
    {
        string tileId = GetTileId(tile);
        if (!tileLevels.ContainsKey(tileId)) tileLevels[tileId] = new TileLevelData();

        TileLevelData levelData = tileLevels[tileId];
        int previousLevel = levelData.currentLevel;
        levelData.AddDupe();
        if (levelData.currentLevel > previousLevel) OnTileLevelUp?.Invoke(tile, levelData);
    }

    private string GetTileId(TileDataSO tile)
    {
        return !string.IsNullOrEmpty(tile.id) ? tile.id : tile.name;
    }

    public void ResetAllLevels()
    {
        tileLevels.Clear();
    }
}
