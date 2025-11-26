using UnityEngine;

public class Ability_SecondLifeBerserk : MonoBehaviour, IEnemyAbility
{
    [Header("Segunda vida")]
    [SerializeField, Range(0.1f, 1f)] private float revivePercent = 0.5f; // 50% de la vida
    [SerializeField] private bool onlyOncePerSpawn = true;

    [Header("Buff de velocidad")]
    [SerializeField, Min(1f)] private float speedMultiplier = 5f;
    [SerializeField] private float speedBuffDuration = 0f;
    // 0 = infinito hasta la muerte real, >0 = duración en segundos

    [Header("FX opcional")]
    [SerializeField] private GameObject reviveVfx;

    private AbilityContext _ctx;
    private bool _used;

    public void Initialize(AbilityContext ctx)
    {
        _ctx = ctx;
        _used = false;

        // Nos suscribimos al pre-death
        if (_ctx.Health != null)
            _ctx.Health.OnPreDeath += TryPreventDeath;
    }

    public void ResetRuntime()
    {
        _used = false;
        if (_ctx.Health != null)
            _ctx.Health.OnPreDeath -= TryPreventDeath;

        // limpiar buff por si quedó colgado en pool
        _ctx.Effects?.RemoveSpeedMultiplier(this);
    }

    public void Tick(float dt) { }
    public void OnDamaged(float dmg, float currentHP, object source) { }

    public void OnDeath()
    {
        // Cuando de verdad muere, desuscribimos y limpiamos buff
        if (_ctx.Health != null)
            _ctx.Health.OnPreDeath -= TryPreventDeath;
        _ctx.Effects?.RemoveSpeedMultiplier(this);
    }

    public void OnArrivedToCore()
    {
        if (_ctx.Health != null)
            _ctx.Health.OnPreDeath -= TryPreventDeath;
        _ctx.Effects?.RemoveSpeedMultiplier(this);
    }

    public void OnWaypoint(int index) { }

    // ---------- Núcleo de la habilidad ----------
    private bool TryPreventDeath()
    {
        if (_used && onlyOncePerSpawn)
            return false; // ya la usamos → dejar que muera

        _used = true;

        float max = Mathf.Max(1f, _ctx.Health.GetMaxHealth());
        float newHP = Mathf.Max(1f, max * revivePercent);

        // Curar al 50% de la vida máx
        _ctx.Health.SetCurrentHealth(newHP);

        // Buff de velocidad x5 (o lo que pongas)
        if (_ctx.Effects != null)
        {
            if (speedBuffDuration > 0f)
                _ctx.Effects.AddSpeedMultiplier(this, speedMultiplier, speedBuffDuration);
            else
                _ctx.Effects.AddSpeedMultiplier(this, speedMultiplier, 9999f); // "hasta morir"
        }

        // FX opcional
        if (reviveVfx != null)
        {
            var vfx = Instantiate(reviveVfx, _ctx.Transform.position, Quaternion.identity, _ctx.Transform);
            Destroy(vfx, 2.0f);
        }

        // MUY IMPORTANTE: devolvemos true → EnemyHealth.NO llama a Die()
        return true;
    }
}
