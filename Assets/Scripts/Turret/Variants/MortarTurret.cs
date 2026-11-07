using UnityEngine;

[RequireComponent(typeof(TurretDataHolder))]
public class MortarTurret : MonoBehaviour, IShootingBehavior
{
    [Header("Shell")]
    [Tooltip("Explosion radius in world units.")]
    [SerializeField, Min(0.1f)] private float explosionRadius = 1.5f;
    [SerializeField, Min(0f)] private float arcHeight = 3f;
    [Tooltip("Horizontal speed used to compute the flight time.")]
    [SerializeField, Min(0.1f)] private float shellSpeed = 7f;
    [SerializeField, Min(0.05f)] private float minFlightTime = 0.5f;
    [SerializeField, Min(0.05f)] private float maxFlightTime = 1.4f;

    public float ExplosionRadius => explosionRadius;

    public void Shoot(Transform firePoint, Transform target, ITurretStats stats)
    {
        if (firePoint == null || target == null || stats == null) return;

        var enemy = target.GetComponentInParent<Enemy>();
        if (enemy == null) return;

        Vector3 ground = target.position;
        var slot = GetComponentInParent<CellSlot>();
        ground.y = slot != null ? slot.transform.position.y : target.position.y;

        Vector3 flat = ground - firePoint.position;
        flat.y = 0f;
        float flight = Mathf.Clamp(flat.magnitude / shellSpeed, minFlightTime, maxFlightTime);

        MortarProjectile.Launch(firePoint.position, ground, flight, arcHeight, stats.Damage, explosionRadius);
        TurretShotFeedback.For(this)?.OnShot(firePoint.position);
    }
}
