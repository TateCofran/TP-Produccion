using System.Collections.Generic;
using UnityEngine;

public class Ability_SpawnMinionsPeriodic : MonoBehaviour, IEnemyAbility
{
    [Header("Spawns")]
    [SerializeField] private EnemyType spawnType = EnemyType.Minion;
    [SerializeField, Min(1)] private int countPerWave = 3;
    [SerializeField, Min(0.1f)] private float periodSeconds = 5f;
    [SerializeField, Min(0f)] private float spreadRadius = 0.75f;
    [SerializeField] private bool spawnOnStart = false;

    [Header("Limits (optional)")]
    [SerializeField, Min(0)] private int maxWaves = 0;
    [SerializeField, Min(0)] private int maxActiveMinions = 0;

    [Header("FX (optional)")]
    [SerializeField] private GameObject spawnVfxPrefab;

    private AbilityContext _ctx;
    private float _timer;
    private int _wavesDone;
    private bool _running;

    public void Initialize(AbilityContext ctx)
    {
        _ctx = ctx;
        _timer = spawnOnStart ? periodSeconds : 0f;
        _wavesDone = 0;
        _running = true;
    }

    public void Tick(float dt)
    {
        if (!_running) return;
        if (maxWaves > 0 && _wavesDone >= maxWaves) return;

        _timer += dt;
        if (_timer < periodSeconds) return;
        _timer = 0f;

        if (maxActiveMinions > 0)
        {
            int active = EnemyTracker.CountEnemiesOfType(spawnType);
            if (active >= maxActiveMinions) return;
        }

        SpawnWave();
        _wavesDone++;
    }

    public void OnDamaged(float dmg, float currentHP, object source) {  }
    public void OnDeath() => _running = false;
    public void OnArrivedToCore() => _running = false;
    public void OnWaypoint(int index) {  }
    public void ResetRuntime()
    {
        _timer = 0f;
        _wavesDone = 0;
        _running = true;
    }

    private void SpawnWave()
    {
        if (EnemyPool.Instance == null) return;

        var pos = _ctx.Transform.position;
        var baseRoute = _ctx.GetRoute != null ? _ctx.GetRoute() : null;
        int wp = _ctx.GetWaypointIndex != null ? _ctx.GetWaypointIndex() : 0;

        List<Vector3> MakeChildRoute(Vector3 start)
        {
            var route = new List<Vector3>(8);
            route.Add(start);
            if (baseRoute != null && baseRoute.Count > 0)
            {
                int from = Mathf.Clamp(wp, 0, baseRoute.Count - 1);
                for (int i = from; i < baseRoute.Count; i++)
                    route.Add(baseRoute[i]);
            }
            return route;
        }

        float angleStep = Mathf.PI * 2f / Mathf.Max(1, countPerWave);
        for (int i = 0; i < countPerWave; i++)
        {
            float a = angleStep * i + Random.Range(-0.25f, 0.25f);
            Vector3 offset = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * spreadRadius;
            Vector3 spawnPos = pos + offset;

            var go = EnemyPool.Instance.GetEnemy(spawnType);
            if (go == null) continue;

            var enemy = go.GetComponent<Enemy>();
            if (enemy == null) { go.transform.position = spawnPos; continue; }

            var route = MakeChildRoute(spawnPos);
            enemy.Init(route);
            WaveManager.Instance?.RegisterExtraEnemies(1);

            if (spawnVfxPrefab != null)
            {
                VfxPool.Spawn(spawnVfxPrefab, spawnPos, Quaternion.identity, 2f);
            }
        }
    }
}
