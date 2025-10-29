using UnityEngine;

[RequireComponent(typeof(TurretStats))]
public class Turret : MonoBehaviour
{
    [SerializeField] private TurretStats stats;
    public TurretStats Stats => stats;

    private TurretDataHolder dataHolder;
    private IShootingBehavior shooter;
    private ITargetingBehavior targeting;
    private IRangeDisplay rangeDisplay;

    [Header("Range Display")]
    [SerializeField] private bool showRangeOnStart = false; // mejor oculto hasta seleccionar

    [Header("Placement Gate")]
    [Tooltip("Se pone en true cuando la torreta quedó colocada en una celda válida.")]
    [SerializeField] private bool isPlaced = false;

    private void Awake()
    {
        if (!stats) stats = GetComponent<TurretStats>();
        dataHolder = GetComponent<TurretDataHolder>();

        // Buscar visualizador también en hijos (incluye inactivos en editor)
        rangeDisplay = GetComponent<IRangeDisplay>();
        if (rangeDisplay == null)
            rangeDisplay = GetComponentInChildren<IRangeDisplay>(true);

        // Behaviors de combate
        shooter = GetComponent<IShootingBehavior>();
        targeting = GetComponent<ITargetingBehavior>();

        string type = (dataHolder && dataHolder.turretDataSO) ? dataHolder.turretDataSO.type : null;
        if (RequiresCombatBehaviors(type))
        {
            if (shooter == null)
                Debug.LogWarning($"[Turret] {name} no tiene IShootingBehavior. No podrá disparar.");
            if (targeting == null)
                Debug.LogWarning($"[Turret] {name} no tiene ITargetingBehavior. No podrá detectar enemigos.");
        }

        // Si todavía no está colocada, bloqueamos combate
        ToggleCombat(isPlaced);
    }

    private void OnEnable()
    {
        if (stats != null)
            stats.OnStatsChanged += HandleStatsChanged;
    }

    private void OnDisable()
    {
        if (stats != null)
            stats.OnStatsChanged -= HandleStatsChanged;
    }

    private void Start()
    {
        // Asegurá que el visualizador tenga el radio actual antes de mostrar/ocultar
        RefreshRangeDisplay(silent: true);

        if (showRangeOnStart) ShowRange();
        else HideRange();
    }

    private void HandleStatsChanged()
    {
        RefreshRangeDisplay(silent: !IsRangeVisible());
    }

    private void RefreshRangeDisplay(bool silent)
    {
        if (rangeDisplay == null || stats == null) return;

        float r = stats.Range;

        if (IsRangeVisible())
        {
            rangeDisplay.Show(r);
        }
        else if (silent)
        {
            if (rangeDisplay is TurretRangeVisualizer viz)
                viz.SetRadius(r, forceRedraw: false);
        }
    }

    public void ShowRange()
    {
        if (rangeDisplay == null || stats == null) return;
        rangeDisplay.Show(stats.Range);
    }

    public void HideRange()
    {
        rangeDisplay?.Hide();
    }

    private bool IsRangeVisible() => rangeDisplay != null && rangeDisplay.IsVisible();

    private static bool RequiresCombatBehaviors(string type)
    {
        if (string.IsNullOrEmpty(type)) return true;
        type = type.ToLowerInvariant();
        return type == "attack" || type == "aoe" || type == "slow";
    }

    // ======== NUEVO: Gate de colocación ========

    /// <summary>Llamar apenas la torreta se coloca sobre una celda válida.</summary>
    public void SetPlaced(bool placed)
    {
        if (isPlaced == placed) return;
        isPlaced = placed;
        ToggleCombat(isPlaced);
    }

    /// <summary>Activa/desactiva componentes de combate/targeting y shooters auxiliares.</summary>
    private void ToggleCombat(bool enabledCombat)
    {
        // Cualquier componente que implemente estas interfaces
        if (targeting is MonoBehaviour mbT) mbT.enabled = enabledCombat;
        if (shooter is MonoBehaviour mbS) mbS.enabled = enabledCombat;

        // Gate para TurretShooter monobehaviour (si existe)
        var turretShooter = GetComponent<TurretShooter>();
        if (turretShooter) turretShooter.SetCombatEnabled(enabledCombat);
    }
}
