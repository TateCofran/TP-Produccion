using System;
using System.Collections.Generic;
using UnityEngine;

public interface ITileDupeSystem
{
    TileLevelData GetTileLevelData(TileLayout layout);
    void AddDupe(TileLayout layout);

    event Action<TileLayout, TileLevelData> OnTileLevelUp;

    event Action<TileLayout> OnDupeChanged;
}

public sealed class TileDupeSystem : MonoBehaviour, ITileDupeSystem
{
    private readonly Dictionary<TileLayout, TileLevelData> tileLevels = new();

    public event Action<TileLayout, TileLevelData> OnTileLevelUp;
    public event Action<TileLayout> OnDupeChanged;

    public TileLevelData GetTileLevelData(TileLayout layout)
    {
        if (layout == null)
        {
            Debug.LogWarning("[TileDupeSystem] GetTileLevelData called with null layout.");
            return new TileLevelData();
        }

        if (!tileLevels.TryGetValue(layout, out var data))
        {
            data = new TileLevelData();
            tileLevels[layout] = data;
        }

        return data;
    }

    public void AddDupe(TileLayout layout)
    {
        if (layout == null)
        {
            Debug.LogWarning("[TileDupeSystem] AddDupe called with null layout.");
            return;
        }

        var data = GetTileLevelData(layout);
        bool leveledUp = data.AddDupe();

        float extraChance = GameModifiersManager.Stat(ModifierStat.TileExtraDupeChance);
        if (extraChance > 0f && UnityEngine.Random.value < extraChance)
        {
            Log.Info("TileDupeSystem", $"Extra dupe granted for {layout.name}");
            leveledUp |= data.AddDupe();
        }

        OnDupeChanged?.Invoke(layout);

        if (leveledUp)
            OnTileLevelUp?.Invoke(layout, data);
    }

    public void ResetAllLevels()
    {
        tileLevels.Clear();
    }
}
