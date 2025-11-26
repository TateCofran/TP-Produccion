using System.Collections.Generic;
using UnityEngine;

public class Ability_DestroyRandomTurretOnLowHP : MonoBehaviour, IEnemyAbility
{
    [Header("Trigger")]
    [SerializeField, Range(0.05f, 0.9f)] private float hpThresholdPercent = 0.25f; // 25%
    [SerializeField] private bool onlyOncePerSpawn = true;

    [Header("Turrets")]
    [SerializeField] private LayerMask turretLayers; // pon aca la layer "Turret"
    [SerializeField, Min(10f)] private float searchRadius = 200f; // bastante grande para agarrar todo el mapa

    [Header("FX (optional)")]
    [SerializeField] private GameObject destroyVfxPrefab;
    [SerializeField, Min(0f)] private float vfxLifetime = 2f;

    private AbilityContext _ctx;
    private bool _used;

    public void Initialize(AbilityContext ctx)
    {
        _ctx = ctx;
        _used = false;
    }

    public void Tick(float dt)
    {
        // no necesita lógica por frame
    }

    public void OnDamaged(float dmg, float currentHP, object source)
    {
        if (_used && onlyOncePerSpawn) return;
        if (_ctx.Health == null) return;

        float max = Mathf.Max(1f, _ctx.Health.GetMaxHealth());
        float thresholdHP = max * hpThresholdPercent;

        // Solo cuando la vida actual ya está por debajo del umbral
        if (currentHP > thresholdHP) return;

        if (DestroyRandomTurret())
        {
            _used = true;
        }
    }

    public void OnDeath()
    {
        // no-op
    }

    public void OnArrivedToCore()
    {
        // no-op
    }

    public void OnWaypoint(int index)
    {
        // no-op
    }

    public void ResetRuntime()
    {
        _used = false;
    }

    // ---------------------------
    // Lógica de destruir torreta
    // ---------------------------
    private bool DestroyRandomTurret()
    {
        if (turretLayers.value == 0)
        {
            Debug.LogWarning("[Ability_DestroyRandomTurretOnLowHP] turretLayers esta en 0. Asigna la layer de las torretas en el inspector.");
            return false;
        }

        if (_ctx.Transform == null)
            return false;

        // Buscamos colliders de torretas en un radio grande desde el jefe
        Collider[] hits = Physics.OverlapSphere(_ctx.Transform.position, searchRadius, turretLayers);
        if (hits == null || hits.Length == 0)
            return false;

        // Nos quedamos con los root GameObjects, activos y unicos
        List<GameObject> candidates = new List<GameObject>();
        for (int i = 0; i < hits.Length; i++)
        {
            var col = hits[i];
            if (col == null) continue;

            GameObject root = col.transform.root.gameObject;
            if (!root.activeInHierarchy) continue;

            if (!candidates.Contains(root))
                candidates.Add(root);
        }

        if (candidates.Count == 0)
            return false;

        // Elegimos una al azar
        int index = Random.Range(0, candidates.Count);
        GameObject target = candidates[index];

        Vector3 pos = target.transform.position;

        // FX opcional
        if (destroyVfxPrefab != null)
        {
            var vfx = Object.Instantiate(destroyVfxPrefab, pos, Quaternion.identity);
            Object.Destroy(vfx, vfxLifetime);
        }

        // Destruimos la torreta
        Object.Destroy(target);

        // Debug opcional
        // Debug.Log("[Ability_DestroyRandomTurretOnLowHP] Torreta destruida: " + target.name);

        return true;
    }

    private void OnDrawGizmosSelected()
    {
        if (_ctx.Transform != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_ctx.Transform.position, searchRadius);
        }
        else
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, searchRadius);
        }
    }
}
