using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    private static readonly Collider[] AoeBuffer = new Collider[32];
    private static readonly HashSet<Enemy> AoeHits = new();

    [Header("Movement")]
    public float speed = 10f;

    [Header("Effects")]
    public bool isSlowing = false;
    [Range(0f, 1f)] public float slowAmount;
    public float slowDuration;

    public bool isAOE = false;
    public float explosionRadius = 0f;
    public LayerMask aoeLayers = ~0;

    private Transform target;
    private Enemy targetEnemy;
    private int targetSpawnId;
    private float damage;
    private bool isActive;

    public void Initialize(Transform targetTransform, float damageValue)
    {
        target = targetTransform;
        targetEnemy = targetTransform != null ? targetTransform.GetComponentInParent<Enemy>() : null;
        targetSpawnId = targetEnemy != null ? targetEnemy.SpawnId : 0;
        damage = damageValue;
        isActive = true;
    }

    private bool TargetIsValid()
    {
        if (target == null || !target.gameObject.activeInHierarchy) return false;
        return targetEnemy == null || targetEnemy.IsSameSpawn(targetSpawnId);
    }

    private void Update()
    {
        if (!isActive || !TargetIsValid())
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

    private void HitTarget()
    {
        if (isAOE && explosionRadius > 0f)
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, explosionRadius, AoeBuffer, aoeLayers, QueryTriggerInteraction.Collide);
            AoeHits.Clear();
            for (int i = 0; i < count; i++)
            {
                var enemy = AoeBuffer[i].GetComponentInParent<Enemy>();
                if (enemy != null && AoeHits.Add(enemy)) ApplyEffects(enemy.gameObject);
            }
            AoeHits.Clear();
        }
        else
        {
            ApplyEffects(target.gameObject);
        }

        ReturnToPool();
    }

    private void ApplyEffects(GameObject enemyGO)
    {
        if (enemyGO == null || !enemyGO.activeInHierarchy) return;

        var damageable = enemyGO.GetComponentInChildren<IDamageable>();
        if (damageable == null) return;

        damageable.TakeDamage(damage);

        if (isSlowing && slowAmount > 0f && slowDuration > 0f)
        {
            var slowable = enemyGO.GetComponentInParent<ISlowable>();
            slowable?.ApplySlow(slowAmount, slowDuration);
        }
    }

    private void ReturnToPool()
    {
        isActive = false;
        target = null;
        targetEnemy = null;

        if (ProjectilePool.Instance != null)
            ProjectilePool.Instance.ReturnProjectile(gameObject);
        else
            gameObject.SetActive(false);
    }
}
