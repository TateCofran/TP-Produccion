using System.Collections.Generic;
using UnityEngine;

public class Ability_ShieldAuraHpBoost : MonoBehaviour, IEnemyAbility
{
    [Header("Shield Aura")]
    [SerializeField, Min(0f)] private float radius = 6f;
    [SerializeField, Range(1f, 3f)] private float hpMultiplier = 1.5f; // 1.5 = +50% HP
    [SerializeField, Min(0.1f)] private float checkInterval = 1.0f;
    [SerializeField] private bool affectSelf = true;

    [Header("FX (optional)")]
    [SerializeField] private GameObject shieldVfxPrefab;
    [SerializeField, Min(0f)] private float vfxLifetime = 2f;

    private AbilityContext _ctx;
    private float _timer;

    // buffer local para evitar allocations
    private readonly List<Enemy> _buffer = new List<Enemy>(64);

    public void Initialize(AbilityContext ctx)
    {
        _ctx = ctx;
        _timer = 0f;
    }

    public void Tick(float dt)
    {
        _timer -= dt;
        if (_timer > 0f) return;
        _timer = checkInterval;

        ApplyAura();
    }

    public void OnDamaged(float dmg, float currentHP, object source) { }

    public void OnDeath() { }

    public void OnArrivedToCore() { }

    public void OnWaypoint(int index) { }

    public void ResetRuntime()
    {
        _timer = 0f;
    }

    private void ApplyAura()
    {
        if (_ctx.Transform == null) return;

        _buffer.Clear();

        // Usamos EnemyTracker.Enemies directamente
        var all = EnemyTracker.Enemies;
        Vector3 center = _ctx.Transform.position;
        float r2 = radius * radius;

        for (int i = 0; i < all.Count; i++)
        {
            var e = all[i];
            if (e == null || e.Health == null || e.Health.IsDead()) continue;

            if (!affectSelf && e == _ctx.Enemy) continue;

            Vector3 diff = e.transform.position - center;
            if (diff.sqrMagnitude > r2) continue;

            _buffer.Add(e);
        }

        for (int i = 0; i < _buffer.Count; i++)
        {
            var enemy = _buffer[i];
            if (enemy == null || enemy.Health == null) continue;

            bool applied = enemy.Health.TryApplyShieldAuraBoost(hpMultiplier);
            if (applied && shieldVfxPrefab != null)
            {
                var vfx = Object.Instantiate(
                    shieldVfxPrefab,
                    enemy.transform.position,
                    Quaternion.identity,
                    enemy.transform
                );
                Object.Destroy(vfx, vfxLifetime);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
