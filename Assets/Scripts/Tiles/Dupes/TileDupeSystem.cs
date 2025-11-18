using System;
using System.Collections.Generic;
using UnityEngine;

public interface ITileDupeSystem
{
    TileLevelData GetTileLevelData(TileDataSO tile);
    void AddDupe(TileDataSO tile);


    event Action<TileDataSO, TileLevelData> OnTileLevelUp;

    event Action<TileDataSO> OnDupeChanged;
}

public sealed class TileDupeSystem : MonoBehaviour, ITileDupeSystem
{
    private readonly Dictionary<string, TileLevelData> tileLevels = new();

    public event Action<TileDataSO, TileLevelData> OnTileLevelUp;
    public event Action<TileDataSO> OnDupeChanged;

    private void Awake()
    {
        var provider = FindFirstObjectByType<TileLevelingPermissionProvider>();
        if (provider != null)
        {
            TileLevelData.SetLevelingProvider(provider);
        }
        else
        {
            Debug.LogWarning("[TileDupeSystem] No TileLevelingPermissionProvider found in scene.");
        }
    }

    public TileLevelData GetTileLevelData(TileDataSO tile)
    {
        if (tile == null)
        {
            Debug.LogWarning("[TileDupeSystem] GetTileLevelData called with null tile.");
            return new TileLevelData();
        }

        string id = GetTileId(tile);

        if (!tileLevels.TryGetValue(id, out var data))
        {
            data = new TileLevelData();
            tileLevels[id] = data;
        }

        return data;
    }

    public void AddDupe(TileDataSO tile)
    {
        if (tile == null)
        {
            Debug.LogWarning("[TileDupeSystem] AddDupe called with null tile.");
            return;
        }

        var data = GetTileLevelData(tile);
        int previousLevel = data.currentLevel;

        data.AddDupe();

        OnDupeChanged?.Invoke(tile);

        if (data.currentLevel > previousLevel)
        {
            OnTileLevelUp?.Invoke(tile, data);
        }
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
