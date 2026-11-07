using UnityEngine;

public class TurretTargeting : MonoBehaviour, ITargetingBehavior
{
    public enum TargetingMode { Closest, Farthest, HighestHealth, LowestHealth }
    public TargetingMode mode = TargetingMode.Closest;

    private ITurretStats stats;
    private TurretShooter shooter;

    void Awake()
    {
        stats = GetComponent<ITurretStats>();
        shooter = GetComponent<TurretShooter>();
    }

    void Update()
    {
        if (stats == null || shooter == null) return;

        shooter.SetTarget(GetTarget(transform.position, stats.Range));
    }

    public void NextMode()
    {
        mode = (TargetingMode)(((int)mode + 1) % System.Enum.GetValues(typeof(TargetingMode)).Length);
    }

    protected virtual bool IsValidTarget(Enemy enemy, float sqrDistance)
    {
        return true;
    }

    protected virtual float PriorityScore(Enemy enemy)
    {
        return 0f;
    }

    public Transform GetTarget(Vector3 turretPosition, float range)
    {
        float r2 = range * range;
        Enemy best = null;
        float bestScore = 0f;

        var list = EnemyTracker.Enemies;
        for (int i = 0; i < list.Count; i++)
        {
            var e = list[i];
            if (e == null || e.Health == null || e.Health.IsDead() || !e.IsTargetable()) continue;

            float d2 = (e.transform.position - turretPosition).sqrMagnitude;
            if (d2 > r2 || !IsValidTarget(e, d2)) continue;

            float score = mode switch
            {
                TargetingMode.Closest => -d2,
                TargetingMode.Farthest => d2,
                TargetingMode.HighestHealth => e.Health.GetCurrentHealth(),
                TargetingMode.LowestHealth => -e.Health.GetCurrentHealth(),
                _ => 0f
            };
            score += PriorityScore(e) * 1000000f;

            if (best == null || score > bestScore)
            {
                best = e;
                bestScore = score;
            }
        }

        return best != null ? best.transform : null;
    }
}
