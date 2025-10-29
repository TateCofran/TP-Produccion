using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Ability_SpeedAura : MonoBehaviour, IEnemyAbility
{
    [Header("Ajustes")]
    [SerializeField, Min(0f)] private float radius = 4f;
    [SerializeField, Min(1f)] private float speedMultiplier = 1.25f; // 25% más rápido
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
        // seguridad por pooling: si algo quedó, lo limpiamos
        CleanupAll();
    }

    public void Tick(float dt)
    {
        _timer -= dt;
        if (_timer > 0f) return;
        _timer = refreshInterval;

        // 1) Buscar objetivos en rango
        EnemyTracker.GetEnemiesInRangeNonAlloc(_ctx.Transform.position, radius, _buffer);

        // Set temporal para saber quién quedó en rango este tick
        // (evita LINQ / allocs)
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

            // 2) Aplicar/refresh del buff con pequeña holgura > refreshInterval
            eff.AddSpeedMultiplier(this, speedMultiplier, refreshInterval + 0.05f);

            // trackeamos afectados (para limpieza si salen del rango o muere el caster)
            _affected.Add(e);
        }

        // 3) Remover de los que ya NO están en rango
        // iterar sobre copia para poder modificar el HashSet
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
            eff?.RemoveSpeedMultiplier(this); // quitamos nuestro aporte
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

    // === IPoolable ===
    public void OnGetFromPool() { CleanupAll(); }
    public void OnReturnToPool() { CleanupAll(); }

    private void CleanupAll()
    {
        // remover nuestro multiplicador de todos los afectados
        foreach (var e in _affected)
        {
            if (e == null) continue;
            var eff = e.GetComponent<EnemyEffects>();
            eff?.RemoveSpeedMultiplier(this);
        }
        _affected.Clear();
    }

    // ===== utilidades GC-free =====
    private static readonly HashSet<Enemy> _tmpInRange = new();
    private static readonly List<Enemy> _tmpToRemove = new(16);
    private static void EnsureTempHashCapacity()
    {
        // nada: los HashSet se autogestionan; esto queda de placeholder si querés hacer pre-alloc
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        // color más visible
        Gizmos.color = Color.cyan;

        // por si el objeto está escalado, usamos matriz identidad en escala pero movida a la posición
        var old = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(transform.position, Quaternion.identity, Vector3.one);

        Gizmos.DrawWireSphere(Vector3.zero, radius);

        // opcional: un ícono para ubicarlo a simple vista
        Gizmos.DrawIcon(transform.position, "d_FilterByType@2x", true);

        Gizmos.matrix = old;
    }
}
