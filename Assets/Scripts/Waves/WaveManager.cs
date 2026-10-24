using System;
using System.Collections;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [Header("Wave Recipes (SO)")]
    [SerializeField] private WaveRecipeList recipeList;
    public WaveRecipeList RecipeList => recipeList;

    [Header("Global Enemy Count Multiplier")]
    [Range(0.5f, 3f)]
    [SerializeField] private float enemyCountMultiplier = 1f;
    public float EnemyCountMultiplier => enemyCountMultiplier;

    [Header("Settings")]
    [SerializeField] private int maxWaves = 45;

    [Header("Autostart")]
    [Tooltip("If enabled, placing a tile or turret while no wave is running starts the next wave.")]
    [SerializeField] private bool autoStartOnPlacement = true;

    [Tooltip("Debounce for several placement events in quick succession.")]

    [SerializeField] private float placementCooldown = 0.15f;

    public int MaxWaves => maxWaves;

    [Header("Next Wave")]
    [SerializeField] private float nextWaveDelaySeconds = 10f;
    private Coroutine nextWaveCountdownCo;

    private float _nextAllowedPlacementStartTime = 0f;

    private int currentWave = 0;
    private int enemiesAlive = 0;
    private int enemiesThisWave = 0;
    private int consecutiveOtherWorldWaves = 0;
    private int totalEnemiesKilled = 0;
    public int GetTotalEnemiesKilled() => totalEnemiesKilled;

    public bool WaveInProgress => enemiesAlive > 0;
    public bool IsFirstWave => isFirstWave;
    public bool IsLastWave() => currentWave >= maxWaves;

    private bool isFirstWave = true;
    private bool waveStarted = false;

    public event Action<int, int> OnWaveStarted;
    public event Action OnWaveEnded;
    public event System.Action<int> OnEnemiesRemainingChanged;
    public event System.Action<float> OnNextWaveCountdownTick;
    public event System.Action OnNextWaveReady;
    public event System.Action<int> OnWaveNumberChanged;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

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

        Debug.Log($"[WaveManager] Autostart from {source}. Current wave: {currentWave}, enemies alive: {enemiesAlive}");
        StartNextWave();
    }

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
            Debug.Log("[WaveManager] The last wave has already been reached");
            return;
        }

        waveStarted = true;
        currentWave++;
        OnWaveNumberChanged?.Invoke(currentWave);

        enemiesThisWave = CalculateEnemiesThisWave();
        enemiesAlive = enemiesThisWave;
        OnEnemiesRemainingChanged?.Invoke(enemiesAlive);

        Debug.Log($"[WaveManager] Wave {currentWave} started with {enemiesThisWave} enemies");
        OnWaveStarted?.Invoke(currentWave, enemiesThisWave);

        TutorialEventHub.RaiseWaveStarted();

        if (nextWaveCountdownCo != null) { StopCoroutine(nextWaveCountdownCo); nextWaveCountdownCo = null; }

        UIController.Instance?.UpdateWave(currentWave, maxWaves);
        UIController.Instance?.UpdateEnemiesRemaining(enemiesAlive);
    }
    public void ForceStartNextWave()
    {
        if (TutorialManager.Instance != null && !TutorialManager.Instance.AllowStartWave)
            return;

        if (waveStarted || WaveInProgress || IsLastWave())
        {
            Debug.Log("[WaveManager] Cannot force a new wave right now");
            return;
        }

        if (nextWaveCountdownCo != null)
        {
            StopCoroutine(nextWaveCountdownCo);
            nextWaveCountdownCo = null;
        }

        Debug.Log("[WaveManager] Starting the next wave manually");
        StartNextWave();
    }
    public void JumpToWave(int wave)
    {
        int target = Mathf.Clamp(wave, 1, maxWaves);

        if (nextWaveCountdownCo != null)
        {
            StopCoroutine(nextWaveCountdownCo);
            nextWaveCountdownCo = null;
        }

        waveStarted = false;
        enemiesAlive = 0;
        enemiesThisWave = 0;
        OnEnemiesRemainingChanged?.Invoke(enemiesAlive);

        currentWave = target - 1;

        UIController.Instance?.UpdateWave(currentWave, maxWaves);
        UIController.Instance?.UpdateEnemiesRemaining(enemiesAlive);

        Debug.Log($"[WaveManager] JumpToWave({wave}): starting wave {target}");
        StartNextWave();
    }

    public void RegisterExtraEnemies(int count)
    {
        if (!waveStarted || count <= 0) return;

        enemiesAlive += count;
        enemiesThisWave += count;

        OnEnemiesRemainingChanged?.Invoke(enemiesAlive);
        UIController.Instance?.UpdateEnemiesRemaining(enemiesAlive);
    }

    public void NotifyEnemyKilled() => NotifyEnemyRemoved(true);

    public void NotifyEnemyRemoved(bool wasKilled)
    {
        enemiesAlive = Mathf.Max(0, enemiesAlive - 1);
        if (wasKilled) totalEnemiesKilled++;

        OnEnemiesRemainingChanged?.Invoke(enemiesAlive);

        UIController.Instance?.UpdateEnemiesRemaining(enemiesAlive);

        if (waveStarted && enemiesAlive <= 0)
        {
            Debug.Log($"[WaveManager] Wave {currentWave} finished");
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
            Debug.LogWarning("[WaveManager] WaveRecipeList is not assigned");
            return 0;
        }

        var recipe = recipeList.waveRecipes.Find(r => r.waveNumber == currentWave);
        if (recipe == null)
        {
            Debug.LogWarning($"[WaveManager] No recipe configured for wave {currentWave}");
            return 0;
        }

        int total = 0;
        foreach (var step in recipe.steps)
            total += Mathf.CeilToInt(step.count * enemyCountMultiplier);

        Debug.Log($"[WaveManager] Total enemies this wave: {total} (x{enemyCountMultiplier})");
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

        UIController.Instance?.UpdateWave(currentWave, maxWaves);
        UIController.Instance?.UpdateEnemiesRemaining(enemiesAlive);
    }
}
