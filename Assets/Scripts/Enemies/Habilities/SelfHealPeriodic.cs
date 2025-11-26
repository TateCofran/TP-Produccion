using UnityEngine;

public class Ability_SelfHealPeriodic : MonoBehaviour, IEnemyAbility
{
    [Header("Heal settings")]
    [SerializeField, Range(0f, 1f)] private float healPercentOfMaxHP = 0.10f; // 0.10 = 10%
    [SerializeField, Min(0.1f)] private float intervalSeconds = 5f;

    [Header("FX (optional)")]
    [SerializeField] private GameObject healVfxPrefab;
    [SerializeField, Min(0f)] private float vfxLifetime = 1.5f;

    private AbilityContext _ctx;
    private float _timer;
    private bool _active;

    public void Initialize(AbilityContext ctx)
    {
        _ctx = ctx;
        _timer = 0f;
        _active = true;
    }

    public void Tick(float dt)
    {
        if (!_active || _ctx.Health == null || _ctx.Health.IsDead()) return;

        _timer += dt;
        if (_timer < intervalSeconds) return;
        _timer = 0f;

        float max = Mathf.Max(1f, _ctx.Health.GetMaxHealth());
        float healAmount = healPercentOfMaxHP * max;
        if (healAmount <= 0f) return;

        _ctx.Health.Heal(healAmount);

        if (healVfxPrefab != null)
        {
            var vfx = Instantiate(
                healVfxPrefab,
                _ctx.Transform.position,
                Quaternion.identity,
                _ctx.Transform
            );
            Destroy(vfx, vfxLifetime);
        }
    }

    public void OnDamaged(float dmg, float currentHP, object source)
    {
        // no-op
    }

    public void OnDeath()
    {
        _active = false;
    }

    public void OnArrivedToCore()
    {
        _active = false;
    }

    public void OnWaypoint(int index)
    {
        // no-op
    }

    public void ResetRuntime()
    {
        _timer = 0f;
        _active = true;
    }
}
