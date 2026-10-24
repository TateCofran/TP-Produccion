using UnityEngine;

public class TurretStats : MonoBehaviour, ITurretStats
{
    [Header("Stats")]
    [SerializeField] private float range;
    [SerializeField] private float fireRate;
    [SerializeField] private float damage;
    [SerializeField] private int upgradeLevel = 1;
    [SerializeField] private int maxUpgradeLevel = 15;

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
    public int MaxUpgradeLevel => maxUpgradeLevel;
    public WorldAffinity Affinity => affinity;
    public bool IsInAffinityWorld => affinity != WorldAffinity.Neutral && WorldState.Matches(affinity);
    public float AffinityMultiplier
    {
        get
        {
            if (affinity == WorldAffinity.Neutral) return 1f;
            return WorldState.Matches(affinity) ? 1f + affinityBonus : 1f - offWorldPenalty;
        }
    }

    private IRangeDisplay rangeDisplay;
    private TurretDataHolder dataHolder;

    public event System.Action OnStatsChanged;

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

        baseDamage = so.damage;
        baseRange = so.range;
        baseFireRate = so.fireRate;

        affinity = so.affinity;
        affinityBonus = so.affinityBonus;
        offWorldPenalty = so.offWorldPenalty;

        TurretPermanentUpgrades.ApplyPermanentUpgrades(
            ref baseDamage,
            ref baseRange,
            ref baseFireRate
        );

        RecalculateStats();
        UpdateRangeVisualizer();
    }

    public void SetLevel(int level, float damageMult, float rangeMult, float fireRateMult)
    {
        upgradeLevel = Mathf.Clamp(level, 1, maxUpgradeLevel);
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

        if (GameModifiersManager.Instance != null)
        {
            rangeMod *= GameModifiersManager.Instance.turretRangeMultiplier;
            fireRateMod *= GameModifiersManager.Instance.turretFireRateMultiplier;
            damageMod *= GameModifiersManager.Instance.turretDamageMultiplier;
        }

        currentDamage = Round1(baseDamage * levelDamageMult * damageMod * AffinityMultiplier);
        currentRange = Round1(baseRange * levelRangeMult * rangeMod);
        currentFireRate = Round1(baseFireRate * levelFireRateMult * fireRateMod);

        OnStatsChanged?.Invoke();
    }

    private static float Round1(float v) => Mathf.Round(v * 10f) / 10f;

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
