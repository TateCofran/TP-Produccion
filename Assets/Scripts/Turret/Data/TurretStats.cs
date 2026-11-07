using UnityEngine;

public class TurretStats : MonoBehaviour, ITurretStats
{
    [Header("Stats")]
    [SerializeField] private float range;
    [SerializeField] private float fireRate;
    [SerializeField] private float damage;
    [SerializeField] private int upgradeLevel = 1;

    private float baseDamage;
    private float baseRange;
    private float baseFireRate;

    private float levelDamageMult = 1f;
    private float levelRangeMult = 1f;
    private float levelFireRateMult = 1f;

    private WorldAffinity affinity = WorldAffinity.Neutral;
    private float affinityBonus;
    private float offWorldPenalty;

    [Header("Actual Stats")]
    [SerializeField] private float currentDamage;
    [SerializeField] private float currentRange;
    [SerializeField] private float currentFireRate;

    [Header("Level Modifiers")]
    [SerializeField] private float levelDamageModifier = 1f;
    [SerializeField] private float levelRangeModifier = 1f;
    [SerializeField] private float levelFireRateModifier = 1f;

    public float Damage => currentDamage;
    public float Range => currentRange;
    public float FireRate => currentFireRate;
    public int UpgradeLevel => upgradeLevel;
    public WorldAffinity Affinity => affinity;
    public TurretDataSO Source { get; private set; }
    public bool IsInAffinityWorld => affinity != WorldAffinity.Neutral && WorldState.Matches(affinity);
    public float AffinityMultiplier
    {
        get
        {
            if (affinity == WorldAffinity.Neutral) return 1f;
            if (GameModifiersManager.Stat(ModifierStat.Worldless) > 0f) return 1f + affinityBonus;
            return WorldState.Matches(affinity) ? 1f + affinityBonus : 1f - offWorldPenalty;
        }
    }

    private IRangeDisplay rangeDisplay;
    private TurretDataHolder dataHolder;

    public event System.Action OnStatsChanged;

    private readonly System.Collections.Generic.Dictionary<object, (float damage, float fireRate)> externalBuffs = new();
    private float externalDamageMult = 1f;
    private float externalFireRateMult = 1f;

    public float ExternalDamageMultiplier => externalDamageMult;
    public float ExternalFireRateMultiplier => externalFireRateMult;
    public bool HasExternalBuff => externalBuffs.Count > 0;

    public void AddExternalBuff(object source, float damageMult, float fireRateMult)
    {
        if (source == null) return;
        if (externalBuffs.TryGetValue(source, out var existing) && Mathf.Approximately(existing.damage, damageMult) && Mathf.Approximately(existing.fireRate, fireRateMult)) return;
        externalBuffs[source] = (damageMult, fireRateMult);
        RefreshExternalBuffs();
    }

    public void RemoveExternalBuff(object source)
    {
        if (source == null || !externalBuffs.Remove(source)) return;
        RefreshExternalBuffs();
    }

    private void RefreshExternalBuffs()
    {
        float bestDamage = 1f;
        float bestFireRate = 1f;
        foreach (var buff in externalBuffs.Values)
        {
            if (buff.damage > bestDamage || (Mathf.Approximately(buff.damage, bestDamage) && buff.fireRate > bestFireRate))
            {
                bestDamage = buff.damage;
                bestFireRate = buff.fireRate;
            }
        }
        externalDamageMult = bestDamage;
        externalFireRateMult = bestFireRate;
        RecalculateStats();
    }

    private void Awake()
    {
        rangeDisplay = GetComponent<IRangeDisplay>();
        if (rangeDisplay == null)
            rangeDisplay = GetComponentInChildren<IRangeDisplay>(true);

        dataHolder = GetComponent<TurretDataHolder>();
    }

    private void OnEnable()
    {
        WorldState.OnWorldChanged += HandleWorldChanged;
    }

    private void OnDisable()
    {
        WorldState.OnWorldChanged -= HandleWorldChanged;
    }

    private void HandleWorldChanged(ShiftingWorldMechanic.World world)
    {
        if (affinity != WorldAffinity.Neutral)
            RecalculateStats();
    }

    public void InitializeFromSO(TurretDataSO so)
    {
        if (so == null) return;
        Source = so;

        baseDamage = so.damage;
        baseRange = so.range;
        baseFireRate = so.fireRate;

        affinity = so.affinity;
        affinityBonus = so.affinityBonus;
        offWorldPenalty = so.offWorldPenalty;

        RecalculateStats();
        UpdateRangeVisualizer();
    }

    public void SetLevel(int level, float damageMult, float rangeMult, float fireRateMult)
    {
        upgradeLevel = Mathf.Max(1, level);
        levelDamageMult = damageMult;
        levelRangeMult = rangeMult;
        levelFireRateMult = fireRateMult;

        RecalculateStats();
        UpdateRangeVisualizer();
    }

    public void RecalculateStats()
    {
        float rangeMod = 1f;
        float fireRateMod = 1f;
        float damageMod = 1f;

        var synergy = GetComponent<TurretSynergy>();
        if (synergy != null)
        {
            damageMod *= 1f + synergy.Get(SynergyStat.Damage);
            fireRateMod *= 1f + synergy.Get(SynergyStat.FireRate);
        }

        if (GameModifiersManager.Instance != null)
        {
            rangeMod *= GameModifiersManager.Instance.turretRangeMultiplier;
            fireRateMod *= GameModifiersManager.Instance.turretFireRateMultiplier;
            damageMod *= GameModifiersManager.Instance.turretDamageMultiplier;
        }

        currentDamage = Round1(baseDamage * levelDamageMult * damageMod * AffinityMultiplier * externalDamageMult);
        currentRange = Round1(baseRange * levelRangeMult * rangeMod);
        currentFireRate = Round2(baseFireRate * levelFireRateMult * fireRateMod * externalFireRateMult);

        OnStatsChanged?.Invoke();
    }

    private static float Round1(float v) => Mathf.Round(v * 10f) / 10f;
    private static float Round2(float v) => Mathf.Round(v * 100f) / 100f;

    private void UpdateRangeVisualizer()
    {
        if (rangeDisplay == null) return;

        if (rangeDisplay.IsVisible())
        {
            rangeDisplay.Show(currentRange);
        }
        else
        {
            if (rangeDisplay is TurretRangeVisualizer concrete)
            {
                concrete.SetRadius(currentRange, forceRedraw: false);
            }
        }
    }

    public void ShowRange()
    {
        if (rangeDisplay == null) return;
        rangeDisplay.Show(currentRange);
    }

    public void HideRange()
    {
        if (rangeDisplay == null) return;
        rangeDisplay.Hide();
    }
}
