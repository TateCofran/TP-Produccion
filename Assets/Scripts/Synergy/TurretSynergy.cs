using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class TurretSynergy : MonoBehaviour, ITurretInfoProvider
{
    private readonly List<SynergyRule> active = new();
    private readonly List<Vector3> sourceCells = new();

    public IReadOnlyList<SynergyRule> Active => active;
    public IReadOnlyList<Vector3> SourceCells => sourceCells;
    public bool HasAny => active.Count > 0;

    public event System.Action OnSynergiesChanged;

    public float Get(SynergyStat stat)
    {
        float total = 0f;
        foreach (var r in active)
            if (r.stat == stat) total += r.value;
        return total;
    }

    public void SetActive(IEnumerable<SynergyRule> rules, IEnumerable<Vector3> cells)
    {
        var previous = new List<SynergyRule>(active);
        active.Clear();
        if (rules != null) active.AddRange(rules);
        sourceCells.Clear();
        if (cells != null) sourceCells.AddRange(cells);

        bool changed = previous.Count != active.Count;
        if (!changed)
            for (int i = 0; i < active.Count; i++)
                if (previous[i] != active[i]) { changed = true; break; }

        if (!changed) return;
        Apply();
        OnSynergiesChanged?.Invoke();
    }

    private void Apply()
    {
        var stats = GetComponent<TurretStats>();
        if (stats != null) stats.RecalculateStats();

        var banner = GetComponent<WarBannerAura>();
        if (banner != null) banner.ExtraRadiusCells = Get(SynergyStat.AuraRadius);

        var warden = GetComponent<WardenAura>();
        if (warden != null) warden.RadiusMultiplier = 1f + Get(SynergyStat.ProtectionRadius);

        var rift = GetComponent<RiftCannonTurret>();
        if (rift != null) rift.ChargeRateMultiplier = 1f + Get(SynergyStat.RiftChargeRate);

        var guillotine = GetComponent<GuillotineTurret>();
        if (guillotine != null) guillotine.PercentMultiplier = 1f + Get(SynergyStat.PercentDamage);

        var reaper = GetComponent<ReaperTurret>();
        if (reaper != null) reaper.BonusMultiplier = 1f + Get(SynergyStat.EssenceBonus);
    }

    public string GetInfoLine()
    {
        if (active.Count == 0) return string.Empty;
        var sb = new System.Text.StringBuilder();
        foreach (var r in active)
        {
            if (sb.Length > 0) sb.Append('\n');
            sb.Append($"Synergy ({r.EffectLabel}): {r.BonusLabel}");
        }
        return sb.ToString();
    }
}
