using System.Collections.Generic;

public static class RunLoadout
{
    private static readonly List<TileLayout> tiles = new List<TileLayout>();
    private static readonly List<TurretDataSO> turrets = new List<TurretDataSO>();
    private static bool initialized;

    public static IReadOnlyList<TileLayout> Tiles
    {
        get
        {
            EnsureInitialized();
            return tiles;
        }
    }

    public static IReadOnlyList<TurretDataSO> Turrets
    {
        get
        {
            EnsureInitialized();
            return turrets;
        }
    }

    public static bool ContainsTurret(TurretDataSO turret)
    {
        EnsureInitialized();
        return turret != null && turrets.Contains(turret);
    }

    public static void Set(IEnumerable<TileLayout> newTiles, IEnumerable<TurretDataSO> newTurrets)
    {
        tiles.Clear();
        turrets.Clear();
        foreach (var t in newTiles) if (t != null && !tiles.Contains(t)) tiles.Add(t);
        foreach (var t in newTurrets) if (t != null && !turrets.Contains(t)) turrets.Add(t);
        initialized = true;
    }

    public static void EnsureInitialized()
    {
        if (initialized && tiles.Count > 0 && turrets.Count > 0) return;
        LoadoutStore.Load(out var loadedTiles, out var loadedTurrets);
        Set(loadedTiles, loadedTurrets);
        Log.Info(nameof(RunLoadout), $"Loadout loaded from storage: {tiles.Count} tiles, {turrets.Count} turrets");
    }

    public static void Reset()
    {
        tiles.Clear();
        turrets.Clear();
        initialized = false;
    }
}
