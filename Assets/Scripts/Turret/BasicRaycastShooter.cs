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
    [Tooltip("Prefab spawned at the impact point.")]
    [SerializeField] private GameObject impactVfxPrefab;
    [Tooltip("Offset along the hit normal so the VFX does not clip into the enemy.")]
    [SerializeField] private float impactVfxOffsetAlongNormal = 0.03f;
    [Tooltip("If enabled, the VFX follows the enemy that was hit.")]
    [SerializeField] private bool parentVfxToHit = true;
    [Tooltip("Seconds before the VFX is returned to the pool.")]
    [SerializeField] private float impactVfxLifetime = 1.5f;

    [Header("Impact Sound")]
    [SerializeField] private AudioClip impactClip;
    [Range(0f, 1f)]
    [SerializeField] private float impactVolume = 1f;
    [SerializeField] private bool ensureAudioSource = true;

    private AudioSource audioSource;

    private void Awake()
    {
        if (!ensureAudioSource) return;

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

    public void Shoot(Transform firePoint, Transform target, ITurretStats stats)
    {
        if (!firePoint || !target || stats == null) return;

        Vector3 origin = firePoint.position;
        Vector3 toTarget = target.position + targetOffset - origin;
        float distance = toTarget.magnitude;
        if (distance <= 0.001f) return;

        Vector3 dir = toTarget / distance;
        int damage = Mathf.Max(1, (int)stats.Damage);

        if (Physics.Raycast(origin, dir, out RaycastHit hit, distance + 0.25f, hittableLayers, QueryTriggerInteraction.Ignore))
        {
            OnHit(hit, dir, damage);
            if (debugDraw) Debug.DrawLine(origin, hit.point, Color.cyan, 0.1f);
            return;
        }

        if (sphereCastRadius > 0f &&
            Physics.SphereCast(origin, sphereCastRadius, dir, out RaycastHit sphereHit, distance + 0.25f, hittableLayers, QueryTriggerInteraction.Ignore))
        {
            OnHit(sphereHit, dir, damage);
            if (debugDraw) Debug.DrawLine(origin, sphereHit.point, Color.yellow, 0.1f);
            return;
        }

        if (debugDraw) Debug.DrawLine(origin, origin + dir * Mathf.Min(distance, 5f), Color.red, 0.1f);
    }

    private void OnHit(RaycastHit hit, Vector3 dir, int damage)
    {
        SpawnImpactVfx(hit, dir);
        TryApplyDamage(hit.collider.transform, damage);
    }

    private void SpawnImpactVfx(RaycastHit hit, Vector3 fallbackDir)
    {
        if (impactVfxPrefab)
        {
            Vector3 normal = hit.normal.sqrMagnitude > 0.0001f ? hit.normal : -fallbackDir;
            Vector3 pos = hit.point + normal * impactVfxOffsetAlongNormal;
            Transform follow = parentVfxToHit ? hit.collider.transform : null;
            VfxPool.Spawn(impactVfxPrefab, pos, Quaternion.LookRotation(normal), impactVfxLifetime, follow);
        }

        if (audioSource && impactClip) audioSource.PlayOneShot(impactClip, impactVolume);
    }

    private static bool TryApplyDamage(Transform hitTf, int damage)
    {
        if (!hitTf) return false;

        var enemyHealth = hitTf.GetComponentInParent<EnemyHealth>();
        if (enemyHealth != null) { enemyHealth.TakeDamage(damage); return true; }

        var damageable = hitTf.GetComponentInParent<IDamageable>();
        if (damageable != null) { damageable.TakeDamage(damage); return true; }

        return false;
    }
}
