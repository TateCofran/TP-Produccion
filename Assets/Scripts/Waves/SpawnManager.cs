using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Spawnea enemigos en base a las recetas de WaveManager,
/// utilizando la lógica de spawns y rutas del GridGenerator:
/// - Toma spawns con GetNextSpawnRoundRobin()
/// - Pide ruta Exit->Core con TryGetRouteExitToCore()
/// - Instancia desde EnemyPool y hace Enemy.Init(route)
/// También notifica muertes a WaveManager.
/// </summary>
[DisallowMultipleComponent]
public class SpawnManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private WaveManager waveManager;   // arrastrá tu WaveManager
    [SerializeField] private GridGenerator grid;        // arrastrá tu GridGenerator (el de tu código)
    [SerializeField] private EnemyPool enemyPool;       // arrastrá tu EnemyPool

    [Header("Spawn")]
    [Tooltip("Altura extra al spawnear (si el Grid no provee su propio offset).")]
    [SerializeField] private float defaultSpawnYOffset = 0.05f;

    [Tooltip("Si falla construir la ruta con el Grid, usar línea recta Exit→Core.")]
    [SerializeField] private bool fallbackLineToCore = true;

    [Tooltip("Hacer un yield (frame) entre steps para evitar micro-freezes.")]
    [SerializeField] private bool yieldBetweenSteps = true;

    private Coroutine runningWaveCo;

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

    private void HandleAnyEnemyKilled(Enemy e)
    {
        if (waveManager) waveManager.NotifyEnemyKilled();
    }

    private void HandleWaveStarted(int waveNumber, int totalEnemies)
    {
        var list = waveManager ? waveManager.RecipeList : null;
        if (!list || list.waveRecipes == null || list.waveRecipes.Count == 0)
        {
            Debug.LogWarning("[SpawnManager] WaveRecipeList no asignado o vacío en WaveManager.");
            return;
        }

        var recipe = list.waveRecipes.FirstOrDefault(r => r.waveNumber == waveNumber);
        if (recipe == null)
        {
            Debug.LogWarning($"[SpawnManager] No hay receta para la oleada {waveNumber}.");
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
            Debug.LogError("[SpawnManager] Falta EnemyPool.");
            yield break;
        }
        if (!grid)
        {
            Debug.LogError("[SpawnManager] Falta GridGenerator.");
            yield break;
        }

        foreach (var step in recipe.steps)
        {
            int toSpawn = Mathf.Max(0, Mathf.CeilToInt(step.count * (waveManager ? waveManager.EnemyCountMultiplier : 1f)));
            float interval = Mathf.Max(0f, step.interval);
            float waitAfter = Mathf.Max(0f, step.waitAfterStep);

            for (int i = 0; i < toSpawn; i++)
            {
                SpawnOne(step.enemyType);
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
        // 1) Elegimos EXIT desde el Grid (round-robin natural de tu sistema)
        Vector3 exit = grid.GetNextSpawnRoundRobin();
        if (exit == Vector3.zero)
        {
            var all = grid.GetSpawnPoints();
            if (all.Count == 0)
            {
                Debug.LogWarning("[SpawnManager] Grid no tiene spawn points disponibles.");
                return;
            }
            exit = all[0];
        }

        // 2) Pedimos ruta Exit→Core al Grid (tu BFS). Si falla, fallback opcional.
        List<Vector3> route;
        bool hasRoute = grid.TryGetRouteExitToCore(exit, out route);
        if (!hasRoute || route == null || route.Count == 0)
        {
            if (!fallbackLineToCore)
            {
                Debug.LogWarning("[SpawnManager] No se pudo construir ruta Exit→Core y fallback desactivado.");
                return;
            }

            route = new List<Vector3>();
            float y = TryGetGridYOffsetOrDefault();
            route.Add(new Vector3(exit.x, y, exit.z));
            var corePos = grid.HasCore() ? grid.GetCorePosition() : exit;
            route.Add(new Vector3(corePos.x, y, corePos.z));
        }

        // 3) Instanciamos desde el pool y llamamos Init(route)
        GameObject go = enemyPool.GetEnemy(type);
        if (!go)
        {
            Debug.LogWarning($"[SpawnManager] EnemyPool devolvió null para {type}.");
            return;
        }

        // Colocamos en el primer punto de la ruta
        go.transform.position = route[0];

        var enemy = go.GetComponent<Enemy>();
        if (!enemy)
        {
            Debug.LogWarning("[SpawnManager] El prefab del pool no tiene componente Enemy.");
            return;
        }

        enemy.Init(route); // Usa exactamente tu firma: Enemy.Init(List<Vector3>)
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

    // -------- utilidades para test rápido --------
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
