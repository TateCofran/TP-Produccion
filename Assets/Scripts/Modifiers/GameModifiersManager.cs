using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameModifiersManager : MonoBehaviour
{
    public static GameModifiersManager Instance;

    [Header("Pools")]
    [SerializeField] private List<ModifierSO> normalPool = new List<ModifierSO>();
    [SerializeField] private List<ModifierSO> otherPool = new List<ModifierSO>();
    [SerializeField] private List<ModifierSO> legendaryNormalPool = new List<ModifierSO>();
    [SerializeField] private List<ModifierSO> legendaryOtherPool = new List<ModifierSO>();

    private readonly Dictionary<ModifierStat, float> stats = new Dictionary<ModifierStat, float>();
    private readonly List<IGameModifier> appliedModifiers = new List<IGameModifier>();
    private readonly Dictionary<System.Type, int> modifierUsageCount = new Dictionary<System.Type, int>();

    private ModifierPanelSelection modifierPanelSelection;

    public event System.Action<ModifierSO> OnModifierApplied;

    public float turretRangeMultiplier => GetMultiplier(ModifierStat.TurretRange);
    public float turretFireRateMultiplier => GetMultiplier(ModifierStat.TurretFireRate);
    public float turretDamageMultiplier => GetMultiplier(ModifierStat.TurretDamage);

    public float enemyCountMultiplier => Mathf.Max(0f, GetMultiplier(ModifierStat.EnemyCount));
    public float enemySpeedMultiplier => Mathf.Max(0f, GetMultiplier(ModifierStat.EnemySpeed));
    public float enemyDamageTakenMultiplier => Mathf.Max(0f, GetMultiplier(ModifierStat.EnemyDamageTaken));

    public float blueEssenceMultiplier => Mathf.Max(0f, GetMultiplier(ModifierStat.BlueEssence));
    public float redEssenceMultiplier => Mathf.Max(0f, GetMultiplier(ModifierStat.RedEssence));

    public IReadOnlyList<ModifierSO> NormalPool => normalPool;
    public IReadOnlyList<ModifierSO> OtherPool => otherPool;
    public IReadOnlyList<ModifierSO> LegendaryNormalPool => legendaryNormalPool;
    public IReadOnlyList<ModifierSO> LegendaryOtherPool => legendaryOtherPool;

    public void InjectModifierPanelSelection(ModifierPanelSelection panel)
    {
        modifierPanelSelection = panel;
    }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        ResetRun();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void ResetRun()
    {
        stats.Clear();
        appliedModifiers.Clear();
        modifierUsageCount.Clear();
    }

    public void AddStat(ModifierStat stat, float value)
    {
        stats.TryGetValue(stat, out float current);
        stats[stat] = current + value;
    }

    public float GetStat(ModifierStat stat)
    {
        return stats.TryGetValue(stat, out float value) ? value : 0f;
    }

    public float GetMultiplier(ModifierStat stat) => 1f + GetStat(stat);

    public static float Stat(ModifierStat stat)
    {
        return Instance != null ? Instance.GetStat(stat) : 0f;
    }

    public static float Multiplier(ModifierStat stat)
    {
        return Instance != null ? Instance.GetMultiplier(stat) : 1f;
    }

    public List<ModifierSO> GetAvailable(ModifierCategory category)
    {
        var pool = PoolFor(category);
        var result = new List<ModifierSO>();
        if (pool == null) return result;

        foreach (var mod in pool)
        {
            if (mod == null) continue;
            if (appliedModifiers.Contains(mod)) continue;
            if (!ProgressionStore.IsUnlocked(mod)) continue;
            if (result.Contains(mod)) continue;
            result.Add(mod);
        }
        return result;
    }

    private List<ModifierSO> PoolFor(ModifierCategory category)
    {
        switch (category)
        {
            case ModifierCategory.OtherWorld: return otherPool;
            case ModifierCategory.LegendaryNormal: return legendaryNormalPool;
            case ModifierCategory.LegendaryOther: return legendaryOtherPool;
            default: return normalPool;
        }
    }

    public static bool IsLegendary(ModifierCategory category)
    {
        return category == ModifierCategory.LegendaryNormal || category == ModifierCategory.LegendaryOther;
    }

    public List<ModifierSO> GetRandomAvailable(ModifierCategory category, int count)
    {
        var pool = GetAvailable(category);
        var selected = new List<ModifierSO>();
        for (int i = 0; i < count && pool.Count > 0; i++)
        {
            int idx = Random.Range(0, pool.Count);
            selected.Add(pool[idx]);
            pool.RemoveAt(idx);
        }
        return selected;
    }

    public static ModifierCategory CategoryForCurrentWorld()
    {
        return CategoryForCurrentWorld(false);
    }

    public static ModifierCategory CategoryForCurrentWorld(bool legendary)
    {
        bool other = WorldState.Current == ShiftingWorldMechanic.World.Other;
        if (legendary) return other ? ModifierCategory.LegendaryOther : ModifierCategory.LegendaryNormal;
        return other ? ModifierCategory.OtherWorld : ModifierCategory.NormalWorld;
    }

    public void ApplyModifier(IGameModifier modifier)
    {
        if (modifier == null) return;

        appliedModifiers.Add(modifier);
        modifier.Apply(this);

        var modType = modifier.GetType();
        modifierUsageCount.TryGetValue(modType, out int count);
        modifierUsageCount[modType] = count + 1;

        var so = modifier as ModifierSO;
        if (so != null)
        {
            Debug.Log($"[GameModifiersManager] Applied modifier {so.id} {so.displayName}");

            if (so.Affects(ModifierStat.TurretDamage) || so.Affects(ModifierStat.TurretFireRate) || so.Affects(ModifierStat.TurretRange) || so.Affects(ModifierStat.Worldless))
                RecalculateAllTurrets();

            if (so.Affects(ModifierStat.UnifiedGround))
            {
                var tileDupes = FindFirstObjectByType<TileDupeSystem>();
                if (tileDupes != null) tileDupes.MergeByEffect();
            }

            if (so.Affects(ModifierStat.CoreMaxHealth) && Core.Active != null)
                Core.Active.ModifyMaxHealth(Mathf.RoundToInt(so.entries.Where(e => e != null && e.stat == ModifierStat.CoreMaxHealth).Sum(e => e.value)));

            OnModifierApplied?.Invoke(so);
        }
    }

    private static void RecalculateAllTurrets()
    {
        var all = FindObjectsByType<TurretStats>(FindObjectsSortMode.None);
        foreach (var turretStats in all)
            turretStats.RecalculateStats();
    }

    public IReadOnlyList<IGameModifier> GetModifiers() => appliedModifiers.AsReadOnly();

    public bool HasModifier<T>() where T : IGameModifier
    {
        return appliedModifiers.Exists(m => m is T);
    }

    public bool IsApplied(ModifierSO modifier) => modifier != null && appliedModifiers.Contains(modifier);

    public int GetModifierStacks(IGameModifier modifier)
    {
        int count = 0;
        foreach (var mod in appliedModifiers)
        {
            if (ReferenceEquals(mod, modifier))
                count++;
        }
        return count;
    }

    public int GetModifierUsageCount<T>() where T : IGameModifier
    {
        var modType = typeof(T);
        return modifierUsageCount.TryGetValue(modType, out int count) ? count : 0;
    }

    public Dictionary<string, int> GetAllModifierUsageCounts()
    {
        return modifierUsageCount.ToDictionary(k => k.Key.Name, v => v.Value);
    }
}
