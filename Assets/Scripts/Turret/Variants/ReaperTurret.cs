using UnityEngine;

[RequireComponent(typeof(TurretDataHolder))]
public class ReaperTurret : MonoBehaviour, IShootingBehavior, ITurretInfoProvider
{
    [Header("Essence Bonus")]
    [Tooltip("Extra essences for kills made by this turret (1 = +100%).")]
    [SerializeField, Min(0f)] private float essenceBonus = 1f;
    [SerializeField, Min(0f)] private float essenceBonusPerLevel = 0.1f;

    [Header("Visual")]
    [SerializeField] private Color shotColor = new Color(0.55f, 0.35f, 0.75f, 1f);
    [SerializeField] private Color scytheColor = new Color(0.75f, 0.6f, 1f, 1f);
    [SerializeField] private Vector3 targetOffset = new Vector3(0f, 0.6f, 0f);

    private TurretStats stats;
    private float bonusMultiplier = 1f;

    public float BonusMultiplier { get => bonusMultiplier; set => bonusMultiplier = Mathf.Max(0f, value); }
    public int Kills { get; private set; }

    private int Level => stats != null ? Mathf.Max(1, stats.UpgradeLevel) : 1;
    public float EssenceMultiplier => 1f + (essenceBonus + essenceBonusPerLevel * (Level - 1)) * bonusMultiplier;

    private void Awake()
    {
        stats = GetComponent<TurretStats>();
    }

    public void Shoot(Transform firePoint, Transform target, ITurretStats turretStats)
    {
        if (target == null || turretStats == null) return;
        var enemy = target.GetComponentInParent<Enemy>();
        if (enemy == null || enemy.Health == null || enemy.Health.IsDead()) return;

        Vector3 hitPoint = target.position + targetOffset;
        if (firePoint != null)
        {
            var feedback = TurretShotFeedback.For(this);
            feedback?.OnShot(firePoint.position);
            FeedbackFxPool.Tracer(firePoint.position, hitPoint, shotColor, 0.04f, 0.06f);
        }

        enemy.LastHitSource = this;
        enemy.Health.TakeDamage(TurretDamageCalculator.Compute(turretStats.Damage, enemy));
        if (enemy.Health.IsDead())
        {
            Kills++;
            PlayScythe(hitPoint);
        }
        else if (enemy.LastHitSource == this)
        {
            enemy.LastHitSource = null;
        }
    }

    private void PlayScythe(Vector3 center)
    {
        const int segments = 6;
        float radius = 0.9f;
        Vector3 prev = center + new Vector3(-radius, 0.6f, 0f);
        for (int i = 1; i <= segments; i++)
        {
            float a = Mathf.Lerp(Mathf.PI, 0f, i / (float)segments);
            Vector3 next = center + new Vector3(Mathf.Cos(a) * radius, 0.6f + Mathf.Sin(a) * radius * 0.8f, 0f);
            FeedbackFxPool.Tracer(prev, next, scytheColor, 0.09f, 0.25f);
            prev = next;
        }
    }

    public string GetInfoLine()
    {
        return $"Kills give +{(EssenceMultiplier - 1f) * 100f:0}% essences";
    }
}
