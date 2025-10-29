using UnityEngine;

public class Projectile : MonoBehaviour
{
    private Transform target;
    private float damage;
    private bool isActive = false;

    [Header("Movimiento")]
    public float speed = 10f;

    [Header("Efectos")]
    public bool isSlowing = false;
    public float slowAmount;
    public float slowDuration;

    public bool isAOE = false;
    public float explosionRadius = 0f;

    public void Initialize(Transform targetEnemy, float damageValue)
    {
        target = targetEnemy;
        damage = damageValue;
        isActive = true;

        // Resetear flags
        isAOE = false;
        isSlowing = false;
        slowAmount = 0f;
        slowDuration = 0f;
        explosionRadius = 0f;
    }

    void Update()
    {
        if (!isActive || target == null || !target.gameObject.activeInHierarchy)
        {
            ReturnToPool();
            return;
        }


        Vector3 direction = target.position - transform.position;
        float distanceThisFrame = speed * Time.deltaTime;

        if (direction.magnitude <= distanceThisFrame)
        {
            HitTarget();
            return;
        }

        transform.Translate(direction.normalized * distanceThisFrame, Space.World);
    }

    void HitTarget()
    {
        if (isAOE)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);
            foreach (var hit in hits)
            {
                if (hit.CompareTag("Enemy"))
                    ApplyEffects(hit.gameObject);
            }
        }
        else
        {
            ApplyEffects(target.gameObject);
        }

        ReturnToPool();
    }

    void ApplyEffects(GameObject enemyGO)
    {
        if (enemyGO == null || !enemyGO.activeInHierarchy) return;

        var damageable = enemyGO.GetComponentInChildren<IDamageable>();
        //Debug.Log("damageable: " + damageable);

        if (damageable != null && IsTargetAlive(damageable))
        {
            damageable.TakeDamage(damage);

            if (isSlowing)
            {
                var slowable = enemyGO.GetComponent<ISlowable>();
                if (slowable != null)
                    slowable.ApplySlow(slowAmount, slowDuration);
            }
        }
    }
    bool IsTargetAlive(IDamageable damageable)
    {
        // Este método puede ser más sofisticado si tu interfaz IDamageable no tiene método IsAlive
        if (damageable is MonoBehaviour mono)
        {
            return mono.gameObject.activeInHierarchy;
        }

        return true;
    }


    void ReturnToPool()
    {
        isActive = false;
        target = null;
        gameObject.SetActive(false);
        ProjectilePool.Instance.ReturnProjectile(gameObject);
    }
}


