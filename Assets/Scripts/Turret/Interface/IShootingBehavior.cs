using UnityEngine;

public interface IShootingBehavior
{
    void Shoot(Transform firePoint, Transform target, ITurretStats stats);
}
