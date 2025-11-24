using System;
using System.Collections;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [Header("Receta de oleadas (SO)")]
    [SerializeField] private WaveRecipeList recipeList;
    public WaveRecipeList RecipeList => recipeList;

    [Header("Multiplicador global de cantidad de enemigos")]
    [Range(0.5f, 3f)]
    [SerializeField] private float enemyCountMultiplier = 1f;
    public float EnemyCountMultiplier => enemyCountMultiplier;

    [Header("Configuración")]
    [SerializeField] private int maxWaves = 45;

    [Header("Autostart")]
    [Tooltip("Si está activo, al colocar un tile/torreta con éxito y no hay oleada en curso, arranca la siguiente.")]
    [SerializeField] private bool autoStartOnPlacement = true;

    [Tooltip("Anti-rebote para múltiples eventos de colocación casi simultáneos.")]

    [SerializeField] private float placementCooldown = 0.15f;

    public int MaxWaves => maxWaves;

    // ===== Delay para la próxima oleada (contador en UI) =====
    [Header("Próxima oleada")]
    [SerializeField] private float nextWaveDelaySeconds = 10f;   // ajustá desde el inspector
    private Coroutine nextWaveCountdownCo;

    private float _nextAllowedPlacementStartTime = 0f;

    private int currentWave = 0;
    private int enemiesAlive = 0;
    private int enemiesThisWave = 0;
    private int consecutiveOtherWorldWaves = 0;
    private int totalEnemiesKilled = 0; // total de enemigos eliminados durante la partida
    public int GetTotalEnemiesKilled() => totalEnemiesKilled;

    public bool WaveInProgress => enemiesAlive > 0;
    public bool IsFirstWave => isFirstWave;
    public bool IsLastWave() => currentWave >= maxWaves;

    private bool isFirstWave = true;
    private bool waveStarted = false;

    public event Action<int, int> OnWaveStarted;
    public event Action OnWaveEnded; 
    public event System.Action<int> OnEnemiesRemainingChanged;  // enemigos vivos/restantes
    public event System.Action<float> OnNextWaveCountdownTick;  // segundos restantes para la próxima
    public event System.Action OnNextWaveReady;                 // countdown terminó, listo para botón
    public event System.Action<int> OnWaveNumberChanged;        // si cambiás la wave fuera de Start

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // UI inicial (oleada 0 / enemigos 0)
        UIController.Instance?.UpdateWave(currentWave, maxWaves);
        UIController.Instance?.UpdateEnemiesRemaining(enemiesAlive);
    }

    private void OnEnable()
    {
        PlacementEvents.OnTurretPlaced += HandleTurretPlaced;
        PlacementEvents.OnTileApplied += HandleTileApplied;
    }

    private void OnDisable()
    {
        PlacementEvents.OnTurretPlaced -= HandleTurretPlaced;
        PlacementEvents.OnTileApplied -= HandleTileApplied;
    }

    // ====== AUTOSTART DESDE COLOCACIÓN ======
    private void HandleTurretPlaced(PlacementEvents.TurretPlacedInfo info)
    {
        TryAutoStartAfterPlacement("TurretPlaced");
    }

    private void HandleTileApplied(PlacementEvents.TileAppliedInfo info)
    {
        TryAutoStartAfterPlacement("TileApplied");
    }

    private void TryAutoStartAfterPlacement(string source)
    {
        if (!autoStartOnPlacement) return;

        if (TutorialManager.Instance != null && !TutorialManager.Instance.AllowStartWave)
            return;

        if (Time.unscaledTime < _nextAllowedPlacementStartTime) return;
        _nextAllowedPlacementStartTime = Time.unscaledTime + placementCooldown;

        if (waveStarted || WaveInProgress) return;
        if (IsLastWave()) return;

        Debug.Log($"[WaveManager] Autostart por {source}. Oleada actual: {currentWave}, enemigos vivos: {enemiesAlive}.");
        StartNextWave();
    }

    // ====== API PÚBLICA ======
    public void TryStartFirstWave()
    {
        if (!waveStarted && currentWave == 0)
            StartNextWave();
    }

    public void StartNextWave()
    {
        if (waveStarted) return;
        if (IsLastWave())
        {
            Debug.Log("[WaveManager] Ya se alcanzó la última oleada.");
            return;
        }

        waveStarted = true;
        currentWave++;
        OnWaveNumberChanged?.Invoke(currentWave);

        enemiesThisWave = CalculateEnemiesThisWave();
        enemiesAlive = enemiesThisWave;
        OnEnemiesRemainingChanged?.Invoke(enemiesAlive);

        Debug.Log($"[WaveManager] Oleada {currentWave} iniciada con {enemiesThisWave} enemigos.");
        OnWaveStarted?.Invoke(currentWave, enemiesThisWave);

        TutorialEventHub.RaiseWaveStarted();


        // (si tenías un countdown en curso, lo cancelás)
        if (nextWaveCountdownCo != null) { StopCoroutine(nextWaveCountdownCo); nextWaveCountdownCo = null; }


        // UI: mostrar oleada y enemigos al inicio
        UIController.Instance?.UpdateWave(currentWave, maxWaves);
        UIController.Instance?.UpdateEnemiesRemaining(enemiesAlive);

    }
    public void ForceStartNextWave()
    {
        if (TutorialManager.Instance != null && !TutorialManager.Instance.AllowStartWave)
            return;
        // Si hay una oleada activa o ya se alcanzó la última, no hacemos nada
        if (waveStarted || WaveInProgress || IsLastWave())
        {
            Debug.Log("[WaveManager] No se puede forzar una nueva oleada ahora.");
            return;
        }

        // Cancelar countdown si estaba corriendo
        if (nextWaveCountdownCo != null)
        {
            StopCoroutine(nextWaveCountdownCo);
            nextWaveCountdownCo = null;
        }

        Debug.Log("[WaveManager] Iniciando siguiente oleada manualmente (botón StartWaveButton).");
        StartNextWave();
    }
    public void JumpToWave(int wave)
    {
        int target = Mathf.Clamp(wave, 1, maxWaves);

        // Cancelar cualquier countdown hacia la próxima
        if (nextWaveCountdownCo != null)
        {
            StopCoroutine(nextWaveCountdownCo);
            nextWaveCountdownCo = null;
        }

        // Reset de estado de oleada actual
        waveStarted = false;
        enemiesAlive = 0;
        enemiesThisWave = 0;
        OnEnemiesRemainingChanged?.Invoke(enemiesAlive);

        // Ajustar índice: StartNextWave() incrementa, por eso dejamos currentWave en target - 1
        currentWave = target - 1;

        // UI provisional antes de iniciar (muestra el índice actual)
        UIController.Instance?.UpdateWave(currentWave, maxWaves);
        UIController.Instance?.UpdateEnemiesRemaining(enemiesAlive);

        Debug.Log($"[WaveManager] JumpToWave({wave}) → preparando e iniciando oleada {target}.");
        StartNextWave();
    }

    public void NotifyEnemyKilled()
    {
        enemiesAlive = Mathf.Max(0, enemiesAlive - 1);
        totalEnemiesKilled++;

        OnEnemiesRemainingChanged?.Invoke(enemiesAlive);

        //UI: actualizar enemigos restantes
        UIController.Instance?.UpdateEnemiesRemaining(enemiesAlive);

        if (waveStarted && enemiesAlive <= 0)
        {
            Debug.Log($"[WaveManager] Oleada {currentWave} finalizada.");
            waveStarted = false;
            OnWaveEnded?.Invoke();

            TutorialEventHub.RaiseWaveCompleted();

            if (nextWaveCountdownCo != null) StopCoroutine(nextWaveCountdownCo);
            nextWaveCountdownCo = StartCoroutine(NextWaveCountdownRoutine());
        }
    }
    private IEnumerator NextWaveCountdownRoutine()
    {
        float t = nextWaveDelaySeconds;
        while (t > 0f)
        {
            OnNextWaveCountdownTick?.Invoke(t);
            yield return null;
            t -= Time.deltaTime;
        }

        OnNextWaveCountdownTick?.Invoke(0f);
        OnNextWaveReady?.Invoke();
        nextWaveCountdownCo = null;
    }
    public int GetCurrentWave() => currentWave;
    public int GetEnemiesAlive() => enemiesAlive;
    public int GetEnemiesThisWave() => enemiesThisWave;

    private int CalculateEnemiesThisWave()
    {
        if (recipeList == null)
        {
            Debug.LogWarning("[WaveManager] No se asignó el WaveRecipeList.");
            return 0;
        }

        var recipe = recipeList.waveRecipes.Find(r => r.waveNumber == currentWave);
        if (recipe == null)
        {
            Debug.LogWarning($"No hay receta configurada para la ronda {currentWave}");
            return 0;
        }

        int total = 0;
        foreach (var step in recipe.steps)
            total += Mathf.CeilToInt(step.count * enemyCountMultiplier);

        Debug.Log($"[WaveManager] Total enemigos esta oleada: {total} (SO + x{enemyCountMultiplier})");
        return total;
    }

    public void SetEnemyCountMultiplier(float value)
        => enemyCountMultiplier = Mathf.Clamp(value, 0.5f, 3f);

    public void ResetWaves()
    {
        currentWave = 0;
        enemiesAlive = 0;
        enemiesThisWave = 0;
        waveStarted = false;
        totalEnemiesKilled = 0; 

        // UI reset
        UIController.Instance?.UpdateWave(currentWave, maxWaves);
        UIController.Instance?.UpdateEnemiesRemaining(enemiesAlive);
    }
}
