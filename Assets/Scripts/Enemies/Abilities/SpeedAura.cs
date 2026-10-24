using System.Collections.Generic;
using UnityEngine;

public class Ability_SpeedAura : MonoBehaviour, IEnemyAbility
{
    [Header("Settings")]
    [SerializeField, Min(0f)] private float radius = 4f;
    [SerializeField, Min(1f)] private float speedMultiplier = 1.25f;
    [SerializeField, Min(0.05f)] private float refreshInterval = 0.3f;
    [SerializeField] private bool affectSelf = false;
    [SerializeField] private bool drawGizmos = true;

    private AbilityContext _ctx;
    private float _timer;
    private readonly List<Enemy> _buffer = new();
    private readonly HashSet<Enemy> _affected = new();

    public void Initialize(AbilityContext ctx)
    {
        _ctx = ctx;
        _timer = 0f;
        _affected.Clear();

        CleanupAll();
    }

    public void Tick(float dt)
    {
        _timer -= dt;
        if (_timer > 0f) return;
        _timer = refreshInterval;

        EnemyTracker.GetEnemiesInRangeNonAlloc(_ctx.Transform.position, radius, _buffer);

        EnsureTempHashCapacity();
        _tmpInRange.Clear();

        for (int i = 0; i < _buffer.Count; i++)
        {
            var e = _buffer[i];
            if (e == null) continue;
            if (!affectSelf && e == _ctx.Enemy) continue;

            _tmpInRange.Add(e);

            var eff = e.GetComponent<EnemyEffects>();
            if (eff == null) continue;

            eff.AddSpeedMultiplier(this, speedMultiplier, refreshInterval + 0.05f);

            _affected.Add(e);
        }

        _tmpToRemove.Clear();
        foreach (var e in _affected)
        {
            if (e == null || !_tmpInRange.Contains(e) || !e.gameObject.activeInHierarchy || e.Health.IsDead())
                _tmpToRemove.Add(e);
        }
        for (int i = 0; i < _tmpToRemove.Count; i++)
        {
            var e = _tmpToRemove[i];
            _affected.Remove(e);
            var eff = e ? e.GetComponent<EnemyEffects>() : null;
            eff?.RemoveSpeedMultiplier(this);
        }
        _tmpToRemove.Clear();
    }

    public void OnDamaged(float damage, float currentHP, object source) { }
    public void OnDeath() => CleanupAll();
    public void OnArrivedToCore() => CleanupAll();
    public void OnWaypoint(int index) { }

    public void ResetRuntime()
    {
        _timer = 0f;
        CleanupAll();
    }

    public void OnGetFromPool() { CleanupAll(); }
    public void OnReturnToPool() { CleanupAll(); }

    private void CleanupAll()
    {
        foreach (var e in _affected)
        {
            if (e == null) continue;
            var eff = e.GetComponent<EnemyEffects>();
            eff?.RemoveSpeedMultiplier(this);
        }
        _affected.Clear();
    }

    private static readonly HashSet<Enemy> _tmpInRange = new();
    private static readonly List<Enemy> _tmpToRemove = new(16);
    private static void EnsureTempHashCapacity()
    {
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        Gizmos.color = Color.cyan;

        var old = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(transform.position, Quaternion.identity, Vector3.one);

        Gizmos.DrawWireSphere(Vector3.zero, radius);

        Gizmos.DrawIcon(transform.position, "d_FilterByType@2x", true);

        Gizmos.matrix = old;
    }
}
