using System.Collections.Generic;
using UnityEngine;
using World = ShiftingWorldMechanic.World;

public class UnlockedItem
{
    public enum Kind { Tile, Turret, Modifier }

    public Kind kind;
    public TileLayout tile;
    public TurretDataSO turret;
    public ModifierSO modifier;
    public int level;
    public bool normalBar;

    public string DisplayName
    {
        get
        {
            switch (kind)
            {
                case Kind.Tile: return tile != null ? tile.name.Replace("Tile-", string.Empty) : "?";
                case Kind.Turret: return turret != null ? (!string.IsNullOrEmpty(turret.displayName) ? turret.displayName : turret.name) : "?";
                default: return modifier != null ? modifier.displayName : "?";
            }
        }
    }

    public string Caption => kind == Kind.Tile ? "New tile" : kind == Kind.Turret ? "New turret" : "New modifier";
    public bool IsLegendary => kind == Kind.Modifier && modifier != null && GameModifiersManager.IsLegendary(modifier.category);
}

public class RunProgressResult
{
    public int blueGained;
    public int redGained;
    public int normalXpBefore;
    public int normalXpAfter;
    public int otherXpBefore;
    public int otherXpAfter;
    public List<int> normalLevelsGained = new List<int>();
    public List<int> otherLevelsGained = new List<int>();
    public List<UnlockedItem> unlocked = new List<UnlockedItem>();
}

public static class ProgressionStore
{
    private const string NormalXpKey = "Progress_XP_Normal";
    private const string OtherXpKey = "Progress_XP_Other";
    private const string MigratedKey = "Progress_Migrated";
    private const string LegacyBlueKey = "TotalBlueEssences";
    private const string LegacyRedKey = "TotalRedEssences";

    public static RunProgressResult LastRun { get; private set; }

    private static ProgressionSettingsSO Settings => ProgressionSettingsSO.Instance;
    public static UnlockTableSO Table => Settings != null ? Settings.unlockTable : null;
    public static XpCurveSO Curve => Settings != null ? Settings.xpCurve : null;

    private static string KeyFor(World mode) => mode == World.Normal ? NormalXpKey : OtherXpKey;

    public static void EnsureMigrated()
    {
        if (PlayerPrefs.GetInt(MigratedKey, 0) == 1) return;
        int blue = PlayerPrefs.GetInt(LegacyBlueKey, 0);
        int red = PlayerPrefs.GetInt(LegacyRedKey, 0);
        if (blue > 0) PlayerPrefs.SetInt(NormalXpKey, PlayerPrefs.GetInt(NormalXpKey, 0) + blue);
        if (red > 0) PlayerPrefs.SetInt(OtherXpKey, PlayerPrefs.GetInt(OtherXpKey, 0) + red);
        PlayerPrefs.DeleteKey(LegacyBlueKey);
        PlayerPrefs.DeleteKey(LegacyRedKey);
        PlayerPrefs.SetInt(MigratedKey, 1);
        PlayerPrefs.Save();
        if (blue > 0 || red > 0) Log.Info(nameof(ProgressionStore), $"Migrated legacy essences to XP: Normal {blue}, Other {red}");
    }

    public static int GetXp(World mode)
    {
        EnsureMigrated();
        return PlayerPrefs.GetInt(KeyFor(mode), 0);
    }

    public static int GetLevel(World mode) => LevelForXp(GetXp(mode), out _, out _);

    public static int LevelForXp(int xp, out int into, out int needed)
    {
        var curve = Curve;
        if (curve == null)
        {
            into = 0;
            needed = 1;
            return 1;
        }
        return curve.LevelForXp(xp, out into, out needed);
    }

    public static List<int> AddXp(World mode, int amount)
    {
        var gained = new List<int>();
        if (amount <= 0) return gained;
        int before = GetXp(mode);
        int levelBefore = LevelForXp(before, out _, out _);
        int after = before + amount;
        PlayerPrefs.SetInt(KeyFor(mode), after);
        PlayerPrefs.Save();
        int levelAfter = LevelForXp(after, out _, out _);
        for (int l = levelBefore + 1; l <= levelAfter; l++) gained.Add(l);
        if (gained.Count > 0) Log.Info(nameof(ProgressionStore), $"{mode} bar reached level {levelAfter}");
        return gained;
    }

