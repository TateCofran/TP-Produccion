using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(TurretStats))]
public class FireTurret : MonoBehaviour, IShootingBehavior
{
    [Header("Refs")]
    [SerializeField] private Transform firePoint;

    [Header("Raycast")]
    [SerializeField] private LayerMask hittableLayers = ~0;
    [SerializeField] private LayerMask losBlockers = 0;
    [SerializeField] private float sphereCastRadius = 0.12f;
    [SerializeField] private Vector3 targetOffset = new Vector3(0f, 0.35f, 0f);
    [SerializeField] private bool debugDraw;

    [Header("Impact VFX")]
    [SerializeField] private GameObject impactVfxPrefab;
    [SerializeField] private float impactVfxOffsetAlongNormal = 0.03f;
    [SerializeField] private bool parentVfxToHit = true;
    [SerializeField] private float impactVfxLifetime = 1.5f;

    [Header("Fireball")]
    [SerializeField] private GameObject fireballPrefab;
    [SerializeField] private float fireballSpeed = 20f;
    [Tooltip("Maximum fireball lifetime as a safety net.")]
    [SerializeField] private float fireballHardLifetime = 3f;
    [Tooltip("Scale applied to the fireball when spawned.")]
    [SerializeField] private Vector3 fireballSpawnScale = Vector3.one;

    [Header("Burn Damage")]
    [SerializeField] private float tickRate = 1f;
    [SerializeField] private float damageFractionPerSecond = 1f / 2f;
    [Tooltip("Spawn the impact VFX on every burn tick.")]
    [SerializeField] private bool vfxOnEachTick = false;

    [Header("Impact Sound")]
    [SerializeField] private AudioClip impactClip;
    [Range(0f, 1f)]
    [SerializeField] private float impactVolume = 1f;
    [SerializeField] private bool ensureAudioSource = true;

    private ITurretStats stats;
    private AudioSource audioSource;
    private readonly List<GameObject> activeFireballs = new();

    private float tickCountdown;
    private Enemy burningTarget;
    private int burningSpawnId;
    private float totalDamageApplied;

    private void Awake()
    {
        stats = GetComponent<ITurretStats>();
        if (!firePoint) Debug.LogWarning("[FireTurret] firePoint is not assigned");
        tickCountdown = 0f;

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
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        for (int i = 0; i < activeFireballs.Count; i++)
            VfxPool.Release(activeFireballs[i]);
        activeFireballs.Clear();
    }

    private void Update()
    {
        if (burningTarget == null) return;

        tickCountdown -= Time.deltaTime;
        if (tickCountdown <= 0f)
        {
            ApplyBurnDamage();
            tickCountdown = 1f / Mathf.Max(0.01f, tickRate);
        }
    }

    public void Shoot(Transform firePointIgnored, Transform target, ITurretStats statsParam)
    {
        if (!firePoint || target == null) return;

        ITurretStats effectiveStats = statsParam ?? stats;
        if (effectiveStats == null) return;

        Vector3 origin = firePoint.position;
        Vector3 aim = target.position + targetOffset;
        Vector3 dir = aim - origin;
        float dist = dir.magnitude;
        if (dist <= 0.001f) return;

        float maxRange = effectiveStats.Range;
        Vector3 ndir = dir / dist;

        if (dist > maxRange)
        {
            if (debugDraw) Debug.DrawLine(origin, origin + ndir * maxRange, Color.yellow, 0.1f);
            return;
        }

        if (losBlockers != 0 &&
            Physics.Raycast(origin, ndir, dist, losBlockers, QueryTriggerInteraction.Ignore))
        {
            if (debugDraw) Debug.DrawLine(origin, aim, Color.gray, 0.1f);
            return;
        }

        float maxRayDist = Mathf.Min(dist + 0.25f, maxRange);

        if (Physics.Raycast(origin, ndir, out RaycastHit hit, maxRayDist, hittableLayers, QueryTriggerInteraction.Ignore))
        {
            HandleAcquiredHit(origin, ndir, hit);
            return;
        }

        if (sphereCastRadius > 0f &&
            Physics.SphereCast(origin, sphereCastRadius, ndir, out RaycastHit sphereHit, maxRayDist, hittableLayers, QueryTriggerInteraction.Ignore))
        {
            HandleAcquiredHit(origin, ndir, sphereHit);
        }
    }

    private void HandleAcquiredHit(Vector3 origin, Vector3 shotDir, RaycastHit hit)
    {
        TryGetEnemy(hit.collider.transform, out Enemy enemy);
        int spawnId = enemy != null ? enemy.SpawnId : 0;

        if (fireballPrefab != null)
        {
            StartCoroutine(FireballTravel(origin, hit, enemy, spawnId));
        }
        else
        {
            SpawnImpactVfx(hit, shotDir);
            if (enemy != null) StartOrRefreshBurn(enemy, spawnId);
        }

        if (debugDraw) Debug.DrawLine(origin, hit.point, Color.red, 0.15f);
    }

