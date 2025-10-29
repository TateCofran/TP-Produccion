using UnityEngine;

public class TurretShooter : MonoBehaviour
{
    [SerializeField] private Transform firePoint;
    private IShootingBehavior shootingBehavior;
    private ITurretStats stats;

    private float fireCountdown;
    private Transform currentTarget;

    // NUEVO: respetar el gate de colocación
    private bool _combatEnabled = true;

    void Awake()
    {
        stats = GetComponent<ITurretStats>();
        shootingBehavior = GetComponent<IShootingBehavior>();

        if (shootingBehavior == null)
        {
            Debug.LogWarning($"[TurretShooter] No hay IShootingBehavior en {gameObject.name}. Este componente no disparará.");
            enabled = false;
        }
    }

    void Update()
    {
        if (!_combatEnabled) return;         // <- NUEVO: no disparar si no está colocado
        if (currentTarget == null) return;

        fireCountdown -= Time.deltaTime;

        if (fireCountdown <= 0f)
        {
            Shoot();
            fireCountdown = 1f / stats.FireRate;
        }
    }

    public void SetTarget(Transform target)
    {
        if (target == null) { currentTarget = null; return; }

        var enemy = target.GetComponent<Enemy>();
        currentTarget = enemy ? target : null;
    }

    public void SetCombatEnabled(bool enabledCombat)
    {
        _combatEnabled = enabledCombat;
    }

    private void Shoot()
    {
        if (shootingBehavior == null || firePoint == null || stats == null) return;
        shootingBehavior.Shoot(firePoint, currentTarget, stats);
    }
}
