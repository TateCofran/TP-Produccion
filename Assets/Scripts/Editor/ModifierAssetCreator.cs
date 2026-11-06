using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ModifierAssetCreator
{
    private const string NormalFolder = "Assets/Scriptable Object/Modifiers/Normal";
    private const string OtherFolder = "Assets/Scriptable Object/Modifiers/Other";

    private struct Definition
    {
        public string id;
        public string name;
        public string description;
        public ModifierCategory category;
        public (ModifierStat stat, float value)[] entries;
    }

    [MenuItem("Tools/Shadow Realms/Create Modifier Assets")]
    public static void CreateModifierAssets()
    {
        EnsureFolder(NormalFolder);
        EnsureFolder(OtherFolder);

        int created = 0;
        int skipped = 0;

        foreach (var def in GetDefinitions())
        {
            string folder = def.category == ModifierCategory.OtherWorld ? OtherFolder : NormalFolder;
            string fileName = $"{def.id}_{def.name.Replace(" ", string.Empty)}.asset";
            string path = $"{folder}/{fileName}";

            if (AssetDatabase.LoadAssetAtPath<ModifierSO>(path) != null)
            {
                skipped++;
                continue;
            }

            var so = ScriptableObject.CreateInstance<ModifierSO>();
            so.id = def.id;
            so.displayName = def.name;
            so.description = def.description;
            so.category = def.category;
            so.entries = new List<ModifierEntry>();
            foreach (var (stat, value) in def.entries)
                so.entries.Add(new ModifierEntry { stat = stat, value = value });

            AssetDatabase.CreateAsset(so, path);
            created++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[ModifierAssetCreator] Created {created} modifier assets, skipped {skipped} existing ones");
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        string parent = Path.GetDirectoryName(path).Replace("\\", "/");
        string leaf = Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }

    private static Definition N(string id, string name, string description, params (ModifierStat, float)[] entries)
    {
        return new Definition { id = id, name = name, description = description, category = ModifierCategory.NormalWorld, entries = entries };
    }

    private static Definition O(string id, string name, string description, params (ModifierStat, float)[] entries)
    {
        return new Definition { id = id, name = name, description = description, category = ModifierCategory.OtherWorld, entries = entries };
    }

    private static IEnumerable<Definition> GetDefinitions()
    {
        yield return N("N01", "Fast Builder", "The tile bar fills 20% faster.", (ModifierStat.TileBarSpeed, 0.2f));
        yield return N("N02", "Wide Choice", "One more tile option every time the tile bar fills.", (ModifierStat.TileOptionsCount, 1f));
        yield return N("N03", "Reroll", "Reroll the tile options once per selection.", (ModifierStat.TileRerolls, 1f));
        yield return N("N04", "Burning Ground", "Damage tiles deal 25% more damage.", (ModifierStat.TileDamage, 0.25f));
        yield return N("N05", "Frozen Ground", "Slow tiles slow 15% more.", (ModifierStat.TileSlow, 0.15f));
        yield return N("N06", "Shock Stones", "Stun tiles stun for 0.3 more seconds.", (ModifierStat.TileStunSeconds, 0.3f));
        yield return N("N07", "Toxic Trail", "Basic path tiles deal 1 damage per hit.", (ModifierStat.BasicTileDamage, 1f));
        yield return N("N08", "Lingering Curse", "Slow and stun last 1 more second after leaving the tile.", (ModifierStat.EffectLingerSeconds, 1f));
        yield return N("N09", "Twin Tiles", "20% chance to gain an extra copy when placing a tile.", (ModifierStat.TileExtraDupeChance, 0.2f));
        yield return N("N10", "Heavy Soil", "All enemies move 8% slower.", (ModifierStat.EnemySpeed, -0.08f));
        yield return N("N11", "Fog Harvest", "Each revealed fog tile grants 1 Normal essence.", (ModifierStat.FogRevealEssence, 1f));
        yield return N("N12", "Pathfinder", "Reward zones are always visible and give 50% more reward.", (ModifierStat.RewardZoneAlwaysVisible, 1f), (ModifierStat.RewardZoneBonus, 0.5f));
        yield return N("N13", "Essence Road", "Gain 25% more Normal essences.", (ModifierStat.BlueEssence, 0.25f));
        yield return N("N14", "Narrow Path", "One less tile option, but the tile bar fills 35% faster.", (ModifierStat.TileOptionsCount, -1f), (ModifierStat.TileBarSpeed, 0.35f));
        yield return N("N15", "Shift Charge", "Switching to the Normal world charges the tile bar by 15.", (ModifierStat.ShiftChargeTileBar, 15f));

        yield return O("O01", "Fast Forge", "The turret bar fills 20% faster.", (ModifierStat.TurretBarSpeed, 0.2f));
        yield return O("O02", "Armory", "One more turret option every time the turret bar fills.", (ModifierStat.TurretOptionsCount, 1f));
        yield return O("O03", "Sharpened", "Turrets deal 15% more damage.", (ModifierStat.TurretDamage, 0.15f));
        yield return O("O04", "Overclock", "Turrets fire 15% faster.", (ModifierStat.TurretFireRate, 0.15f));
        yield return O("O05", "Eagle Eye", "Turrets have 15% more range.", (ModifierStat.TurretRange, 0.15f));
        yield return O("O06", "Wildfire", "When a burning enemy dies, the burn spreads to the closest enemy.", (ModifierStat.FireSpreadOnDeath, 1f));
        yield return O("O07", "Chain Lightning", "Tesla turrets hit one more enemy.", (ModifierStat.TeslaExtraTargets, 1f));
        yield return O("O08", "Deadeye", "Every 4th Sniper shot deals double damage.", (ModifierStat.SniperNthShotDouble, 4f));
        yield return O("O09", "Power Grid", "Energy turrets generate 30% more energy.", (ModifierStat.EnergyOutput, 0.3f));
        yield return O("O10", "Twin Forge", "20% chance to gain an extra copy when choosing a turret.", (ModifierStat.TurretExtraDupeChance, 0.2f));
        yield return O("O11", "Executioner", "Turrets deal 50% more damage to enemies below 25% health.", (ModifierStat.ExecuteBonus, 0.5f));
        yield return O("O12", "Boss Hunter", "Turrets deal 30% more damage to mini bosses and bosses.", (ModifierStat.BossBonus, 0.3f));
        yield return O("O13", "Essence Reaper", "Gain 25% more Other World essences.", (ModifierStat.RedEssence, 0.25f));
        yield return O("O14", "Blood Price", "Turrets deal 40% more damage, but the core loses 3 max health.", (ModifierStat.TurretDamage, 0.4f), (ModifierStat.CoreMaxHealth, -3f));
        yield return O("O15", "Horde Bounty", "30% more enemies, but essences per kill are doubled.", (ModifierStat.EnemyCount, 0.3f), (ModifierStat.BlueEssence, 1f), (ModifierStat.RedEssence, 1f));
    }
}
