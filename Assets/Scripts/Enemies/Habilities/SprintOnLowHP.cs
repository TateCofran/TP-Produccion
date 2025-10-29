using UnityEngine;

public class Ability_SprintOnLowHP : MonoBehaviour, IEnemyAbility
{
    [SerializeField] private float threshold = 0.5f;
    [SerializeField] private float multiplier = 1.8f;
    [SerializeField] private float duration = 2.5f;
    [SerializeField] private float cooldown = 5f;

    private AbilityContext _ctx;
    private bool _active;
    private float _activeTimer;
    private float _cdTimer;

    public void Initialize(AbilityContext ctx)
    {
        _ctx = ctx;
        _active = false;
        _cdTimer = 0;
    }

    public void Tick(float dt)
    {
        if (_cdTimer > 0) _cdTimer -= dt;

        if (_active)
        {
            _activeTimer -= dt;
            if (_activeTimer <= 0f)
            {
                _active = false;
                _ctx.Effects.RemoveSpeedMultiplier(this);
            }
        }
    }

    public void OnDamaged(float dmg, float currentHP, object source)
    {
        if (_active || _cdTimer > 0) return;

        float max = _ctx.Health.GetMaxHealth();
        if (currentHP <= max * threshold)
        {
            _active = true;
            _activeTimer = duration;
            _cdTimer = cooldown;

            _ctx.Effects.AddSpeedMultiplier(this, multiplier, duration);
        }
    }

    public void OnDeath() => _ctx.Effects.RemoveSpeedMultiplier(this);
    public void OnArrivedToCore() => _ctx.Effects.RemoveSpeedMultiplier(this);
    public void OnWaypoint(int index) { }
    public void ResetRuntime()
    {
        _active = false;
        _activeTimer = 0;
        _cdTimer = 0;
        _ctx.Effects?.RemoveSpeedMultiplier(this);
    }
}
