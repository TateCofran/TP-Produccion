using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class UnlockLevel
{
    [Min(2)] public int level = 2;
    public List<TileLayout> tiles = new List<TileLayout>();
    public List<TurretDataSO> turrets = new List<TurretDataSO>();
    public List<ModifierSO> modifiers = new List<ModifierSO>();
}

[CreateAssetMenu(menuName = "Progression/Unlock Table")]
public class UnlockTableSO : ScriptableObject
{
    [Header("Starting content")]
    public List<TileLayout> startingTiles = new List<TileLayout>();
    public List<TurretDataSO> startingTurrets = new List<TurretDataSO>();
    public List<ModifierSO> startingNormalModifiers = new List<ModifierSO>();
    public List<ModifierSO> startingOtherModifiers = new List<ModifierSO>();

    [Header("Normal bar (tiles and Normal modifiers)")]
    public List<UnlockLevel> normalLevels = new List<UnlockLevel>();

    [Header("Other World bar (turrets and Other modifiers)")]
    public List<UnlockLevel> otherLevels = new List<UnlockLevel>();

    public int MaxNormalLevel => MaxLevel(normalLevels);
    public int MaxOtherLevel => MaxLevel(otherLevels);

    private static int MaxLevel(List<UnlockLevel> levels)
    {
        int max = 1;
        if (levels != null)
            foreach (var l in levels) if (l != null && l.level > max) max = l.level;
        return max;
    }

    public int LevelOf(TileLayout tile)
    {
        if (tile == null) return -1;
        tile = tile.DupeKey;
        if (startingTiles.Contains(tile)) return 1;
        foreach (var l in normalLevels) if (l != null && l.tiles.Contains(tile)) return l.level;
        return -1;
    }

    public int LevelOf(TurretDataSO turret)
    {
        if (turret == null) return -1;
        if (startingTurrets.Contains(turret)) return 1;
        foreach (var l in otherLevels) if (l != null && l.turrets.Contains(turret)) return l.level;
        return -1;
    }

    public int LevelOf(ModifierSO modifier)
    {
        if (modifier == null) return -1;
        if (startingNormalModifiers.Contains(modifier) || startingOtherModifiers.Contains(modifier)) return 1;
        foreach (var l in normalLevels) if (l != null && l.modifiers.Contains(modifier)) return l.level;
        foreach (var l in otherLevels) if (l != null && l.modifiers.Contains(modifier)) return l.level;
        return -1;
    }

    public bool IsNormalModifier(ModifierSO modifier)
    {
        if (modifier == null) return false;
        if (startingNormalModifiers.Contains(modifier)) return true;
        foreach (var l in normalLevels) if (l != null && l.modifiers.Contains(modifier)) return true;
        return false;
    }

    public IEnumerable<TileLayout> AllTiles()
    {
        foreach (var t in startingTiles) if (t != null) yield return t;
        foreach (var l in normalLevels) if (l != null) foreach (var t in l.tiles) if (t != null) yield return t;
    }

    public IEnumerable<TurretDataSO> AllTurrets()
    {
        foreach (var t in startingTurrets) if (t != null) yield return t;
        foreach (var l in otherLevels) if (l != null) foreach (var t in l.turrets) if (t != null) yield return t;
    }

    public UnlockLevel GetLevel(bool normal, int level)
    {
        var list = normal ? normalLevels : otherLevels;
        foreach (var l in list) if (l != null && l.level == level) return l;
        return null;
    }
}
