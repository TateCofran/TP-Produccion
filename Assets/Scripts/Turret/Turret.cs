using System.Collections;
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
    [SerializeField] private bool showRangeOnStart = false;

    [Header("Placement Gate")]
    [SerializeField] private bool isPlaced = false;
    public bool IsPlaced => isPlaced;

    private void Awake()
    {
        if (!stats) stats = GetComponent<TurretStats>();
        dataHolder = GetComponent<TurretDataHolder>();

        rangeDisplay = GetComponent<IRangeDisplay>();
        if (rangeDisplay == null)
            rangeDisplay = GetComponentInChildren<IRangeDisplay>(true);

        shooter = GetComponent<IShootingBehavior>();
        targeting = GetComponent<ITargetingBehavior>();

        string type = (dataHolder && dataHolder.turretDataSO) ? dataHolder.turretDataSO.type : null;
        if (RequiresCombatBehaviors(type))
        {
            if (shooter == null)
                Debug.LogWarning($"[Turret] {name} has no IShootingBehavior and cannot shoot");
            if (targeting == null)
                Debug.LogWarning($"[Turret] {name} has no ITargetingBehavior and cannot detect enemies");
        }

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

    public void SetPlaced(bool placed)
    {
        if (isPlaced == placed) return;
        isPlaced = placed;
        ToggleCombat(isPlaced);
    }

    private void ToggleCombat(bool enabledCombat)
    {
        if (targeting is MonoBehaviour mbT) mbT.enabled = enabledCombat;
        if (shooter is MonoBehaviour mbS) mbS.enabled = enabledCombat;

        var turretShooter = GetComponent<TurretShooter>();
        if (turretShooter) turretShooter.SetCombatEnabled(enabledCombat);
    }

    public void SetCombatEnabled(bool value)
    {
        ToggleCombat(value);
    }

    private Coroutine _disableCo;
    public bool IsTemporarilyDisabled => _disableCo != null;

    public void DisableFor(float seconds)
    {
        if (!isActiveAndEnabled) return;
        if (_disableCo != null) StopCoroutine(_disableCo);
        _disableCo = StartCoroutine(DisableRoutine(seconds));
    }

    private IEnumerator DisableRoutine(float seconds)
    {
        ToggleCombat(false);
        yield return new WaitForSeconds(seconds);
        _disableCo = null;
        ToggleCombat(isPlaced);
    }
}
