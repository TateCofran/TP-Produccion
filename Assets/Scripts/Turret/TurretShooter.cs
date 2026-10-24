using UnityEngine;

public class TurretShooter : MonoBehaviour
{
    [SerializeField] private Transform firePoint;
    private IShootingBehavior shootingBehavior;
    private ITurretStats stats;

    private float fireCountdown;
    private Transform currentTarget;

    private bool _combatEnabled = true;

    void Awake()
    {
        stats = GetComponent<ITurretStats>();
        shootingBehavior = GetComponent<IShootingBehavior>();

        if (shootingBehavior == null)
        {
            Debug.LogWarning($"[TurretShooter] No IShootingBehavior on {gameObject.name}. This component will not shoot");
            enabled = false;
        }
    }

    void Update()
    {
        if (!_combatEnabled) return;
        if (currentTarget == null) return;

        fireCountdown -= Time.deltaTime;

        if (fireCountdown <= 0f)
        {
            Shoot();
            fireCountdown = 1f / Mathf.Max(0.01f, stats.FireRate);
        }
    }

    public void SetTarget(Transform target)
    {
        if (target == currentTarget) return;
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
