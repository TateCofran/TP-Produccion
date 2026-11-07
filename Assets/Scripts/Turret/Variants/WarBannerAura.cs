using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class WarBannerAura : MonoBehaviour, ITurretInfoProvider
{
    [Header("Buff")]
    [SerializeField, Min(0f)] private float damageBonus = 0.15f;
    [SerializeField, Min(0f)] private float fireRateBonus = 0.10f;
    [SerializeField, Min(0f)] private float damageBonusPerLevel = 0.03f;
    [SerializeField, Min(0f)] private float fireRateBonusPerLevel = 0.02f;

    [Header("Area")]
    [Tooltip("Aura radius in cells. The turret data range overrides it when greater than zero.")]
    [SerializeField, Min(0.1f)] private float radiusInCells = 2.5f;
    [SerializeField, Min(0.1f)] private float cellSize = 1f;
    [SerializeField, Min(0.05f)] private float refreshInterval = 0.5f;

    private readonly HashSet<TurretStats> buffed = new();
    private readonly List<TurretStats> scratch = new();
    private readonly List<Transform> linkTargets = new();
    private Turret turret;
    private TurretStats stats;
    private TurretDataHolder holder;
    private AuraVisual visual;
    private float nextRefresh;
    private float extraRadiusCells;

    public int BuffedCount => buffed.Count;
    public float ExtraRadiusCells { get => extraRadiusCells; set => extraRadiusCells = Mathf.Max(0f, value); }

    public float Radius
    {
        get
        {
            float cells = holder != null && holder.turretDataSO != null && holder.turretDataSO.range > 0f ? holder.turretDataSO.range : radiusInCells;
            return (cells + extraRadiusCells) * cellSize;
        }
    }

    public int Level => stats != null ? Mathf.Max(1, stats.UpgradeLevel) : 1;
    public float DamageMultiplier => 1f + damageBonus + damageBonusPerLevel * (Level - 1);
    public float FireRateMultiplier => 1f + fireRateBonus + fireRateBonusPerLevel * (Level - 1);

    private void Awake()
    {
        turret = GetComponent<Turret>();
        stats = GetComponent<TurretStats>();
        holder = GetComponent<TurretDataHolder>();
        visual = GetComponent<AuraVisual>();
        if (visual == null) visual = gameObject.AddComponent<AuraVisual>();
        visual.SetColor(new Color(1f, 0.75f, 0.25f, 0.6f));
    }

    private void OnDisable()
    {
        ClearAll();
    }

    private void OnDestroy()
    {
        ClearAll();
    }

    private void Update()
    {
        if (turret != null && !turret.IsPlaced) return;
        if (Time.time < nextRefresh) return;
        nextRefresh = Time.time + refreshInterval;
        Refresh();
    }

    public void Refresh()
    {
        float r = Radius;
        float r2 = r * r;
        Vector3 c = transform.position;
        float dmg = DamageMultiplier;
        float fr = FireRateMultiplier;

        scratch.Clear();
        foreach (var t in FindObjectsByType<Turret>(FindObjectsSortMode.None))
        {
            if (t == null || t == turret || !t.IsPlaced) continue;
            if (t.GetComponent<WarBannerAura>() != null) continue;
            Vector3 d = t.transform.position - c;
            d.y = 0f;
            if (d.sqrMagnitude > r2) continue;
            var s = t.Stats;
            if (s != null) scratch.Add(s);
        }

        var toRemove = new List<TurretStats>();
        foreach (var s in buffed) if (s == null || !scratch.Contains(s)) toRemove.Add(s);
        foreach (var s in toRemove)
        {
            if (s != null) s.RemoveExternalBuff(this);
            buffed.Remove(s);
        }

        linkTargets.Clear();
        foreach (var s in scratch)
        {
            s.AddExternalBuff(this, dmg, fr);
            buffed.Add(s);
            linkTargets.Add(s.transform);
        }

        if (visual != null)
        {
            visual.SetRadius(r);
            visual.SetLinks(linkTargets);
        }
    }

    private void ClearAll()
    {
        foreach (var s in buffed)
            if (s != null) s.RemoveExternalBuff(this);
        buffed.Clear();
    }

    public string GetInfoLine()
    {
        return $"Aura: +{(DamageMultiplier - 1f) * 100f:0}% damage, +{(FireRateMultiplier - 1f) * 100f:0}% fire rate ({buffed.Count} turrets)";
    }
}
