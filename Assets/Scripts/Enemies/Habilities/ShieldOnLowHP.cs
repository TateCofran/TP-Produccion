using UnityEngine;

public class Ability_ShieldOnLowHP : MonoBehaviour, IEnemyAbility
{
    [Header("Config")]
    [SerializeField, Range(0.05f, 0.9f)] private float thresholdPercent = 0.25f; // 25%
    [SerializeField, Min(0f)] private float shieldFactor = 1.0f; // 1.0 = 100% del max HP
    [SerializeField] private bool onlyOnce = true;

    [Header("FX (opcional)")]
    [SerializeField] private GameObject shieldVfxPrefab;

    private AbilityContext _ctx;
    private bool _used;

    public void Initialize(AbilityContext ctx)
    {
        _ctx = ctx;
        _used = false;

        // Seguridad pooling: si quedó escudo viejo en el enemigo, lo respetamos.
        // Si querés que siempre arranque sin escudo, descomentá:
        // _ctx.Health.ClearShield();
    }

    public void OnDamaged(float dmg, float currentHP, object source)
    {
        if (_used && onlyOnce) return;

        float max = _ctx.Health.GetMaxHealth();
        if (max <= 0f) return;

        if (currentHP <= max * thresholdPercent)
        {
            float amount = Mathf.Max(0f, max * shieldFactor);
            _ctx.Health.AddShield(amount);
            _used = true;

            // VFX opcional
            if (shieldVfxPrefab)
            {
                var vfx = Object.Instantiate(shieldVfxPrefab, _ctx.Transform.position, Quaternion.identity, _ctx.Transform);
                Object.Destroy(vfx, 3f);
            }
        }
    }

    public void Tick(float dt) { }
    public void OnDeath() { /* no hace falta limpiar (muere igual) */ }
    public void OnArrivedToCore() { /* no-op */ }
    public void OnWaypoint(int index) { }
    public void ResetRuntime() { _used = false; }
}
