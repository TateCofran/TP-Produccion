using UnityEngine;

[RequireComponent(typeof(TurretDataHolder))]
public class BasicProjectileShooter : MonoBehaviour, IShootingBehavior
{
    [SerializeField] private Transform firePoint;

    public void Shoot(Transform firePoint, Transform target, ITurretStats stats)
    {
        if (target == null || firePoint == null || stats == null) return;

        if (ProjectilePool.Instance == null)
        {
            Debug.LogWarning("[BasicProjectileShooter] ProjectilePool not found in the scene");
            return;
        }

        var projectile = ProjectilePool.Instance.GetProjectile();
        projectile.transform.SetPositionAndRotation(firePoint.position, firePoint.rotation);

        if (projectile.TryGetComponent<Projectile>(out var p))
            p.Initialize(target, stats.Damage);
    }
}
