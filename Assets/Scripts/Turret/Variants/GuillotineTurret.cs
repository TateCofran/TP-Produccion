using UnityEngine;

[RequireComponent(typeof(TurretDataHolder))]
public class GuillotineTurret : MonoBehaviour, IShootingBehavior, ITurretInfoProvider
{
    [Header("Percent Damage")]
    [Tooltip("Percent of max health per hit against bosses and mini bosses. The normal percent is the turret data damage.")]
    [SerializeField, Min(0f)] private float bossPercent = 4f;
    [Tooltip("Percent added per turret level.")]
    [SerializeField, Min(0f)] private float percentPerLevel = 0.5f;

    [Header("Blade")]
    [SerializeField] private Color bladeColor = new Color(0.85f, 0.9f, 1f, 1f);
    [SerializeField, Min(0.1f)] private float bladeHeight = 3f;

    private TurretDataHolder holder;
    private TurretStats stats;
    private float percentMultiplier = 1f;

    public float PercentMultiplier { get => percentMultiplier; set => percentMultiplier = Mathf.Max(0f, value); }
    public float LastDamage { get; private set; }

    private void Awake()
    {
        holder = GetComponent<TurretDataHolder>();
        stats = GetComponent<TurretStats>();
    }

    private int Level => stats != null ? Mathf.Max(1, stats.UpgradeLevel) : 1;

    public float NormalPercent => ((holder != null && holder.turretDataSO != null ? holder.turretDataSO.damage : 8f) + percentPerLevel * (Level - 1)) * percentMultiplier;
    public float BossPercent => (bossPercent + percentPerLevel * (Level - 1)) * percentMultiplier;

    public void Shoot(Transform firePoint, Transform target, ITurretStats turretStats)
    {
        if (target == null) return;
        var enemy = target.GetComponentInParent<Enemy>();
        if (enemy == null || enemy.Health == null || enemy.Health.IsDead()) return;

        float percent = TurretDamageCalculator.IsBossType(enemy.Type) ? BossPercent : NormalPercent;
        float raw = enemy.Health.GetMaxHealth() * percent / 100f;
        LastDamage = TurretDamageCalculator.Compute(raw, enemy);
        enemy.Health.TakeDamage(LastDamage);

        Vector3 p = target.position;
        FeedbackFxPool.Tracer(p + Vector3.up * bladeHeight, p + Vector3.up * 0.2f, bladeColor, 0.35f, 0.12f);
        FeedbackFxPool.MuzzleFlash(p + Vector3.up * 0.5f, bladeColor, 1.2f, 0.12f);
        if (firePoint != null) TurretShotFeedback.For(this)?.OnShot(firePoint.position);
    }

    public string GetInfoLine()
    {
        return $"Cut: {NormalPercent:0.#}% max health ({BossPercent:0.#}% vs bosses)";
    }
}
