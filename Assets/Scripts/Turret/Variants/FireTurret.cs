using System.Collections;
using UnityEngine;

[RequireComponent(typeof(TurretStats))]
public class FireTurret : MonoBehaviour, IShootingBehavior
{
    private ITurretStats stats;

    [Header("Refs")]
    [SerializeField] private Transform firePoint;

    [Header("Raycast")]
    [SerializeField] private LayerMask hittableLayers = ~0;
    [SerializeField] private LayerMask losBlockers = 0;
    [SerializeField] private float sphereCastRadius = 0.12f;
    [SerializeField] private Vector3 targetOffset = new Vector3(0f, 0.35f, 0f);
    [SerializeField] private bool debugDraw;

    [Header("Impact VFX (en la adquisición del objetivo)")]
    [SerializeField] private GameObject impactVfxPrefab;
    [SerializeField] private float impactVfxOffsetAlongNormal = 0.03f;
    [SerializeField] private bool parentVfxToHit = true;
    [SerializeField] private float impactVfxLifetime = 1.5f;

    [Header("Fireball (proyectil visual)")]
    [SerializeField] private GameObject fireballPrefab;
    [SerializeField] private float fireballSpeed = 20f;
    [Tooltip("Si se asigna, el fireball se parenta aquí. Si es null, queda en raíz.")]
    [SerializeField] private Transform fireballParent;
    [Tooltip("Si > 0, destruir el fireball por seguridad a los N segundos (además del impacto).")]
    [SerializeField] private float fireballHardLifetime = 3f;
    [Tooltip("Opcional: escalar el fireball al instanciarlo.")]
    [SerializeField] private Vector3 fireballSpawnScale = Vector3.one;

    [Header("Daño de fuego (DoT)")]
    [SerializeField] private float tickRate = 1f;
    [SerializeField] private float damageFractionPerSecond = 1f / 2f;
    [Tooltip("Opcional: instanciar VFX en cada tick (en el centro del enemigo).")]
    [SerializeField] private bool vfxOnEachTick = false;

    private float tickCountdown;
    private Enemy burningTarget;
    private float totalDamageApplied = 0f;

    [Header("Impact Sound")]
    [SerializeField] private AudioClip impactClip;
    [Range(0f, 1f)]
    [SerializeField] private float impactVolume = 1f;
    [SerializeField] private bool ensureAudioSource = true;

    private AudioSource _audioSource;

