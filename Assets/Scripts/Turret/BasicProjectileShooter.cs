using UnityEngine;

[RequireComponent(typeof(TurretDataHolder))]
public class BasicProjectileShooter : MonoBehaviour, IShootingBehavior
{
    [SerializeField] private Transform firePoint;
    private TurretStats stats;
    private void Awake()
    {
        stats = GetComponent<TurretStats>();
        if (stats == null)
            Debug.LogError("[BasicProjectileShooter] No se encontró TurretStats en la torreta.");
    }
    public void Shoot(Transform firePoint, Transform target, ITurretStats stats)
    {
        if (target == null || firePoint == null) return;

        var projectile = ProjectilePool.Instance.GetProjectile();
        projectile.transform.position = firePoint.position;
        projectile.transform.rotation = firePoint.rotation;

        var p = projectile.GetComponent<Projectile>();
        p.Initialize(target, stats.Damage); // Usamos el daño real en runtime
    }

}
