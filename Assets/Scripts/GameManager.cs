using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Pause UI")]
    [SerializeField] private GameObject pausePanel;
    [Tooltip("Tag used to find the pause panel when 'pausePanel' is empty after a scene load.")]
    [SerializeField] private string pausePanelTag = "PausePanel";
    [Tooltip("Exact object name used as a fallback when the tag is not found.")]
    [SerializeField] private string pausePanelName = "PausePanel";

    private bool isPaused = false;

    [Header("Victory")]
    [SerializeField] private string resultScene = "ResultScene";
    [SerializeField] private int wavesToWin = 10;

    [Header("Scenes")]
    [SerializeField] private string mainMenuScene = "Menu";
    [SerializeField] private string gameplayScene = "GameScene";

    public float timePlayed { get; private set; }
    public bool PlayerHasWon { get; private set; }
    public int wavesCompleted { get; private set; }
    public int totalEnemiesKilled { get; private set; }
    public int totalBlueEssences { get; private set; }
    public int totalRedEssences { get; private set; }

    public bool IsPauseMenuOpen => isPaused;

    private bool subscribedToWave = false;
    private bool runEnded = false;
    private Coroutine subscribeRoutine;

    [Header("Debug")]
    [Tooltip("Wave the debug button jumps to.")]
    [Min(1), SerializeField] private int debugWaveToJump = 1;
    public int DebugWaveToJump
    {
        get => debugWaveToJump;
        set => debugWaveToJump = Mathf.Max(1, value);
    }

    [Header("Debug Core")]
    [Tooltip("Current state of the core infinite health cheat (read only).")]
    [SerializeField] private bool coreInfiniteHealth = false;

    private Core cachedCore;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        if (WaveManager.Instance != null)
            WaveManager.Instance.OnWaveEnded -= NotifyWaveCompleted;
    }

    private bool IsGameplayScene => SceneManager.GetActiveScene().name == gameplayScene;

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryBindPausePanel();
        if (scene.name == gameplayScene)
            ResetRunState(false);

        cachedCore = null;

        if (subscribeRoutine != null)
        {
            StopCoroutine(subscribeRoutine);
            subscribeRoutine = null;
        }

        if (scene.name == gameplayScene)
            subscribeRoutine = StartCoroutine(SubscribeWhenReady());
    }

    private IEnumerator SubscribeWhenReady()
    {
        while (WaveManager.Instance == null)
            yield return null;

        if (!subscribedToWave)
        {
            WaveManager.Instance.OnWaveEnded -= NotifyWaveCompleted;
            WaveManager.Instance.OnWaveEnded += NotifyWaveCompleted;
            subscribedToWave = true;
        }

        subscribeRoutine = null;
    }

    private void Update()
    {
        if (!GameTime.IsPaused)
            timePlayed += Time.unscaledDeltaTime;

        if (!IsGameplayScene) return;

        if (Input.GetKeyDown(KeyCode.Escape))
            HandleEscape();
    }

    private void HandleEscape()
    {
        if (isPaused)
        {
            ResumeGame();
            return;
        }

        if (EscapeRouter.TryConsume()) return;

        PauseGame();
    }

    #region Pause Panel Binding

    public void RegisterPausePanel(GameObject panel)
    {
        pausePanel = panel;
        if (pausePanel != null)
            pausePanel.SetActive(isPaused);
    }

    private void TryBindPausePanel()
    {
        if (pausePanel != null) return;

        if (!string.IsNullOrEmpty(pausePanelTag))
        {
            var byTag = GameObject.FindGameObjectWithTag(pausePanelTag);
            if (byTag != null)
            {
                pausePanel = byTag;
                pausePanel.SetActive(isPaused);
                return;
            }
        }

        if (!string.IsNullOrEmpty(pausePanelName))
        {
            var allRoots = SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var root in allRoots)
            {
                var found = FindDeepChildByName(root.transform, pausePanelName);
                if (found != null)
                {
                    pausePanel = found.gameObject;
                    pausePanel.SetActive(isPaused);
                    return;
                }
            }
        }

        if (IsGameplayScene)
        {
            Debug.LogWarning("[GameManager] Could not bind the pause panel. Assign it in the inspector, tag it '" +
                             pausePanelTag + "' or make sure an object named '" + pausePanelName + "' exists.");
        }
    }

    private Transform FindDeepChildByName(Transform parent, string childName)
    {
        if (parent.name == childName) return parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            var result = FindDeepChildByName(parent.GetChild(i), childName);
            if (result != null) return result;
        }
        return null;
    }

    #endregion

    #region Pause

    public void PauseGame()
    {
        if (isPaused) return;
        isPaused = true;
        GameTime.RequestPause();
        if (pausePanel == null) TryBindPausePanel();
        if (pausePanel != null) pausePanel.SetActive(true);
    }

    public void ResumeGame()
    {
        if (!isPaused) return;
        isPaused = false;
        GameTime.ReleasePause();
        if (pausePanel == null) TryBindPausePanel();
        if (pausePanel != null) pausePanel.SetActive(false);
    }

    #endregion

    #region Run Flow

    public void NotifyWaveCompleted()
    {
        wavesCompleted++;
        if (wavesCompleted >= wavesToWin)
            WinGame();
    }

    public void GameOver()
    {
        PlayerHasWon = false;
        EndRun();
    }

    private void WinGame()
    {
        PlayerHasWon = true;
        EndRun();
    }

    private void EndRun()
    {
        if (runEnded) return;
        runEnded = true;

        if (WaveManager.Instance != null)
            totalEnemiesKilled = WaveManager.Instance.GetTotalEnemiesKilled();

        var essences = FindFirstObjectByType<WorldSwitchPoints>();
        if (essences != null)
        {
            totalBlueEssences = essences.TotalBlue;
            totalRedEssences = essences.TotalRed;
            EssenceBank.Add(totalBlueEssences, totalRedEssences);
        }
        else
        {
            totalBlueEssences = 0;
            totalRedEssences = 0;
            Debug.LogWarning("[GameManager] WorldSwitchPoints not found. The run ends without essences.");
        }

        isPaused = false;
        GameTime.ResetAll();
        SceneManager.LoadScene(resultScene);
    }

    #endregion

    #region Result Scene Buttons

    public void OnMainMenuButton()
    {
        ResetRunState(false);
        WaveManager.Instance?.ResetWaves();
        SceneManager.LoadScene(mainMenuScene);
    }

    public void OnPlayAgainButton()
    {
        ResetRunState(false);
        WaveManager.Instance?.ResetWaves();
        var essences = FindFirstObjectByType<WorldSwitchPoints>();
        if (essences != null)
        {
            essences.AddBlueEssence(-essences.TotalBlue);
            essences.AddRedEssence(-essences.TotalRed);
        }

        SceneManager.LoadScene(gameplayScene);
    }

    #endregion

    private void ResetRunState(bool keepTime)
    {
        wavesCompleted = 0;
        if (!keepTime) timePlayed = 0f;
        isPaused = false;
        GameTime.ResetAll();
        subscribedToWave = false;
        runEnded = false;
    }

    #region Debug

    public void Debug_Editor_JumpToWave() => TryJumpToWave(debugWaveToJump);
    public void Debug_Editor_ForceWin() => WinGame();
    public void Debug_Editor_ForceLose() => GameOver();

    private void TryJumpToWave(int waveNumber)
    {
        var wm = WaveManager.Instance;
        if (wm == null)
        {
            Debug.LogWarning("[GameManager] WaveManager.Instance is not available. Are you in the gameplay scene");
            return;
        }

        wm.JumpToWave(waveNumber);
        Debug.Log($"[GameManager] Jumped to wave {waveNumber}");
    }

    public void Debug_Editor_CompleteCurrentWaveAndKillEnemies()
    {
        var wm = WaveManager.Instance;
        if (wm == null)
        {
            Debug.LogWarning("[GameManager] Cannot complete the wave: WaveManager.Instance is null");
            return;
        }

        if (wm.GetEnemiesAlive() <= 0)
        {
            Debug.Log("[GameManager] Debug_CompleteWave: there are no enemies alive in this wave");
            return;
        }

        var enemies = new List<Enemy>(EnemyTracker.Enemies);
        int killed = 0;
        foreach (var e in enemies)
        {
            if (e == null) continue;
            e.NotifyDeath();
            killed++;
        }

        int remaining = wm.GetEnemiesAlive();
        for (int i = 0; i < remaining; i++)
            wm.NotifyEnemyRemoved(false);

        Debug.Log($"[GameManager] Debug_CompleteWave: {killed} enemies returned to the pool, {remaining} pending enemies discounted");
    }

    private Core GetCoreInstance()
    {
        if (cachedCore == null)
        {
            cachedCore = FindFirstObjectByType<Core>();
            if (cachedCore == null)
                Debug.LogWarning("[GameManager] No Core found in the current scene");
        }
        return cachedCore;
    }

    public void Debug_Editor_ToggleCoreInfiniteHealth()
    {
        var core = GetCoreInstance();
        if (core == null)
        {
            Debug.LogWarning("[GameManager] Cannot toggle infinite health: Core not found");
            return;
        }

        coreInfiniteHealth = !coreInfiniteHealth;
        core.SetInfiniteHealth(coreInfiniteHealth);

        Debug.Log($"[GameManager] Core infinite health: {(coreInfiniteHealth ? "ON" : "OFF")}");
    }

    public void Debug_Editor_SetCoreInfiniteHealth(bool enabled)
    {
        var core = GetCoreInstance();
        if (core == null)
        {
            Debug.LogWarning("[GameManager] Cannot set infinite health: Core not found");
            return;
        }

        coreInfiniteHealth = enabled;
        core.SetInfiniteHealth(coreInfiniteHealth);

        Debug.Log($"[GameManager] Core infinite health set to {coreInfiniteHealth}");
    }

    #endregion
}