    private void Awake()
    {
        stats = GetComponent<ITurretStats>();
        if (!firePoint) Debug.LogWarning("[FireTurret] Falta asignar firePoint.");
        tickCountdown = 0f;

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

    private void Update()
    {
        if (burningTarget == null) return;

        tickCountdown -= Time.deltaTime;
        if (tickCountdown <= 0f)
        {
            ApplyBurnDamage();
            tickCountdown = 1f / tickRate;
        }
    }

    public void Shoot(Transform firePointIgnored, Transform target, ITurretStats statsParam)
    {
        if (!firePoint || target == null || stats == null) return;

        Vector3 o = firePoint.position;
        Vector3 predicted = target.position + targetOffset;
        Vector3 dir = (predicted - o);
        float dist = dir.magnitude;
        if (dist <= 0.001f) return;

        Vector3 ndir = dir / dist;

        // Línea de visión opcional
        if (losBlockers != 0 &&
            Physics.Raycast(o, ndir, dist, losBlockers, QueryTriggerInteraction.Ignore))
        {
            if (debugDraw) Debug.DrawLine(o, predicted, Color.gray, 0.1f);
            return;
        }

        // 1) Intento con Raycast fino
        if (Physics.Raycast(o, ndir, out RaycastHit hit, dist + 0.25f, hittableLayers, QueryTriggerInteraction.Ignore))
        {
            HandleAcquiredHit(o, ndir, hit);
            return;
        }

        // 2) Fallback con SphereCast (más ancho)
        if (sphereCastRadius > 0f &&
            Physics.SphereCast(o, sphereCastRadius, ndir, out RaycastHit shit, dist + 0.25f, hittableLayers, QueryTriggerInteraction.Ignore))
        {
            HandleAcquiredHit(o, ndir, shit);
            return;
        }

        // Si no impactó nada, opcionalmente podríamos disparar a un punto “vacío”,
        // pero el requerimiento pide que el fireball vaya al punto de impacto real del raycast,
        // así que si no hay impacto, no disparamos fireball.
    }

    private void HandleAcquiredHit(Vector3 origin, Vector3 shotDir, RaycastHit hit)
    {
        Enemy enemy = null;
        TryGetEnemy(hit.collider.transform, out enemy);

        // Disparar el fireball hacia el punto de impacto detectado por el raycast.
        if (fireballPrefab != null)
        {
            StartCoroutine(FireballTravel(origin, hit, enemy));
        }
        else
        {
            // Si no hay prefab, al menos mantener el comportamiento previo (impact VFX + DoT).
            SpawnImpactVfx(hit, shotDir);
            if (enemy != null) StartOrRefreshBurn(enemy);
        }

        if (debugDraw) Debug.DrawLine(origin, hit.point, Color.red, 0.15f);
    }

    private IEnumerator FireballTravel(Vector3 start, RaycastHit hit, Enemy enemyAtShotTime)
    {
        // Instancia del fireball
        GameObject fb = Instantiate(fireballPrefab, start, Quaternion.identity, fireballParent ? fireballParent : null);
        if (fireballSpawnScale != Vector3.one) fb.transform.localScale = fireballSpawnScale;

        // Orientar hacia el punto de impacto (trayectoria recta del raycast)
        Vector3 target = hit.point;
        Vector3 toTarget = (target - start);
        float totalDist = toTarget.magnitude;
        if (totalDist <= 0.0001f)
        {
            // Nada que recorrer: resolvemos impacto inmediatamente
            Destroy(fb);
            ResolveImpact(hit, enemyAtShotTime, -toTarget.normalized);
            yield break;
        }

        Vector3 dir = toTarget / totalDist;
        fb.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

        // Seguridad de vida del proyectil
        float killAt = (fireballHardLifetime > 0f) ? Time.time + fireballHardLifetime : float.PositiveInfinity;

        // Movimiento lineal hasta el impacto
        float remaining = totalDist;
        Vector3 lastPos = fb.transform.position;

        while (remaining > 0f)
        {
            float step = fireballSpeed * Time.deltaTime;
            if (step <= 0f) break;

            // Evitar overshoot
            float advance = Mathf.Min(step, remaining);
            fb.transform.position += dir * advance;
            remaining -= advance;

            // Si el proyectil tiene un Trail o VFX, ya seguirá su propio update

            // Seguridad temporal
            if (Time.time >= killAt) break;

            lastPos = fb.transform.position;
            yield return null;
        }

        // Llegó (o timeout): resolver impacto visual y lógicas
        Destroy(fb);
        ResolveImpact(hit, enemyAtShotTime, -dir);
    }

    private void ResolveImpact(RaycastHit hit, Enemy enemyAtShotTime, Vector3 fallbackDir)
    {
        // VFX + sonido
        SpawnImpactVfx(hit, fallbackDir);

        // Iniciar/Refrescar DoT SOLO al impactar
        if (enemyAtShotTime != null)
        {
            StartOrRefreshBurn(enemyAtShotTime);
        }
    }

    private void StartOrRefreshBurn(Enemy enemy)
    {
        if (enemy == null || enemy.Health == null || enemy.Health.IsDead()) return;
        if (burningTarget == enemy) return;

        burningTarget = enemy;
        totalDamageApplied = 0f;
        tickCountdown = 0f;
    }

    private void ApplyBurnDamage()
    {
        if (burningTarget == null || burningTarget.Health == null) return;
        if (burningTarget.Health.IsDead())
        {
            burningTarget = null;
            totalDamageApplied = 0f;
            return;
        }

        if (totalDamageApplied < stats.Damage)
        {
            float remaining = stats.Damage - totalDamageApplied;
            float dps = Mathf.Max(0f, stats.Damage * damageFractionPerSecond);
            float damageThisTick = Mathf.Min(dps, remaining);

            int dmgInt = Mathf.Max(1, Mathf.RoundToInt(damageThisTick));
            float before = burningTarget.Health.GetCurrentHealth();
            burningTarget.Health.TakeDamage(dmgInt);
            float after = burningTarget.Health.GetCurrentHealth();

            float realApplied = Mathf.Max(0f, before - after);
            totalDamageApplied += realApplied;

            if (vfxOnEachTick && impactVfxPrefab)
            {
                Vector3 pos = burningTarget.transform.position + Vector3.up * 0.35f;
                GameObject fx = Instantiate(impactVfxPrefab, pos, Quaternion.identity, parentVfxToHit ? burningTarget.transform : null);
                if (impactVfxLifetime > 0f) Destroy(fx, impactVfxLifetime);
            }
        }
        else
        {
            burningTarget = null;
            totalDamageApplied = 0f;
        }
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
        if (_audioSource && impactClip) _audioSource.PlayOneShot(impactClip, impactVolume);
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
}
