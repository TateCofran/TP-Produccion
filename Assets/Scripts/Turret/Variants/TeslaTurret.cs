using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(TurretStats))]
public class TeslaTurret : MonoBehaviour, IShootingBehavior
{
    [Header("Refs")]
    [SerializeField] private Transform firePoint;

    [Header("Raycast")]
    [SerializeField] private LayerMask hittableLayers = ~0;
    [SerializeField] private LayerMask losBlockers = 0;
    [SerializeField] private float sphereCastRadius = 0.18f;
    [SerializeField] private Vector3 targetOffset = new Vector3(0f, 0.35f, 0f);
    [SerializeField] private bool debugDraw = false;

    [Header("Impact VFX")]
    [SerializeField] private GameObject impactVfxPrefab;
    [SerializeField] private float impactVfxOffsetAlongNormal = 0.03f;
    [SerializeField] private bool parentVfxToHit = true;
    [SerializeField] private float impactVfxLifetime = 1.5f;

    [Header("Bolt")]
    [SerializeField] private GameObject boltPrefab;
    [SerializeField] private float boltSpeed = 28f;
    [Tooltip("Scale applied to the bolt when spawned.")]
    [SerializeField] private Vector3 boltSpawnScale = Vector3.one;
    [Tooltip("Maximum bolt lifetime as a safety net.")]
    [SerializeField] private float boltHardLifetime = 2f;

    [Header("Chain")]
    [Tooltip("Maximum number of enemies hit per shot, including the first one.")]
    [SerializeField] private int maxChainTargets = 3;
    [Tooltip("Short visual delay between hops.")]
    [SerializeField] private float chainDelay = 0.04f;
    [Tooltip("Search radius from the current enemy for the next hop. 0 uses the turret range.")]
    [SerializeField] private float perHopSearchRadius = 0f;

    [Header("Impact Sound")]
    [SerializeField] private AudioClip impactClip;
    [Range(0f, 1f)]
    [SerializeField] private float impactVolume = 1f;
    [SerializeField] private bool ensureAudioSource = true;

    private ITurretStats stats;
    private AudioSource audioSource;
    private readonly List<GameObject> activeBolts = new();
    private WaitForSeconds chainWait;

