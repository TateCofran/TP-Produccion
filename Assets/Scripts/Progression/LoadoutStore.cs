using System.Collections.Generic;
using UnityEngine;

public static class LoadoutStore
{
    private const string TilesKey = "Loadout_Tiles";
    private const string TurretsKey = "Loadout_Turrets";

    public static int MinPerColumn => ProgressionSettingsSO.Instance != null ? ProgressionSettingsSO.Instance.minPerColumn : 3;
    public static int MaxPerColumn => ProgressionSettingsSO.Instance != null ? ProgressionSettingsSO.Instance.maxPerColumn : 5;

    public static void Save(IList<TileLayout> tiles, IList<TurretDataSO> turrets)
    {
        var tileIds = new List<string>();
        foreach (var t in tiles) if (t != null) tileIds.Add(t.name);
        var turretIds = new List<string>();
        foreach (var t in turrets) if (t != null) turretIds.Add(TurretId(t));
        PlayerPrefs.SetString(TilesKey, string.Join(",", tileIds));
        PlayerPrefs.SetString(TurretsKey, string.Join(",", turretIds));
        PlayerPrefs.Save();
    }

    public static void Load(out List<TileLayout> tiles, out List<TurretDataSO> turrets)
    {
        tiles = new List<TileLayout>();
        turrets = new List<TurretDataSO>();
        var table = ProgressionStore.Table;
        if (table == null) return;

        var tileIds = new HashSet<string>(PlayerPrefs.GetString(TilesKey, string.Empty).Split(','));
        foreach (var t in table.AllTiles())
            if (tiles.Count < MaxPerColumn && tileIds.Contains(t.name) && !tiles.Contains(t) && ProgressionStore.IsUnlocked(t)) tiles.Add(t);

        var turretIds = new HashSet<string>(PlayerPrefs.GetString(TurretsKey, string.Empty).Split(','));
        foreach (var t in table.AllTurrets())
            if (turrets.Count < MaxPerColumn && turretIds.Contains(TurretId(t)) && !turrets.Contains(t) && ProgressionStore.IsUnlocked(t)) turrets.Add(t);

        foreach (var t in table.startingTiles)
            if (tiles.Count < MinPerColumn && t != null && !tiles.Contains(t)) tiles.Add(t);
        foreach (var t in table.startingTurrets)
            if (turrets.Count < MinPerColumn && t != null && !turrets.Contains(t)) turrets.Add(t);
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(TilesKey);
        PlayerPrefs.DeleteKey(TurretsKey);
    }

    public static string TurretId(TurretDataSO turret)
    {
        return turret == null ? string.Empty : !string.IsNullOrEmpty(turret.id) ? turret.id : turret.name;
    }
}