    public static RunProgressResult RecordRun(int blue, int red)
    {
        var result = new RunProgressResult
        {
            blueGained = Mathf.Max(0, blue),
            redGained = Mathf.Max(0, red),
            normalXpBefore = GetXp(World.Normal),
            otherXpBefore = GetXp(World.Other)
        };
        result.normalLevelsGained = AddXp(World.Normal, result.blueGained);
        result.otherLevelsGained = AddXp(World.Other, result.redGained);
        result.normalXpAfter = GetXp(World.Normal);
        result.otherXpAfter = GetXp(World.Other);

        var table = Table;
        if (table != null)
        {
            foreach (int l in result.normalLevelsGained) AppendUnlocks(result.unlocked, table.GetLevel(true, l), true);
            foreach (int l in result.otherLevelsGained) AppendUnlocks(result.unlocked, table.GetLevel(false, l), false);
        }

        LastRun = result;
        return result;
    }

    public static void AppendUnlocks(List<UnlockedItem> list, UnlockLevel level, bool normalBar)
    {
        if (level == null) return;
        foreach (var t in level.tiles) if (t != null) list.Add(new UnlockedItem { kind = UnlockedItem.Kind.Tile, tile = t, level = level.level, normalBar = normalBar });
        foreach (var t in level.turrets) if (t != null) list.Add(new UnlockedItem { kind = UnlockedItem.Kind.Turret, turret = t, level = level.level, normalBar = normalBar });
        foreach (var m in level.modifiers) if (m != null) list.Add(new UnlockedItem { kind = UnlockedItem.Kind.Modifier, modifier = m, level = level.level, normalBar = normalBar });
    }

    public static int RequiredLevel(TileLayout tile) => Table != null ? Table.LevelOf(tile) : 1;
    public static int RequiredLevel(TurretDataSO turret) => Table != null ? Table.LevelOf(turret) : 1;

    public static bool IsUnlocked(TileLayout tile)
    {
        if (tile == null) return false;
        if (Table == null) return true;
        int req = Table.LevelOf(tile);
        return req >= 1 && GetLevel(World.Normal) >= req;
    }

    public static bool IsUnlocked(TurretDataSO turret)
    {
        if (turret == null) return false;
        if (Table == null) return true;
        int req = Table.LevelOf(turret);
        return req >= 1 && GetLevel(World.Other) >= req;
    }

    public static bool IsUnlocked(ModifierSO modifier)
    {
        if (modifier == null) return false;
        if (Table == null) return true;
        int req = Table.LevelOf(modifier);
        if (req < 0) return true;
        bool normal = Table.IsNormalModifier(modifier);
        return GetLevel(normal ? World.Normal : World.Other) >= req;
    }

    public static List<TileLayout> GetUnlockedTiles()
    {
        var result = new List<TileLayout>();
        if (Table == null) return result;
        foreach (var t in Table.AllTiles()) if (!result.Contains(t) && IsUnlocked(t)) result.Add(t);
        return result;
    }

    public static List<TurretDataSO> GetUnlockedTurrets()
    {
        var result = new List<TurretDataSO>();
        if (Table == null) return result;
        foreach (var t in Table.AllTurrets()) if (!result.Contains(t) && IsUnlocked(t)) result.Add(t);
        return result;
    }

    public static List<ModifierSO> GetUnlockedModifiers(ModifierCategory category)
    {
        var result = new List<ModifierSO>();
        var manager = GameModifiersManager.Instance;
        if (manager == null) return result;
        IReadOnlyList<ModifierSO> pool;
        switch (category)
        {
            case ModifierCategory.OtherWorld: pool = manager.OtherPool; break;
            case ModifierCategory.LegendaryNormal: pool = manager.LegendaryNormalPool; break;
            case ModifierCategory.LegendaryOther: pool = manager.LegendaryOtherPool; break;
            default: pool = manager.NormalPool; break;
        }
        foreach (var m in pool) if (m != null && IsUnlocked(m)) result.Add(m);
        return result;
    }

    public static void ResetAll()
    {
        PlayerPrefs.DeleteKey(NormalXpKey);
        PlayerPrefs.DeleteKey(OtherXpKey);
        PlayerPrefs.SetInt(MigratedKey, 1);
        LoadoutStore.Clear();
        PlayerPrefs.Save();
        LastRun = null;
    }
}