    private void Awake()
    {
        stats = GetComponent<ITurretStats>();
        if (!firePoint) Debug.LogWarning("[TeslaTurret] firePoint is not assigned");

        if (ensureAudioSource)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1f;
                audioSource.rolloffMode = AudioRolloffMode.Linear;
                audioSource.minDistance = 1f;
                audioSource.maxDistance = 20f;
                audioSource.dopplerLevel = 0f;
            }
        }

        chainWait = chainDelay > 0f ? new WaitForSeconds(chainDelay) : null;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        for (int i = 0; i < activeBolts.Count; i++)
            VfxPool.Release(activeBolts[i]);
        activeBolts.Clear();
    }

    public void Shoot(Transform firePointIgnored, Transform target, ITurretStats statsParam)
    {
        if (stats == null || !firePoint) return;

        Enemy first = ResolveInitialTarget(target);
        if (first == null) return;

        if (debugDraw) Debug.DrawLine(firePoint.position, first.transform.position + targetOffset, Color.cyan, 0.1f);

        StartCoroutine(ChainLightning(first));
    }

    private Enemy ResolveInitialTarget(Transform candidate)
    {
        Enemy enemy = null;
        if (candidate != null) TryGetEnemy(candidate, out enemy);

        float range = stats.Range;
        bool valid = enemy != null &&
                     enemy.Health != null &&
                     !enemy.Health.IsDead() &&
                     enemy.IsTargetable() &&
                     (enemy.transform.position - transform.position).sqrMagnitude <= range * range;

        if (valid) return enemy;

        return FindClosest(transform.position, range, null);
    }

    private static Enemy FindClosest(Vector3 origin, float radius, HashSet<Enemy> exclude)
    {
        float r2 = radius * radius;
        Enemy best = null;
        float bestD2 = float.MaxValue;

        var list = EnemyTracker.Enemies;
        for (int i = 0; i < list.Count; i++)
        {
            var e = list[i];
            if (e == null || e.Health == null || e.Health.IsDead() || !e.IsTargetable()) continue;
            if (exclude != null && exclude.Contains(e)) continue;

            float d2 = (e.transform.position - origin).sqrMagnitude;
            if (d2 > r2 || d2 >= bestD2) continue;

            best = e;
            bestD2 = d2;
        }

        return best;
    }

    private IEnumerator ChainLightning(Enemy firstTarget)
    {
        var chainVisited = new HashSet<Enemy>();
        Enemy current = firstTarget;
        int currentSpawnId = current.SpawnId;
        Vector3 segmentOrigin = firePoint.position;

        for (int hop = 0; hop < maxChainTargets; hop++)
        {
            if (current == null || !current.IsSameSpawn(currentSpawnId) || current.Health.IsDead()) break;

            if (!TryRayToEnemy(segmentOrigin, current, out RaycastHit hit, out Vector3 shotDir))
            {
                if (debugDraw) Debug.DrawLine(segmentOrigin, current.transform.position + targetOffset, Color.gray, 0.12f);
                break;
            }

            yield return BoltTravelAndImpact(segmentOrigin, hit, current, currentSpawnId, shotDir);

            chainVisited.Add(current);
            segmentOrigin = hit.point;

            float radius = perHopSearchRadius > 0f ? perHopSearchRadius : stats.Range;
            Vector3 from = current != null ? current.transform.position : hit.point;
            Enemy next = FindClosest(from, radius, chainVisited);
            if (next == null) break;

            if (chainWait != null) yield return chainWait;

            current = next;
            currentSpawnId = next.SpawnId;
        }
    }

    private bool TryRayToEnemy(Vector3 origin, Enemy enemy, out RaycastHit hit, out Vector3 shotDir)
    {
        hit = default;
        shotDir = Vector3.zero;

        Vector3 aim = enemy.transform.position + targetOffset;
        Vector3 dir = aim - origin;
        float dist = dir.magnitude;
        if (dist <= 0.001f) return false;

        Vector3 ndir = dir / dist;
        shotDir = ndir;

        if (losBlockers != 0 &&
            Physics.Raycast(origin, ndir, dist, losBlockers, QueryTriggerInteraction.Ignore))
            return false;

        if (Physics.Raycast(origin, ndir, out hit, dist + 0.25f, hittableLayers, QueryTriggerInteraction.Ignore))
            return true;

        if (sphereCastRadius > 0f &&
            Physics.SphereCast(origin, sphereCastRadius, ndir, out hit, dist + 0.25f, hittableLayers, QueryTriggerInteraction.Ignore))
            return true;

        return false;
    }

    private IEnumerator BoltTravelAndImpact(Vector3 start, RaycastHit hit, Enemy enemyAtShot, int spawnIdAtShot, Vector3 shotDir)
    {
        Vector3 toTarget = hit.point - start;
        float totalDist = toTarget.magnitude;

        if (boltPrefab == null || totalDist <= 0.0001f)
        {
            Vector3 fallback = totalDist > 0.0001f ? toTarget / totalDist : shotDir;
            ResolveImpact(hit, enemyAtShot, spawnIdAtShot, -(hit.normal.sqrMagnitude > 0.0001f ? hit.normal : fallback));
            yield break;
        }

        Vector3 dir = toTarget / totalDist;
        GameObject bolt = VfxPool.Spawn(boltPrefab, start, Quaternion.LookRotation(dir, Vector3.up), boltHardLifetime + 0.5f);
        bolt.transform.localScale = boltSpawnScale;
        activeBolts.Add(bolt);

        float killAt = boltHardLifetime > 0f ? Time.time + boltHardLifetime : float.PositiveInfinity;
        float remaining = totalDist;

        while (remaining > 0f)
        {
            float step = boltSpeed * Time.deltaTime;
            if (step > 0f)
            {
                float advance = Mathf.Min(step, remaining);
                bolt.transform.position += dir * advance;
                remaining -= advance;
            }

            if (Time.time >= killAt) break;
            yield return null;
        }

        activeBolts.Remove(bolt);
        VfxPool.Release(bolt);
        ResolveImpact(hit, enemyAtShot, spawnIdAtShot, -dir);

        if (debugDraw) Debug.DrawLine(start, hit.point, Color.yellow, 0.15f);
    }

    private void ResolveImpact(RaycastHit hit, Enemy enemyAtShot, int spawnIdAtShot, Vector3 fallbackDir)
    {
        SpawnImpactVfx(hit, fallbackDir);

        int damage = Mathf.Max(1, Mathf.RoundToInt(stats.Damage));

        if (enemyAtShot != null && enemyAtShot.IsSameSpawn(spawnIdAtShot) && !enemyAtShot.Health.IsDead())
            enemyAtShot.Health.TakeDamage(damage);

        if (audioSource && impactClip) audioSource.PlayOneShot(impactClip, impactVolume);
    }

    private void SpawnImpactVfx(RaycastHit hit, Vector3 fallbackDir)
    {
        if (!impactVfxPrefab) return;

        Vector3 normal = hit.normal.sqrMagnitude > 0.0001f ? hit.normal : -fallbackDir;
        Vector3 pos = hit.point + normal * impactVfxOffsetAlongNormal;
        Transform follow = parentVfxToHit && hit.collider != null ? hit.collider.transform : null;

        VfxPool.Spawn(impactVfxPrefab, pos, Quaternion.LookRotation(normal), impactVfxLifetime, follow);
    }

    private static bool TryGetEnemy(Transform t, out Enemy enemy)
    {
        enemy = null;
        if (!t) return false;

        enemy = t.GetComponentInParent<Enemy>();
        return enemy != null && enemy.Health != null;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (stats != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, stats.Range);
        }
    }
#endif
}
