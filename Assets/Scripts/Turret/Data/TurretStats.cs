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

    private IRangeDisplay rangeDisplay;        // puede vivir en este GO o en un hijo
    private TurretDataHolder dataHolder;

    public event System.Action OnStatsChanged;

    private void Awake()
    {
        // Buscar en este GO y también en hijos (incluso inactivos en el editor)
        rangeDisplay = GetComponent<IRangeDisplay>();
        if (rangeDisplay == null)
            rangeDisplay = GetComponentInChildren<IRangeDisplay>(true);

        dataHolder = GetComponent<TurretDataHolder>();
    }

    // === Inicialización con SO ===
    public void InitializeFromSO(TurretDataSO so)
    {
        if (so == null) return;

        float mod = (Application.isPlaying && GameModifiersManager.Instance)
            ? GameModifiersManager.Instance.turretDamageMultiplier
            : 1f;

        baseDamage = Mathf.Round(so.damage * mod * 10f) / 10f;
        baseRange = Mathf.Round(so.range * 10f) / 10f;
        baseFireRate = Mathf.Round(so.fireRate * 10f) / 10f;

        RecalculateStats();      // esto llama UpdateRangeVisualizer adentro
        UpdateRangeVisualizer();
    }

    public void ApplyLevelModifiers(float damage, float range, float fireRate)
    {
        baseDamage = damage;
        baseRange = range;
        baseFireRate = fireRate;

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

        currentDamage = Mathf.Round(baseDamage * damageMod * 10f) / 10f;
        currentRange = Mathf.Round(baseRange * rangeMod * 10f) / 10f;
        currentFireRate = Mathf.Round(baseFireRate * fireRateMod * 10f) / 10f;

        OnStatsChanged?.Invoke();
    }

    /// <summary>
    /// Actualiza el visualizador acorde al rango actual.
    /// Si está visible: Show(currentRange).
    /// Si está oculto y es TurretRangeVisualizer: SetRadius(currentRange, false) para refrescarlo sin mostrar.
    /// </summary>
    private void UpdateRangeVisualizer()
    {
        if (rangeDisplay == null) return;

        if (rangeDisplay.IsVisible())
        {
            rangeDisplay.Show(currentRange);
        }
        else
        {
            // Si usás el TurretRangeVisualizer que te pasé, esto refresca "en frío"
            if (rangeDisplay is TurretRangeVisualizer concrete)
            {
                concrete.SetRadius(currentRange, forceRedraw: false);
            }
            // Si NO es ese tipo, lo dejamos así: se actualizará la próxima vez que se haga Show().
        }
    }

    // Helpers para tu lógica de selección/deselección
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
