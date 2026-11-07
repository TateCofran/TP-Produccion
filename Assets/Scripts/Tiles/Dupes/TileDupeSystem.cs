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
    private static readonly Dictionary<string, TileLayout> unifiedKeys = new();

    private readonly Dictionary<TileLayout, TileLevelData> tileLevels = new();

    public event Action<TileLayout, TileLevelData> OnTileLevelUp;
    public event Action<TileLayout> OnDupeChanged;

    public static bool UnifiedActive => GameModifiersManager.Stat(ModifierStat.UnifiedGround) > 0f;

    private void Awake()
    {
        unifiedKeys.Clear();
    }

    public TileLevelData GetTileLevelData(TileLayout layout)
    {
        if (layout == null)
        {
            Debug.LogWarning("[TileDupeSystem] GetTileLevelData called with null layout.");
            return new TileLevelData();
        }

        layout = KeyOf(layout);

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

        layout = KeyOf(layout);
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
        unifiedKeys.Clear();
    }

    public void MergeByEffect()
    {
        var groups = new Dictionary<TileLayout, List<KeyValuePair<TileLayout, TileLevelData>>>();
        foreach (var kv in tileLevels)
        {
            var canonical = KeyOf(kv.Key);
            if (!groups.TryGetValue(canonical, out var list))
            {
                list = new List<KeyValuePair<TileLayout, TileLevelData>>();
                groups[canonical] = list;
            }
            list.Add(kv);
        }

        tileLevels.Clear();
        foreach (var group in groups)
        {
            int total = 0;
            foreach (var kv in group.Value) total += TotalDupes(kv.Value);

            var merged = new TileLevelData();
            for (int i = 0; i < total; i++) merged.AddDupe();
            tileLevels[group.Key] = merged;

            if (group.Value.Count > 1)
                Log.Info(nameof(TileDupeSystem), $"Unified {group.Value.Count} layouts into {group.Key.name}: Lv {merged.currentLevel}");

            OnDupeChanged?.Invoke(group.Key);
            OnTileLevelUp?.Invoke(group.Key, merged);
        }
    }

    public static int TotalDupes(TileLevelData data)
    {
        if (data == null) return 0;
        int total = data.currentDupes;
        int requirement = DupeProgression.InitialDupesRequired;
        for (int level = 1; level < data.currentLevel; level++)
        {
            total += requirement;
            requirement = DupeProgression.GetNextRequirement(requirement);
        }
        return total;
    }

    public static TileLayout KeyOf(TileLayout layout)
    {
        if (layout == null) return null;
        var key = layout.DupeKey;
        if (!UnifiedActive) return key;

        string effect = EffectKeyOf(key);
        if (!unifiedKeys.TryGetValue(effect, out var canonical) || canonical == null)
        {
            canonical = key;
            unifiedKeys[effect] = canonical;
        }
        return canonical;
    }

    public static string EffectKeyOf(TileLayout layout)
    {
        if (layout == null) return "None";
        string main = MainEffect(layout.defaultPathMods);
        if (main == "Basic" && layout.perCellOverrides != null)
        {
            foreach (var o in layout.perCellOverrides)
            {
                main = MainEffect(o.mods);
                if (main != "Basic") break;
            }
        }
        return main;
    }

    private static string MainEffect(TileLayout.PathModifiers mods)
    {
        if (mods.dps > 0f) return "Dps";
        if (mods.slow > 0f) return "Slow";
        if (mods.stun > 0f) return "Stun";
        if (mods.exposure) return "Exposure";
        if (mods.antiHeal) return "AntiHeal";
        return "Basic";
    }
}
