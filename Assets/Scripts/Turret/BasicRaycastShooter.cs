using UnityEngine;

[RequireComponent(typeof(TurretDataHolder))]
public class BasicRaycastShooter : MonoBehaviour, IShootingBehavior
{
    [Header("Raycast")]
    [SerializeField] private LayerMask hittableLayers = ~0;
    [SerializeField] private float sphereCastRadius = 0.18f;
    [SerializeField] private Vector3 targetOffset = new Vector3(0f, 0.35f, 0f);
    [SerializeField] private bool debugDraw = false;

    [Header("Impact VFX")]
    [Tooltip("Prefab con VisualEffect (o cualquier FX) para el impacto.")]
    [SerializeField] private GameObject impactVfxPrefab;
    [Tooltip("Desfase para que el VFX no clippee dentro del enemigo.")]
    [SerializeField] private float impactVfxOffsetAlongNormal = 0.03f;
    [Tooltip("Si es true, el VFX se parentea al enemigo impactado.")]
    [SerializeField] private bool parentVfxToHit = true;
    [Tooltip("Si tu prefab no se autodestruye, lo limpiamos a los X seg.")]
    [SerializeField] private float impactVfxLifetime = 1.5f;

    [Header("Impact Sound")]
    [SerializeField] private AudioClip impactClip; // suena cuando se adquiere target (y opcional en cada tick si vfxOnEachTick)
    [Range(0f, 1f)]
    [SerializeField] private float impactVolume = 1f;
    [SerializeField] private bool ensureAudioSource = true;

    private AudioSource _audioSource;
    private void Awake()
    {
        if (ensureAudioSource)
        {
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
                _audioSource.spatialBlend = 1f; // 3D sound
                _audioSource.rolloffMode = AudioRolloffMode.Linear;
                _audioSource.minDistance = 1f;
                _audioSource.maxDistance = 20f;
                _audioSource.dopplerLevel = 0f;
            }
        }
    }

    public void Shoot(Transform firePoint, Transform target, ITurretStats stats)
    {
        if (!firePoint || !target || stats == null) return;

        Vector3 origin = firePoint.position;
        Vector3 toTarget = (target.position + targetOffset) - origin;
        float distance = toTarget.magnitude;
        if (distance <= 0.001f) return;

        Vector3 dir = toTarget / distance;

        // Raycast fino
        if (Physics.Raycast(origin, dir, out RaycastHit hit, distance + 0.25f, hittableLayers, QueryTriggerInteraction.Ignore))
        {
            SpawnImpactVfx(hit, dir);
            TryApplyDamage(hit.collider.transform, Mathf.Max(1, (int)stats.Damage));
            if (debugDraw) Debug.DrawLine(origin, hit.point, Color.cyan, 0.1f);
            return;
        }

        // Fallback: SphereCast
        if (sphereCastRadius > 0f &&
            Physics.SphereCast(origin, sphereCastRadius, dir, out RaycastHit shit, distance + 0.25f, hittableLayers, QueryTriggerInteraction.Ignore))
        {
            SpawnImpactVfx(shit, dir);
            TryApplyDamage(shit.collider.transform, Mathf.Max(1, (int)stats.Damage));
            if (debugDraw) Debug.DrawLine(origin, shit.point, Color.yellow, 0.1f);
            return;
        }

        if (debugDraw) Debug.DrawLine(origin, origin + dir * Mathf.Min(distance, 5f), Color.red, 0.1f);
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

        _audioSource.PlayOneShot(impactClip, impactVolume);
    }

    private static bool TryApplyDamage(Transform hitTf, int damage)
    {
        if (!hitTf) return false;

        var enemyHealth = hitTf.GetComponentInParent<EnemyHealth>();
        if (enemyHealth != null) { enemyHealth.TakeDamage(damage); return true; }

        var dmg = hitTf.GetComponentInParent<IDamageable>();
        if (dmg != null) { dmg.TakeDamage(damage); return true; }

        var enemy = hitTf.GetComponentInParent<Enemy>();
        if (enemy != null && enemy.Health != null) { enemy.Health.TakeDamage(damage); return true; }

        return false;
    }
}
