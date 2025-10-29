using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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

    [Header("Bolt (proyectil visual)")]
    [SerializeField] private GameObject boltPrefab;
    [SerializeField] private float boltSpeed = 28f;
    [Tooltip("Parent opcional para los bolts.")]
    [SerializeField] private Transform boltParent;
    [Tooltip("Escala al instanciar (opcional).")]
    [SerializeField] private Vector3 boltSpawnScale = Vector3.one;
    [Tooltip("Vida máxima por seguridad (además del impacto).")]
    [SerializeField] private float boltHardLifetime = 2f;

    [Header("Cadena")]
    [Tooltip("Cantidad máxima de enemigos a golpear por disparo (incluye el primero).")]
    [SerializeField] private int maxChainTargets = 3;
    [Tooltip("Pausa breve entre saltos (solo visual).")]
    [SerializeField] private float chainDelay = 0.04f;
    [Tooltip("Al buscar el siguiente objetivo, radio desde el enemigo actual (si 0 usa stats.Range).")]
    [SerializeField] private float perHopSearchRadius = 0f;

    private ITurretStats stats;

    [Header("Impact Sound")]
    [SerializeField] private AudioClip impactClip; // suena en cada impacto
    [Range(0f, 1f)]
    [SerializeField] private float impactVolume = 1f;
    [SerializeField] private bool ensureAudioSource = true;

    private AudioSource _audioSource;
    private readonly List<Coroutine> _runningChains = new();

    private void Awake()
    {
        stats = GetComponent<ITurretStats>();
        if (!firePoint) Debug.LogWarning("[TeslaTurret] Falta asignar firePoint.");

        if (ensureAudioSource)
        {
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
                _audioSource.spatialBlend = 1f;
                _audioSource.rolloffMode = AudioRolloffMode.Linear;
                _audioSource.minDistance = 1f;
                _audioSource.maxDistance = 20f;
                _audioSource.dopplerLevel = 0f;
            }
        }
    }

    /// <summary>
    /// Dispara una cadena: primer objetivo = target (si es válido), si no el más cercano en rango.
    /// Luego salta al enemigo vivo más cercano no visitado, hasta maxChainTargets.
    /// </summary>
    public void Shoot(Transform firePointIgnored, Transform target, ITurretStats statsParam)
    {
        if (stats == null || !firePoint) return;

        // 1) Determinar primer objetivo
        Enemy first = ResolveInitialTarget(target);

        if (first == null) return;
        if (debugDraw) Debug.DrawLine(firePoint.position, first.transform.position + targetOffset, Color.cyan, 0.1f);

        // 2) Lanzar la cadena
        var chain = StartCoroutine(ChainLightning(first));
        _runningChains.Add(chain);
        // Limpieza de lista (no crítica)
        _runningChains.RemoveAll(c => c == null);
    }

    private Enemy ResolveInitialTarget(Transform candidate)
    {
        Enemy tEnemy = null;
        if (candidate != null) TryGetEnemy(candidate, out tEnemy);

        bool validTarget =
            tEnemy != null &&
            tEnemy.Health != null &&
            !tEnemy.Health.IsDead() &&
            Vector3.Distance(transform.position, tEnemy.transform.position) <= stats.Range;

        if (validTarget) return tEnemy;

        // Si no llega target válido, elegimos el más cercano en rango
        var closest = EnemyTracker.GetActiveEnemies()
            .Where(e => e != null && e.Health != null && !e.Health.IsDead())
            .Where(e => Vector3.Distance(transform.position, e.transform.position) <= stats.Range)
            .OrderBy(e => Vector3.Distance(transform.position, e.transform.position))
            .FirstOrDefault();

        return closest;
    }

    private IEnumerator ChainLightning(Enemy firstTarget)
    {
        var visited = new HashSet<Enemy>();
        Enemy current = firstTarget;
        Vector3 segmentOrigin = firePoint.position;

        for (int hop = 0; hop < maxChainTargets; hop++)
        {
            if (current == null || current.Health == null || current.Health.IsDead()) break;

            // 1) Calcular raycast hacia el objetivo actual desde el origen del segmento
            if (!TryRayToEnemy(segmentOrigin, current, out RaycastHit hit, out Vector3 shotDir))
            {
                // Si no hay línea limpia, cortamos la cadena
                if (debugDraw) Debug.DrawLine(segmentOrigin, current.transform.position + targetOffset, Color.gray, 0.12f);
                break;
            }

            // 2) Viajar con bolt hasta el punto de impacto y resolver daño + vfx
            yield return BoltTravelAndImpact(segmentOrigin, hit, current, shotDir);

            visited.Add(current);

            // 3) Preparar próximo salto
            segmentOrigin = hit.point; // siguiente segmento parte desde el punto de impacto actual

            // Buscar siguiente objetivo vivo más cercano no visitado
            Enemy next = FindNextChainTarget(current, visited);
            if (next == null) break;

            // Delay visual opcional entre saltos
            if (chainDelay > 0f) yield return new WaitForSeconds(chainDelay);

            current = next;
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

        // Obstrucciones (LOS) opcional
        if (losBlockers != 0 &&
            Physics.Raycast(origin, ndir, dist, losBlockers, QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        // Raycast fino
        if (Physics.Raycast(origin, ndir, out hit, dist + 0.25f, hittableLayers, QueryTriggerInteraction.Ignore))
        {
            return true;
        }

        // Fallback con spherecast
        if (sphereCastRadius > 0f &&
            Physics.SphereCast(origin, sphereCastRadius, ndir, out hit, dist + 0.25f, hittableLayers, QueryTriggerInteraction.Ignore))
        {
            return true;
        }

        return false;
    }

    private IEnumerator BoltTravelAndImpact(Vector3 start, RaycastHit hit, Enemy enemyAtShotTime, Vector3 shotDir)
    {
        // Si no hay prefab, impacto inmediato
        if (boltPrefab == null)
        {
            ResolveImpact(hit, enemyAtShotTime, -(hit.normal.sqrMagnitude > 0.0001f ? hit.normal : shotDir));
            yield break;
        }

        GameObject bolt = Instantiate(boltPrefab, start, Quaternion.identity, boltParent ? boltParent : null);
        if (boltSpawnScale != Vector3.one) bolt.transform.localScale = boltSpawnScale;

        Vector3 toTarget = (hit.point - start);
        float totalDist = toTarget.magnitude;

        if (totalDist <= 0.0001f)
        {
            Destroy(bolt);
            ResolveImpact(hit, enemyAtShotTime, -(hit.normal.sqrMagnitude > 0.0001f ? hit.normal : (toTarget.sqrMagnitude > 0f ? toTarget.normalized : Vector3.forward)));
            yield break;
        }

        Vector3 dir = toTarget / totalDist;
        bolt.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

        float killAt = (boltHardLifetime > 0f) ? Time.time + boltHardLifetime : float.PositiveInfinity;
        float remaining = totalDist;

        while (remaining > 0f)
        {
            float step = boltSpeed * Time.deltaTime;
            if (step <= 0f) break;

            float advance = Mathf.Min(step, remaining);
            bolt.transform.position += dir * advance;
            remaining -= advance;

            if (Time.time >= killAt) break;
            yield return null;
        }

        Destroy(bolt);
        ResolveImpact(hit, enemyAtShotTime, -dir);

        if (debugDraw) Debug.DrawLine(start, hit.point, Color.yellow, 0.15f);
    }

    private void ResolveImpact(RaycastHit hit, Enemy enemyAtShotTime, Vector3 fallbackDir)
    {
        SpawnImpactVfx(hit, fallbackDir);

        // Aplica daño en el impacto
        if (enemyAtShotTime != null && enemyAtShotTime.Health != null && !enemyAtShotTime.Health.IsDead())
        {
            int dmg = Mathf.Max(1, Mathf.RoundToInt(stats.Damage));
            enemyAtShotTime.Health.TakeDamage(dmg);
        }
        else
        {
            // Si el reference cambió, intentamos aplicar al collider golpeado
            ApplyDamage(hit.collider.transform, Mathf.Max(1, Mathf.RoundToInt(stats.Damage)));
        }

        if (_audioSource && impactClip) _audioSource.PlayOneShot(impactClip, impactVolume);
    }

    private Enemy FindNextChainTarget(Enemy from, HashSet<Enemy> visited)
    {
        if (from == null) return null;

        float radius = perHopSearchRadius > 0f ? perHopSearchRadius : stats.Range;

        var candidates = EnemyTracker.GetActiveEnemies()
            .Where(e => e != null && e.Health != null && !e.Health.IsDead())
            .Where(e => !visited.Contains(e))
            .Where(e => Vector3.Distance(from.transform.position, e.transform.position) <= radius)
            .OrderBy(e => Vector3.Distance(from.transform.position, e.transform.position))
            .ToList();

        return candidates.Count > 0 ? candidates[0] : null;
    }

    private void SpawnImpactVfx(RaycastHit hit, Vector3 fallbackDir)
    {
        if (!impactVfxPrefab) return;

        Vector3 normal = hit.normal.sqrMagnitude > 0.0001f ? hit.normal : -fallbackDir;
        Vector3 pos = hit.point + normal * impactVfxOffsetAlongNormal;
        Quaternion rot = Quaternion.LookRotation(normal);
        Transform parent = parentVfxToHit ? hit.collider.transform : null;

        GameObject fx = Instantiate(impactVfxPrefab, pos, rot, parent);
        if (impactVfxLifetime > 0f) Destroy(fx, impactVfxLifetime);
    }

    private static void ApplyDamage(Transform hitTf, int damage)
    {
        if (!hitTf) return;

        var h = hitTf.GetComponentInParent<EnemyHealth>();
        if (h != null) { h.TakeDamage(damage); return; }

        var dmg = hitTf.GetComponentInParent<IDamageable>();
        if (dmg != null) { dmg.TakeDamage(damage); return; }

        var e = hitTf.GetComponentInParent<Enemy>();
        if (e != null && e.Health != null) { e.Health.TakeDamage(damage); }
    }

    private static bool TryGetEnemy(Transform t, out Enemy enemy)
    {
        enemy = null;
        if (!t) return false;

        enemy = t.GetComponentInParent<Enemy>();
        if (enemy != null && enemy.Health != null) return true;

        var health = t.GetComponentInParent<EnemyHealth>();
        if (health != null)
        {
            enemy = health.GetComponentInParent<Enemy>();
            return enemy != null && enemy.Health != null;
        }
        return false;
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