    private IEnumerator FireballTravel(Vector3 start, RaycastHit hit, Enemy enemyAtShot, int spawnIdAtShot)
    {
        Vector3 toTarget = hit.point - start;
        float totalDist = toTarget.magnitude;

        if (totalDist <= 0.0001f)
        {
            ResolveImpact(hit, enemyAtShot, spawnIdAtShot, Vector3.forward);
            yield break;
        }

        Vector3 dir = toTarget / totalDist;
        GameObject fireball = VfxPool.Spawn(fireballPrefab, start, Quaternion.LookRotation(dir, Vector3.up), fireballHardLifetime + 0.5f);
        fireball.transform.localScale = fireballSpawnScale;
        activeFireballs.Add(fireball);

        float killAt = fireballHardLifetime > 0f ? Time.time + fireballHardLifetime : float.PositiveInfinity;
        float remaining = totalDist;

        while (remaining > 0f)
        {
            float step = fireballSpeed * Time.deltaTime;
            if (step > 0f)
            {
                float advance = Mathf.Min(step, remaining);
                fireball.transform.position += dir * advance;
                remaining -= advance;
            }

            if (Time.time >= killAt) break;
            yield return null;
        }

        activeFireballs.Remove(fireball);
        VfxPool.Release(fireball);
        ResolveImpact(hit, enemyAtShot, spawnIdAtShot, -dir);
    }

    private void ResolveImpact(RaycastHit hit, Enemy enemyAtShot, int spawnIdAtShot, Vector3 fallbackDir)
    {
        SpawnImpactVfx(hit, fallbackDir);

        if (enemyAtShot != null && enemyAtShot.IsSameSpawn(spawnIdAtShot))
            StartOrRefreshBurn(enemyAtShot, spawnIdAtShot);
    }

    private void StartOrRefreshBurn(Enemy enemy, int spawnId)
    {
        if (enemy == null || enemy.Health == null || enemy.Health.IsDead()) return;
        if (burningTarget == enemy && burningSpawnId == spawnId) return;

        burningTarget = enemy;
        burningSpawnId = spawnId;
        totalDamageApplied = 0f;
        tickCountdown = 0f;
    }

    private void ClearBurn()
    {
        burningTarget = null;
        totalDamageApplied = 0f;
    }

    private void ApplyBurnDamage()
    {
        if (burningTarget == null || burningTarget.Health == null) return;

        if (burningTarget.Health.IsDead() || !burningTarget.IsSameSpawn(burningSpawnId))
        {
            ClearBurn();
            return;
        }

        if (totalDamageApplied >= stats.Damage)
        {
            ClearBurn();
            return;
        }

        float remaining = stats.Damage - totalDamageApplied;
        float dps = Mathf.Max(0f, stats.Damage * damageFractionPerSecond);
        float damageThisTick = Mathf.Min(dps, remaining);

        int damage = Mathf.Max(1, Mathf.RoundToInt(damageThisTick));
        float before = burningTarget.Health.GetCurrentHealth();
        burningTarget.Health.TakeDamage(damage);
        float after = burningTarget.Health.GetCurrentHealth();

        totalDamageApplied += Mathf.Max(0f, before - after);

        if (vfxOnEachTick && impactVfxPrefab)
        {
            Vector3 pos = burningTarget.transform.position + Vector3.up * 0.35f;
            Transform follow = parentVfxToHit ? burningTarget.transform : null;
            VfxPool.Spawn(impactVfxPrefab, pos, Quaternion.identity, impactVfxLifetime, follow);
        }
    }

    private void SpawnImpactVfx(RaycastHit hit, Vector3 fallbackDir)
    {
        if (impactVfxPrefab)
        {
            Vector3 normal = hit.normal.sqrMagnitude > 0.0001f ? hit.normal : -fallbackDir;
            Vector3 pos = hit.point + normal * impactVfxOffsetAlongNormal;
            Transform follow = parentVfxToHit && hit.collider != null ? hit.collider.transform : null;
            VfxPool.Spawn(impactVfxPrefab, pos, Quaternion.LookRotation(normal), impactVfxLifetime, follow);
        }

        if (audioSource && impactClip) audioSource.PlayOneShot(impactClip, impactVolume);
    }

    private static bool TryGetEnemy(Transform t, out Enemy enemy)
    {
        enemy = null;
        if (!t) return false;

        enemy = t.GetComponentInParent<Enemy>();
        return enemy != null && enemy.Health != null;
    }
}
