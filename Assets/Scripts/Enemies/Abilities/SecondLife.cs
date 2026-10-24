using UnityEngine;

public class Ability_SecondLifeBerserk : MonoBehaviour, IEnemyAbility
{
    [Header("Second Life")]
    [SerializeField, Range(0.1f, 1f)] private float revivePercent = 0.5f;
    [SerializeField] private bool onlyOncePerSpawn = true;

    [Header("Speed Buff")]
    [SerializeField, Min(1f)] private float speedMultiplier = 5f;
    [SerializeField] private float speedBuffDuration = 0f;

    [Header("FX (optional)")]
    [SerializeField] private GameObject reviveVfx;

    private AbilityContext _ctx;
    private bool _used;

    public void Initialize(AbilityContext ctx)
    {
        _ctx = ctx;
        _used = false;

        if (_ctx.Health != null)
            _ctx.Health.OnPreDeath += TryPreventDeath;
    }

    public void ResetRuntime()
    {
        _used = false;
        if (_ctx.Health != null)
            _ctx.Health.OnPreDeath -= TryPreventDeath;

        _ctx.Effects?.RemoveSpeedMultiplier(this);
    }

    public void Tick(float dt) { }
    public void OnDamaged(float dmg, float currentHP, object source) { }

    public void OnDeath()
    {
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

    private bool TryPreventDeath()
    {
        if (_used && onlyOncePerSpawn)
            return false;

        _used = true;

        float max = Mathf.Max(1f, _ctx.Health.GetMaxHealth());
        float newHP = Mathf.Max(1f, max * revivePercent);

        _ctx.Health.SetCurrentHealth(newHP);

        if (_ctx.Effects != null)
        {
            if (speedBuffDuration > 0f)
                _ctx.Effects.AddSpeedMultiplier(this, speedMultiplier, speedBuffDuration);
            else
                _ctx.Effects.AddSpeedMultiplier(this, speedMultiplier, 9999f);
        }

        if (reviveVfx != null)
        {
            VfxPool.Spawn(reviveVfx, _ctx.Transform.position, Quaternion.identity, 2.0f, _ctx.Transform);
        }

        return true;
    }
}
