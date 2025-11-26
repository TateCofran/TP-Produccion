using System.Collections.Generic;
using UnityEngine;

public class Ability_HealAura : MonoBehaviour, IEnemyAbility
{
    [Header("Aura")]
    [SerializeField, Min(0f)] private float radius = 5f;
    [SerializeField, Range(0f, 1f)] private float healPercentOfMaxHP = 0.10f; // 0.10 = 10%
    [SerializeField, Min(0.1f)] private float intervalSeconds = 5f;
    [SerializeField] private bool affectSelf = true;
    [SerializeField] private bool drawGizmos = true;

    [Header("FX (opcional)")]
    [SerializeField] private GameObject healPulseVfx;
    [SerializeField, Min(0f)] private float vfxLifetime = 1.5f;

    private AbilityContext _ctx;
    private float _timer;
    private bool _active = true;

    // buffer GC-friendly
    private readonly List<Enemy> _buffer = new();

    public void Initialize(AbilityContext ctx)
    {
        _ctx = ctx;
        _timer = 0f;
        _active = true;
        _buffer.Clear();
    }

    public void Tick(float dt)
    {
        if (!_active) return;

        _timer += dt;
        if (_timer < intervalSeconds) return;
        _timer = 0f;

        // Obtener enemigos en rango
        EnemyTracker.GetEnemiesInRangeNonAlloc(_ctx.Transform.position, radius, _buffer);

        for (int i = 0; i < _buffer.Count; i++)
        {
            var e = _buffer[i];
            if (e == null || e.Health == null || e.Health.IsDead()) continue;
            if (!affectSelf && e == _ctx.Enemy) continue;

            float max = Mathf.Max(1f, e.Health.GetMaxHealth());
            float healAmount = healPercentOfMaxHP * max;
            if (healAmount <= 0f) continue;

            e.Health.Heal(healAmount); // usa tu Heal(float amount)

            // VFX opcional
            if (healPulseVfx != null)
            {
                var vfx = Object.Instantiate(
                    healPulseVfx,
                    e.transform.position,
                    Quaternion.identity,
                    e.transform
                );
                Object.Destroy(vfx, vfxLifetime);
            }
        }
    }

    public void OnDamaged(float dmg, float currentHP, object source) { }
    public void OnDeath() { _active = false; }
    public void OnArrivedToCore() { _active = false; }
    public void OnWaypoint(int index) { }

    public void ResetRuntime()
    {
        _timer = 0f;
        _active = true;
        _buffer.Clear();
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
