using System.Collections.Generic;
using UnityEngine;

public class Ability_LinkedShield : MonoBehaviour, IEnemyAbility
{
    [Header("Minions vinculados")]
    [SerializeField] private EnemyType linkedMinionType = EnemyType.Minion;
    [SerializeField, Min(1)] private int minionCount = 5;
    [SerializeField, Min(0f)] private float spawnSpreadRadius = 1.5f;

    [Header("FX (opcional)")]
    [SerializeField] private GameObject spawnVfxPrefab;
    [SerializeField] private GameObject shieldActiveVfxPrefab;  // por ejemplo, un aura grande
    [SerializeField, Min(0f)] private float vfxLifetime = 2f;

    private AbilityContext _ctx;
    private readonly HashSet<Enemy> _linkedMinions = new HashSet<Enemy>();
    private bool _shieldActive;
    private GameObject _shieldVfxInstance;

    public void Initialize(AbilityContext ctx)
    {
        _ctx = ctx;
        _linkedMinions.Clear();
        _shieldActive = false;

        if (_ctx.Health != null)
        {
            _ctx.Health.SetInvulnerable(true);
            _shieldActive = true;
        }

        SpawnLinkedMinions();

        Enemy.OnAnyEnemyKilled += OnAnyEnemyKilled;

        if (shieldActiveVfxPrefab != null && _ctx.Transform != null)
        {
            _shieldVfxInstance = Object.Instantiate(
                shieldActiveVfxPrefab,
                _ctx.Transform.position,
                Quaternion.identity,
                _ctx.Transform
            );
        }
    }

    public void Tick(float dt)
    {
        // Si queres que el VFX siga la posicion, ya es hijo del enemigo, asi que no hace falta nada aca
    }

    public void OnDamaged(float dmg, float currentHP, object source)
    {
        // no-op
    }

    public void OnDeath()
    {
        Cleanup();
    }

    public void OnArrivedToCore()
    {
        Cleanup();
    }

    public void OnWaypoint(int index)
    {
        // no-op
    }

    public void ResetRuntime()
    {
        Cleanup();
    }

    private void Cleanup()
    {
        Enemy.OnAnyEnemyKilled -= OnAnyEnemyKilled;

        if (_ctx.Health != null)
            _ctx.Health.SetInvulnerable(false);

        if (_shieldVfxInstance != null)
        {
            Object.Destroy(_shieldVfxInstance);
            _shieldVfxInstance = null;
        }

        _linkedMinions.Clear();
        _shieldActive = false;
    }

    private void SpawnLinkedMinions()
    {
        if (EnemyPool.Instance == null || _ctx.Transform == null) return;

        Vector3 bossPos = _ctx.Transform.position;
        var baseRoute = _ctx.GetRoute != null ? _ctx.GetRoute() : null;
        int wpIndex = _ctx.GetWaypointIndex != null ? _ctx.GetWaypointIndex() : 0;

        List<Vector3> MakeChildRoute(Vector3 start)
        {
            var route = new List<Vector3>(8);
            route.Add(start);

            if (baseRoute != null && baseRoute.Count > 0)
            {
                int from = Mathf.Clamp(wpIndex, 0, baseRoute.Count - 1);
                for (int i = from; i < baseRoute.Count; i++)
                    route.Add(baseRoute[i]);
            }

            return route;
        }

        float angleStep = Mathf.PI * 2f / Mathf.Max(1, minionCount);

        for (int i = 0; i < minionCount; i++)
        {
            float angle = angleStep * i;
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * spawnSpreadRadius;
            Vector3 spawnPos = bossPos + offset;

            GameObject go = EnemyPool.Instance.GetEnemy(linkedMinionType);
            if (go == null) continue;

            Enemy enemy = go.GetComponent<Enemy>();
            if (enemy != null)
            {
                var route = MakeChildRoute(spawnPos);
                enemy.Init(route);
                _linkedMinions.Add(enemy);
            }
            else
            {
                go.transform.position = spawnPos;
            }

            if (spawnVfxPrefab != null)
            {
                var vfx = Object.Instantiate(spawnVfxPrefab, spawnPos, Quaternion.identity);
                Object.Destroy(vfx, vfxLifetime);
            }
        }
    }

    private void OnAnyEnemyKilled(Enemy e)
    {
        if (!_shieldActive || e == null) return;
        if (!_linkedMinions.Contains(e)) return;

        _linkedMinions.Remove(e);

        if (_linkedMinions.Count == 0)
        {
            _shieldActive = false;

            if (_ctx.Health != null)
                _ctx.Health.SetInvulnerable(false);

            if (_shieldVfxInstance != null)
            {
                Object.Destroy(_shieldVfxInstance);
                _shieldVfxInstance = null;
            }

            // Aqui se podria meter un VFX de "escudo roto"
        }
    }
}
