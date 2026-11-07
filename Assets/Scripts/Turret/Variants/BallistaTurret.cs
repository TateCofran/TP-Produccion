using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(TurretDataHolder))]
public class BallistaTurret : MonoBehaviour, IShootingBehavior
{
    [Header("Pierce")]
    [Tooltip("Half width of the bolt line. Enemies closer than this to the line are hit.")]
    [SerializeField, Min(0.05f)] private float pierceWidth = 0.45f;
    [Tooltip("Damage lost for each enemy already pierced.")]
    [SerializeField, Range(0f, 1f)] private float falloffPerEnemy = 0.1f;
    [SerializeField, Range(0f, 1f)] private float minDamageFactor = 0.5f;

    private readonly List<(float distance, Enemy enemy)> hits = new();

    public int LastHitCount { get; private set; }

    public void Shoot(Transform firePoint, Transform target, ITurretStats stats)
    {
        if (firePoint == null || target == null || stats == null) return;

        Vector3 origin = firePoint.position;
        Vector3 dir = target.position - origin;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;
        dir.Normalize();

        float length = stats.Range;
        hits.Clear();

        var list = EnemyTracker.Enemies;
        for (int i = 0; i < list.Count; i++)
        {
            var e = list[i];
            if (e == null || e.Health == null || e.Health.IsDead() || !e.IsTargetable()) continue;

            Vector3 v = e.transform.position - origin;
            v.y = 0f;
            float t = Vector3.Dot(v, dir);
            if (t < 0f || t > length) continue;
            if ((v - dir * t).sqrMagnitude > pierceWidth * pierceWidth) continue;
            hits.Add((t, e));
        }

        hits.Sort((a, b) => a.distance.CompareTo(b.distance));
        for (int i = 0; i < hits.Count; i++)
        {
            float factor = Mathf.Max(minDamageFactor, 1f - falloffPerEnemy * i);
            var e = hits[i].enemy;
            if (e == null || e.Health == null || e.Health.IsDead()) continue;
            e.Health.TakeDamage(TurretDamageCalculator.Compute(stats.Damage * factor, e));
        }
        LastHitCount = hits.Count;

        var feedback = TurretShotFeedback.For(this);
        if (feedback != null)
        {
            feedback.OnShot(origin);
            Vector3 end = origin + dir * length;
            end.y = origin.y;
            feedback.Tracer(origin, end);
        }
    }
}
