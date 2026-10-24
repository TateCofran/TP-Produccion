using System.Collections.Generic;
using UnityEngine;

public class Ability_DestroyRandomTurretOnLowHP : MonoBehaviour, IEnemyAbility
{
    [Header("Trigger")]
    [SerializeField, Range(0.05f, 0.9f)]
    private float hpThresholdPercent = 0.25f;

    [SerializeField]
    private bool onlyOncePerSpawn = true;

    [Header("Turrets")]
    [SerializeField] private LayerMask turretLayers;
    [SerializeField, Min(10f)] private float searchRadius = 200f;

    [Header("FX (optional)")]
    [SerializeField] private GameObject destroyVfxPrefab;
    [SerializeField] private float vfxLifetime = 2f;

    private AbilityContext _ctx;
    private bool _used;

    public void Initialize(AbilityContext ctx)
    {
        _ctx = ctx;
        _used = false;
    }

    public void Tick(float dt)
    {
    }

    public void OnDamaged(float dmg, float currentHP, object source)
    {
        if (_used && onlyOncePerSpawn) return;
        if (_ctx.Health == null) return;

        float max = _ctx.Health.GetMaxHealth();
        if (max <= 0f) return;

        float thresholdHP = max * hpThresholdPercent;

        if (currentHP > thresholdHP) return;

        if (DestroyRandomTurret())
        {
            _used = true;
        }
    }

    public void OnDeath() { }
    public void OnArrivedToCore() { }
    public void OnWaypoint(int index) { }

    public void ResetRuntime()
    {
        _used = false;
    }

    private bool DestroyRandomTurret()
    {
        if (turretLayers.value == 0)
        {
            Debug.LogWarning("[Ability_DestroyRandomTurretOnLowHP] turretLayers is empty. Assign the turret layer in the inspector");
            return false;
        }

        if (_ctx.Transform == null)
            return false;

        Collider[] hits = Physics.OverlapSphere(_ctx.Transform.position, searchRadius, turretLayers);
        if (hits == null || hits.Length == 0)
            return false;

        List<Turret> candidates = new List<Turret>();

        for (int i = 0; i < hits.Length; i++)
        {
            Collider col = hits[i];
            if (col == null) continue;

            Turret turret = col.GetComponentInParent<Turret>();
            if (turret == null) continue;

            GameObject go = turret.gameObject;
            if (!go.activeInHierarchy) continue;

            if (!candidates.Contains(turret))
                candidates.Add(turret);
        }

        if (candidates.Count == 0)
            return false;

        int index = Random.Range(0, candidates.Count);
        Turret target = candidates[index];

        Vector3 pos = target.transform.position;

        if (destroyVfxPrefab != null)
        {
            VfxPool.Spawn(destroyVfxPrefab, pos, Quaternion.identity, vfxLifetime);
        }

        Debug.Log("[Ability_DestroyRandomTurretOnLowHP] Destroying turret: " + target.name);

        Object.Destroy(target.gameObject);

        return true;
    }

    private void OnDrawGizmosSelected()
    {
        Transform t = _ctx.Transform != null ? _ctx.Transform : transform;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(t.position, searchRadius);
    }
}
