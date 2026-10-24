using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]
public class SpawnManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private GridGenerator grid;
    [SerializeField] private EnemyPool enemyPool;

    [Header("Spawn")]
    [Tooltip("Extra height when spawning (if the grid does not provide its own offset).")]
    [SerializeField] private float defaultSpawnYOffset = 0.05f;

    [Tooltip("If the grid route cannot be built, use a straight line from the exit to the core.")]
    [SerializeField] private bool fallbackLineToCore = true;

    [Tooltip("Enemy types that always spawn from the point with the longest route to the core.")]
    [SerializeField] private List<EnemyType> farthestSpawnTypes = new List<EnemyType> { EnemyType.Boss, EnemyType.Boss2, EnemyType.Boss3 };

    [Tooltip("Yield one frame between steps to avoid micro freezes.")]
    [SerializeField] private bool yieldBetweenSteps = true;

    private Coroutine runningWaveCo;
    private bool spawningForWave;

    private HashSet<EnemyType> shownTypes = new HashSet<EnemyType>();
    [SerializeField] private List<EnemyData> enemyDatabase;
    public event System.Action<EnemyData> OnFirstTimeEnemySpawned;

    private void Reset()
    {
        if (!waveManager) waveManager = FindFirstObjectByType<WaveManager>();
        if (!grid) grid = FindFirstObjectByType<GridGenerator>();
        if (!enemyPool) enemyPool = FindFirstObjectByType<EnemyPool>();
    }

    private void OnEnable()
    {
        if (!waveManager) waveManager = FindFirstObjectByType<WaveManager>();
        if (waveManager)
        {
            waveManager.OnWaveStarted += HandleWaveStarted;
            waveManager.OnWaveEnded += HandleWaveEnded;
        }

        Enemy.OnAnyEnemyKilled += HandleAnyEnemyKilled;
    }

    private void OnDisable()
    {
        if (waveManager)
        {
            waveManager.OnWaveStarted -= HandleWaveStarted;
            waveManager.OnWaveEnded -= HandleWaveEnded;
        }
        Enemy.OnAnyEnemyKilled -= HandleAnyEnemyKilled;
    }

    private EnemyData GetDataFromType(EnemyType type)
    {
        return enemyDatabase.FirstOrDefault(e => e.type == type);
    }

    private void HandleAnyEnemyKilled(Enemy e)
    {
        if (waveManager) waveManager.NotifyEnemyRemoved(!e.ReachedCore);
    }

    private void HandleWaveStarted(int waveNumber, int totalEnemies)
    {
        var list = waveManager ? waveManager.RecipeList : null;
        if (!list || list.waveRecipes == null || list.waveRecipes.Count == 0)
        {
            Debug.LogWarning("[SpawnManager] WaveRecipeList is not assigned or empty on WaveManager");
            return;
        }

        var recipe = list.waveRecipes.FirstOrDefault(r => r.waveNumber == waveNumber);
        if (recipe == null)
        {
            Debug.LogWarning($"[SpawnManager] No recipe for wave {waveNumber}");
            return;
        }

        if (runningWaveCo != null)
            StopCoroutine(runningWaveCo);

        runningWaveCo = StartCoroutine(SpawnRoutine(recipe));
    }

    private void HandleWaveEnded()
    {
        if (runningWaveCo != null)
        {
            StopCoroutine(runningWaveCo);
            runningWaveCo = null;
        }
    }

    private IEnumerator SpawnRoutine(WaveRecipe recipe)
    {
        if (!enemyPool)
        {
            Debug.LogError("[SpawnManager] EnemyPool is missing");
            yield break;
        }
        if (!grid)
        {
            Debug.LogError("[SpawnManager] GridGenerator is missing");
            yield break;
        }

        foreach (var step in recipe.steps)
        {
            int toSpawn = Mathf.Max(0, Mathf.CeilToInt(step.count * (waveManager ? waveManager.EnemyCountMultiplier : 1f)));
            float interval = Mathf.Max(0f, step.interval);
            float waitAfter = Mathf.Max(0f, step.waitAfterStep);

            for (int i = 0; i < toSpawn; i++)
            {
                spawningForWave = true;
                SpawnOne(step.enemyType);
                spawningForWave = false;
                if (interval > 0f) yield return new WaitForSeconds(interval);
                else yield return null;
            }

            if (waitAfter > 0f) yield return new WaitForSeconds(waitAfter);
            if (yieldBetweenSteps) yield return null;
        }

        runningWaveCo = null;
    }

    private void SpawnOne(EnemyType type)
    {
        EnemyData data = GetDataFromType(type);

        if (data != null && !shownTypes.Contains(type))
        {
            shownTypes.Add(type);
            OnFirstTimeEnemySpawned?.Invoke(data);
        }

        bool useFarthest = farthestSpawnTypes != null && farthestSpawnTypes.Contains(type);
        Vector3 exit = useFarthest ? grid.GetFarthestSpawnPoint() : grid.GetNextSpawnRoundRobin();
        if (exit == Vector3.zero)
        {
            var all = grid.GetSpawnPoints();
            if (all.Count == 0)
            {
                Debug.LogWarning("[SpawnManager] The grid has no spawn points available");
                CancelPendingEnemy();
                return;
            }
            exit = all[0];
        }

        List<Vector3> route;
        bool hasRoute = grid.TryGetRouteExitToCore(exit, out route);
        if (!hasRoute || route == null || route.Count == 0)
        {
            if (!fallbackLineToCore)
            {
                Debug.LogWarning("[SpawnManager] Could not build a route from the exit to the core and the fallback is disabled");
                CancelPendingEnemy();
                return;
            }

            route = new List<Vector3>();
            float y = TryGetGridYOffsetOrDefault();
            route.Add(new Vector3(exit.x, y, exit.z));
            var corePos = grid.HasCore() ? grid.GetCorePosition() : exit;
            route.Add(new Vector3(corePos.x, y, corePos.z));
        }

        GameObject go = enemyPool.GetEnemy(type);
        if (!go)
        {
            Debug.LogWarning($"[SpawnManager] EnemyPool returned null for {type}");
            CancelPendingEnemy();
            return;
        }

        go.transform.position = route[0];

        var enemy = go.GetComponent<Enemy>();
        if (!enemy)
        {
            Debug.LogWarning("[SpawnManager] The pooled prefab has no Enemy component");
            enemyPool.ReturnEnemy(go);
            CancelPendingEnemy();
            return;
        }

        enemy.Init(route);
    }

    private void CancelPendingEnemy()
    {
        if (spawningForWave && waveManager) waveManager.NotifyEnemyRemoved(false);
    }

    private float TryGetGridYOffsetOrDefault()
    {
        try
        {
            return grid ? grid.GetRunnerYOffset() : defaultSpawnYOffset;
        }
        catch
        {
            return defaultSpawnYOffset;
        }
    }

    [ContextMenu("Test: Spawn 3 Minions")]
    private void TestSpawn3Minions()
    {
        StartCoroutine(TestBurst(EnemyType.Minion, 3, 0.25f));
    }

    private IEnumerator TestBurst(EnemyType type, int count, float interval)
    {
        for (int i = 0; i < count; i++)
        {
            SpawnOne(type);
            if (interval > 0f) yield return new WaitForSeconds(interval);
            else yield return null;
        }
    }
}
