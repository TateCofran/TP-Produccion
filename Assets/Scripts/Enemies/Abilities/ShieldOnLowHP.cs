using UnityEngine;

public class Ability_ShieldOnLowHP : MonoBehaviour, IEnemyAbility
{
    [Header("Config")]
    [SerializeField, Range(0.05f, 0.9f)]
    private float thresholdPercent = 0.25f;

    [SerializeField, Range(1.0f, 3.0f)]
    private float hpMultiplier = 1.5f;

    [SerializeField]
    private bool onlyOnce = true;

    [Header("FX (optional)")]
    [SerializeField] private GameObject shieldVfxPrefab;
    [SerializeField] private float vfxLifetime = 3f;

    private AbilityContext _ctx;
    private bool _used;

    public void Initialize(AbilityContext ctx)
    {
        _ctx = ctx;
        _used = false;
    }

    public void OnDamaged(float dmg, float currentHP, object source)
    {
        if (_used && onlyOnce) return;
        if (_ctx.Health == null) return;

        float max = _ctx.Health.GetMaxHealth();
        if (max <= 0f) return;

        if (currentHP <= max * thresholdPercent)
        {
            bool applied = _ctx.Health.TryApplyShieldAuraBoost(hpMultiplier);
            if (!applied) return;

            _used = true;

            if (shieldVfxPrefab != null && _ctx.Transform != null)
            {
                VfxPool.Spawn(shieldVfxPrefab, _ctx.Transform.position, Quaternion.identity, vfxLifetime, _ctx.Transform);
            }
        }
    }

    public void Tick(float dt) { }
    public void OnDeath() { }
    public void OnArrivedToCore() { }
    public void OnWaypoint(int index) { }

    public void ResetRuntime()
    {
        _used = false;
    }
}
