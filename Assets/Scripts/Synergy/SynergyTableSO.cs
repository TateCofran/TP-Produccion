using System.Collections.Generic;
using UnityEngine;

public enum TileEffectKind
{
    Dps,
    Slow,
    Stun,
    Exposure,
    AntiHeal,
    WorldLockedSameWorld,
    Any
}

public enum SynergyStat
{
    FireRate,
    Damage,
    BurnDuration,
    DamageVsStunned,
    DamageVsSlowed,
    EnergyOutput,
    AoeRadius,
    RiftChargeRate,
    AuraRadius,
    PercentDamage,
    EssenceBonus,
    ProtectionRadius
}

[System.Serializable]
public class SynergyRule
{
    public TurretType turret;
    public TileEffectKind requiredEffect;
    [Tooltip("Optional second accepted effect (Mortar: Slow or Stun). Use Any to ignore.")]
    public TileEffectKind alternativeEffect = TileEffectKind.Any;
    public bool useAlternative;
    public SynergyStat stat;
    public float value;

    public bool Accepts(TileEffectKind kind)
    {
        if (requiredEffect == TileEffectKind.Any) return kind != TileEffectKind.WorldLockedSameWorld;
        if (kind == requiredEffect) return true;
        return useAlternative && kind == alternativeEffect;
    }

    public string EffectLabel
    {
        get
        {
            string a = Label(requiredEffect);
            return useAlternative ? $"{a} or {Label(alternativeEffect)}" : a;
        }
    }

    public string BonusLabel
    {
        get
        {
            switch (stat)
            {
                case SynergyStat.FireRate: return $"+{value * 100f:0}% fire rate";
                case SynergyStat.Damage: return $"+{value * 100f:0}% damage";
                case SynergyStat.BurnDuration: return $"burn lasts +{value * 100f:0}%";
                case SynergyStat.DamageVsStunned: return $"+{value * 100f:0}% damage vs stunned";
                case SynergyStat.DamageVsSlowed: return $"+{value * 100f:0}% damage vs slowed";
                case SynergyStat.EnergyOutput: return $"+{value * 100f:0}% energy";
                case SynergyStat.AoeRadius: return $"+{value * 100f:0}% explosion radius";
                case SynergyStat.RiftChargeRate: return $"charge x{1f + value:0.#} faster";
                case SynergyStat.AuraRadius: return $"+{value:0.#} cell aura radius";
                case SynergyStat.PercentDamage: return $"percent damage x{1f + value:0.#}";
                case SynergyStat.EssenceBonus: return $"+{value * 100f:0}% essence bonus";
                case SynergyStat.ProtectionRadius: return $"+{value * 100f:0}% protection radius";
                default: return stat.ToString();
            }
        }
    }

    public static string Label(TileEffectKind kind)
    {
        switch (kind)
        {
            case TileEffectKind.Dps: return "DPS";
            case TileEffectKind.AntiHeal: return "Anti-heal";
            case TileEffectKind.WorldLockedSameWorld: return "World-locked (same world)";
            case TileEffectKind.Any: return "any effect";
            default: return kind.ToString();
        }
    }
}

[CreateAssetMenu(menuName = "Synergy/Table", fileName = "SynergyTable")]
public class SynergyTableSO : ScriptableObject
{
    public List<SynergyRule> rules = new List<SynergyRule>();

    public IEnumerable<SynergyRule> RulesFor(TurretType type)
    {
        foreach (var r in rules)
            if (r != null && r.turret == type) yield return r;
    }

    public string DescribeFor(TurretType type)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var r in RulesFor(type))
        {
            if (sb.Length > 0) sb.Append('\n');
            sb.Append($"Synergy: next to {r.EffectLabel} -> {r.BonusLabel}");
        }
        return sb.ToString();
    }
}
