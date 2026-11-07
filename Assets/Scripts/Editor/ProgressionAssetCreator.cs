using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ProgressionAssetCreator
{
    private const string Folder = "Assets/Scriptable Object/Progression";
    private const string TablePath = Folder + "/UnlockTable.asset";
    private const string CurvePath = Folder + "/XpCurve.asset";
    private const string SettingsPath = "Assets/Resources/ProgressionSettings.asset";

    private static readonly List<string> missing = new List<string>();

    [MenuItem("Tools/Shadow Realms/Create Progression Assets (MD 13)")]
    public static void Create()
    {
        missing.Clear();
        EnsureFolder(Folder);
        EnsureFolder("Assets/Resources");

        var table = AssetDatabase.LoadAssetAtPath<UnlockTableSO>(TablePath);
        if (table == null)
        {
            table = ScriptableObject.CreateInstance<UnlockTableSO>();
            AssetDatabase.CreateAsset(table, TablePath);
        }
        Fill(table);
        EditorUtility.SetDirty(table);

        var curve = AssetDatabase.LoadAssetAtPath<XpCurveSO>(CurvePath);
        if (curve == null)
        {
            curve = ScriptableObject.CreateInstance<XpCurveSO>();
            curve.baseXp = 20f;
            curve.exponent = 2f;
            AssetDatabase.CreateAsset(curve, CurvePath);
        }

        var settings = AssetDatabase.LoadAssetAtPath<ProgressionSettingsSO>(SettingsPath);
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<ProgressionSettingsSO>();
            AssetDatabase.CreateAsset(settings, SettingsPath);
        }
        settings.unlockTable = table;
        settings.xpCurve = curve;
        settings.synergyTable = Load<SynergyTableSO>("SynergyTable", "t:SynergyTableSO");
        settings.fallbackTiles = new[] { Tile("Tile-Up"), Tile("Tile-Left") };
        EditorUtility.SetDirty(settings);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (missing.Count > 0) Debug.LogWarning("[ProgressionAssetCreator] Missing assets: " + string.Join(", ", missing));
        else Debug.Log("[ProgressionAssetCreator] Progression assets ready. Every asset of the unlock table was found");
    }

    private static void Fill(UnlockTableSO table)
    {
        table.startingTiles = Tiles("Tile-Up", "Tile-Left", "Tile-UpDPS");
        table.startingTurrets = Turrets(TurretType.Basic, TurretType.Fire, TurretType.Tesla);
        table.startingNormalModifiers = Mods("N01", "N02", "N04", "N09", "N13");
        table.startingOtherModifiers = Mods("O01", "O02", "O03", "O04", "O10");

        table.normalLevels = new List<UnlockLevel>
        {
            N(2, new[] { "Tile-UpSlow" }, "N05"),
            N(3, new[] { "Tile-Up-Left", "Tile-LeftDPS" }),
            N(4, new[] { "Tile-UpStun" }, "N06"),
            N(5, new[] { "Tile-Serpent" }, "N03"),
            N(6, new[] { "Tile-LeftSlow", "Tile-LeftStun" }),
            N(7, new[] { "Tile-Plaza" }, "N10"),
            N(8, new[] { "Tile-DeadEnd" }, "N14"),
            N(9, new[] { "Tile-Hybrid-SlowDps", "Tile-ForkT" }, "N07"),
            N(10, new string[0], "LN1"),
            N(11, new[] { "Tile-UpDPS-Normal", "Tile-Up-LeftDPS" }, "N08"),
            N(12, new[] { "Tile-UpExposure", "Tile-Up-LeftSlow" }, "N11"),
            N(13, new[] { "Tile-Hybrid-StunDps", "Tile-Up-LeftStun" }, "N12"),
            N(14, new[] { "Tile-LeftAntiHeal", "Tile-alldirections" }, "N15"),
            N(15, new[] { "Tile-alldirectionsDPS", "Tile-alldirectionsSlow" }, "LN2"),
            N(16, new[] { "Tile-UpSlow-Other", "Tile-alldirectionsStun" }, "LN3"),
        };

        table.otherLevels = new List<UnlockLevel>
        {
            O(2, TurretType.Sniper, "O05"),
            O(3, TurretType.Mortar),
            O(4, TurretType.Ballista, "O11"),
            O(5, TurretType.Energy, "O13"),
            O(6, TurretType.WarBanner, "O06"),
            O(7, TurretType.Guillotine, "O07"),
            O(8, TurretType.Purifier, "O08"),
            O(9, TurretType.RiftCannon, "O12"),
            O(10, null, "LO1"),
            O(11, TurretType.Reaper, "O09"),
            O(12, TurretType.Warden, "O14"),
            O(13, null, "O15", "LO2"),
            O(14, null, "LO3"),
        };
    }

    private static UnlockLevel N(int level, string[] tiles, params string[] mods)
    {
        return new UnlockLevel { level = level, tiles = Tiles(tiles), turrets = new List<TurretDataSO>(), modifiers = Mods(mods) };
    }

    private static UnlockLevel O(int level, TurretType? turret, params string[] mods)
    {
        return new UnlockLevel
        {
            level = level,
            tiles = new List<TileLayout>(),
            turrets = turret.HasValue ? Turrets(turret.Value) : new List<TurretDataSO>(),
            modifiers = Mods(mods)
        };
    }

    private static List<TileLayout> Tiles(params string[] names)
    {
        var result = new List<TileLayout>();
        foreach (var n in names)
        {
            var t = Tile(n);
            if (t != null) result.Add(t);
        }
        return result;
    }

    private static TileLayout Tile(string name)
    {
        foreach (var guid in AssetDatabase.FindAssets("t:TileLayout"))
        {
            var t = AssetDatabase.LoadAssetAtPath<TileLayout>(AssetDatabase.GUIDToAssetPath(guid));
            if (t != null && t.name == name) return t;
        }
        missing.Add(name);
        return null;
    }

    private static List<TurretDataSO> Turrets(params TurretType[] types)
    {
        var result = new List<TurretDataSO>();
        foreach (var type in types)
        {
            TurretDataSO found = null;
            foreach (var guid in AssetDatabase.FindAssets("t:TurretDataSO"))
            {
                var t = AssetDatabase.LoadAssetAtPath<TurretDataSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (t != null && t.turretType == type && t.prefab != null) { found = t; break; }
            }
            if (found != null) result.Add(found);
            else missing.Add("Turret " + type);
        }
        return result;
    }

    private static List<ModifierSO> Mods(params string[] ids)
    {
        var result = new List<ModifierSO>();
        foreach (var id in ids)
        {
            ModifierSO found = null;
            foreach (var guid in AssetDatabase.FindAssets("t:ModifierSO"))
            {
                var m = AssetDatabase.LoadAssetAtPath<ModifierSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (m != null && m.id == id) { found = m; break; }
            }
            if (found != null) result.Add(found);
            else missing.Add("Modifier " + id);
        }
        return result;
    }

    private static T Load<T>(string name, string filter) where T : Object
    {
        foreach (var guid in AssetDatabase.FindAssets(filter))
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
            if (a != null && a.name == name) return a;
        }
        return null;
    }

    [MenuItem("Tools/Shadow Realms/Reset Progression (XP and Loadout)")]
    public static void ResetProgression()
    {
        ProgressionStore.ResetAll();
        Debug.Log("[ProgressionAssetCreator] Progression reset: XP 0 in both bars, loadout cleared");
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace("\\", "/");
        string leaf = Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }
}
