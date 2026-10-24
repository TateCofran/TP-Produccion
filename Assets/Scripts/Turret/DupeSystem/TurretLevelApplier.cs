using UnityEngine;

[RequireComponent(typeof(TurretStats))]
public class TurretLevelApplier : MonoBehaviour
{
    [SerializeField] private TurretDataSO turretData;
    [SerializeField] private TurretLevelData levelData;

    private TurretStats turretStats;
    private ITurretDupeSystem dupeSystem;
    private bool subscribed;

    private void Awake()
    {
        ResolveDependencies();
    }

    private void OnEnable()
    {
        ResolveDependencies();
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void ResolveDependencies()
    {
        if (turretStats == null) turretStats = GetComponent<TurretStats>();
        if (dupeSystem == null) dupeSystem = FindFirstObjectByType<TurretDupeSystem>();
    }

    private void Subscribe()
    {
        if (subscribed || dupeSystem == null) return;
        dupeSystem.OnTurretLevelUp += HandleTurretLevelUp;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed || dupeSystem == null) return;
        dupeSystem.OnTurretLevelUp -= HandleTurretLevelUp;
        subscribed = false;
    }

    public void Initialize(TurretDataSO data)
    {
        ResolveDependencies();
        if (isActiveAndEnabled) Subscribe();

        turretData = data;

        if (dupeSystem != null)
        {
            levelData = dupeSystem.GetTurretLevelData(data);
            ApplyLevelBonuses();
        }
    }

    private void HandleTurretLevelUp(TurretDataSO so, TurretLevelData data)
    {
        if (so == null || so != turretData) return;
        levelData = data;
        ApplyLevelBonuses();
    }

    private void ApplyLevelBonuses()
    {
        if (turretStats == null || levelData == null) return;

        turretStats.SetLevel(
            levelData.currentLevel,
            levelData.damageMultiplier,
            levelData.rangeMultiplier,
            levelData.fireRateMultiplier);
    }

    public string GetLevelStatus()
    {
        return levelData?.GetStatusText() ?? "Lv 1";
    }
}
