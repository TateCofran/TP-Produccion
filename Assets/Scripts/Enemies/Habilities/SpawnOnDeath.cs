using System.Collections.Generic;
using UnityEngine;

public class Ability_SpawnOnDeath : MonoBehaviour, IEnemyAbility
{
    [Header("Spawn settings")]
    [SerializeField] private EnemyType spawnType = EnemyType.Minion;
    [SerializeField, Min(1)] private int countOnDeath = 3;
    [SerializeField, Min(0f)] private float spreadRadius = 0.75f;

    [Header("Flags")]
    [SerializeField] private bool onlyOnce = true; // por si el enemigo revive, etc.

    [Header("FX (optional)")]
    [SerializeField] private GameObject spawnVfxPrefab;
    [SerializeField, Min(0f)] private float vfxLifetime = 2.0f;

    private AbilityContext _ctx;
    private bool _used;

    public void Initialize(AbilityContext ctx)
    {
        _ctx = ctx;
        _used = false;
    }

    public void Tick(float dt)
    {
        // no-op
    }

    public void OnDamaged(float dmg, float currentHP, object source)
    {
        // no-op
    }

    public void OnDeath()
    {
        if (_used && onlyOnce) return;
        _used = true;

        if (EnemyPool.Instance == null) return;

        Vector3 deathPos = _ctx.Transform.position;
        IReadOnlyList<Vector3> baseRoute = _ctx.GetRoute != null ? _ctx.GetRoute() : null;
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

        float angleStep = Mathf.PI * 2f / Mathf.Max(1, countOnDeath);

        for (int i = 0; i < countOnDeath; i++)
        {
            float a = angleStep * i;
            Vector3 offset = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * spreadRadius;
            Vector3 spawnPos = deathPos + offset;

            GameObject go = EnemyPool.Instance.GetEnemy(spawnType);
            if (go == null) continue;

            var enemy = go.GetComponent<Enemy>();
            if (enemy == null)
            {
                go.transform.position = spawnPos;
            }
            else
            {
                var route = MakeChildRoute(spawnPos);
                enemy.Init(route);
            }

            if (spawnVfxPrefab != null)
            {
                var vfx = Object.Instantiate(spawnVfxPrefab, spawnPos, Quaternion.identity);
                Object.Destroy(vfx, vfxLifetime);
            }
        }
    }

    public void OnArrivedToCore()
    {
        // si quieres que no spawnee cuando llega al core, dejas vacio
    }

    public void OnWaypoint(int index)
    {
        // no-op
    }

    public void ResetRuntime()
    {
        _used = false;
    }
}
