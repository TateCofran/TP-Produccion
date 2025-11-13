using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI Pausa")]
    [SerializeField] private GameObject pausePanel;
    [Tooltip("Intentará encontrar el panel por este Tag si 'pausePanel' está vacío al cargar la escena.")]
    [SerializeField] private string pausePanelTag = "PausePanel";
    [Tooltip("Si no hay Tag, probará por nombre exacto en la jerarquía.")]
    [SerializeField] private string pausePanelName = "PausePanel";

    private bool isPaused = false;

    [Header("Victoria")]
    [SerializeField] private string resultScene = "ResultScene";
    [SerializeField] private int wavesToWin = 10;

    [Header("Escenas comunes")]
    [SerializeField] private string mainMenuScene = "Menu";
    [SerializeField] private string gameplayScene = "GameScene";

    public float timePlayed { get; private set; }
    public bool PlayerHasWon { get; private set; }
    public int wavesCompleted { get; private set; }
    public int totalEnemiesKilled { get; private set; }
    public int totalBlueEssences { get; private set; }
    public int totalRedEssences { get; private set; }

    private bool subscribedToWave = false;

    // === DEBUG / TESTEO EN INSPECTOR ===
    [Header("Debug / Testeo")]
    [Tooltip("Oleada a la que saltará el botón de testeo.")]
    [Min(1), SerializeField] private int debugWaveToJump = 1;
    public int DebugWaveToJump
    {
        get => debugWaveToJump;
        set => debugWaveToJump = Mathf.Max(1, value);
    }

    [Tooltip("Tag que se usa para encontrar enemigos al completar la oleada por debug.")]
    [SerializeField] private string enemyTag = "Enemy";

    [Header("Debug Core")]
    [Tooltip("Estado actual del cheat de vida infinita del Core (solo lectura visual).")]
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

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Cada vez que entra a una escena, reintenta enlazar el Pause Panel
        TryBindPausePanel();
        // Si entramos a la escena de juego, reseteamos estado de run
        if (scene.name == gameplayScene)
            ResetRunState(false);

        // Limpiamos referencia al Core y la reobtendremos cuando haga falta
        cachedCore = null;

        StartCoroutine(SubscribeWhenReady());
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
            Debug.Log("[GameManager] Suscrito correctamente a WaveManager.OnWaveEnded.");
        }
    }

    private void Update()
    {
        if (!isPaused)
            timePlayed += Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }
    }

    #region Pause Panel Binding
    /// <summary>
    /// Permite que el panel se registre solo (usado por PausePanelBinder).
    /// </summary>
    public void RegisterPausePanel(GameObject panel)
    {
        pausePanel = panel;
        if (pausePanel != null)
        {
            // Aseguramos estado acorde al flag actual
            pausePanel.SetActive(isPaused);
            Debug.Log("[GameManager] PausePanel registrado por Binder: " + pausePanel.name);
        }
    }

    /// <summary>
    /// Intenta enlazar el Pause Panel si la referencia está vacía.
    /// 1) Por referencia ya serializada
    /// 2) Por Tag (pausePanelTag)
    /// 3) Por nombre exacto (pausePanelName)
    /// </summary>
    private void TryBindPausePanel()
    {
        if (pausePanel != null) return;

        // 1) Intento por Tag
        if (!string.IsNullOrEmpty(pausePanelTag))
        {
            var byTag = GameObject.FindGameObjectWithTag(pausePanelTag);
            if (byTag != null)
            {
                pausePanel = byTag;
                pausePanel.SetActive(isPaused);
                Debug.Log("[GameManager] PausePanel enlazado por Tag: " + pausePanelTag);
                return;
            }
        }

        // 2) Intento por nombre
        if (!string.IsNullOrEmpty(pausePanelName))
        {
            var allRoots = SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var root in allRoots)
            {
                var t = root.transform;
                var found = FindDeepChildByName(t, pausePanelName);
                if (found != null)
                {
                    pausePanel = found.gameObject;
                    pausePanel.SetActive(isPaused);
                    Debug.Log("[GameManager] PausePanel enlazado por Nombre: " + pausePanelName);
                    return;
                }
            }
        }

        Debug.LogWarning("[GameManager] No se pudo enlazar el Pause Panel. " +
                         "Asignalo por Inspector, poné un Tag '" + pausePanelTag +
                         "' o asegurá que exista un objeto llamado '" + pausePanelName + "'.");
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

    #region Pausa
    public void PauseGame()
    {
        if (isPaused) return;
        isPaused = true;
        Time.timeScale = 0f;
        if (pausePanel == null) TryBindPausePanel();
        if (pausePanel != null) pausePanel.SetActive(true);
        AudioListener.pause = true;
    }

    public void ResumeGame()
    {
        if (!isPaused) return;
        isPaused = false;
        Time.timeScale = 1f;
        if (pausePanel == null) TryBindPausePanel();
        if (pausePanel != null) pausePanel.SetActive(false);
        AudioListener.pause = false;
    }
    #endregion

    #region Flujo de partida
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
        if (WaveManager.Instance != null)
            totalEnemiesKilled = WaveManager.Instance.GetTotalEnemiesKilled();

        var essences = FindFirstObjectByType<WorldSwitchPoints>();
        if (essences != null)
        {
            totalBlueEssences = essences.TotalBlue;
            totalRedEssences = essences.TotalRed;

            EssenceBank.Add(totalBlueEssences, totalRedEssences);

            Time.timeScale = 1f;
            AudioListener.pause = false;
            SceneManager.LoadScene(resultScene);
        }
    }
    #endregion

    #region Botones ResultScene
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
            essences.AddBlueEssence(-essences.TotalBlue); //poner a 0
            essences.AddRedEssence(-essences.TotalRed);   //poner a 0
        }

        SceneManager.LoadScene(gameplayScene);
    }
    #endregion

    private void ResetRunState(bool keepTime)
    {
        wavesCompleted = 0;
        if (!keepTime) timePlayed = 0f;
        isPaused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        subscribedToWave = false;
        // No forzamos aquí SetActive(false) porque el panel puede no estar enlazado aún;
        // se alinea en TryBindPausePanel() cuando cargue la escena.
    }

    // ====== DEBUG BUTTONS EXISTENTES ======
    public void Debug_Editor_JumpToWave() => TryJumpToWave(debugWaveToJump);
    public void Debug_Editor_ForceWin() => WinGame();
    public void Debug_Editor_ForceLose() => GameOver();

    private void TryJumpToWave(int waveNumber)
    {
        var wm = WaveManager.Instance;
        if (wm == null)
        {
            Debug.LogWarning("[GameManager] WaveManager.Instance no está disponible. ¿Estás en la escena de juego?");
            return;
        }

        var t = wm.GetType();

        // Intentar métodos comunes con firma (int)
        string[] candidateMethods =
        {
            "JumpToWave","SetCurrentWave","SetWave","StartWaveNumber","StartWave","GoToWave"
        };

        foreach (var name in candidateMethods)
        {
            MethodInfo m = t.GetMethod(name, BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(int) }, null);
            if (m != null)
            {
                m.Invoke(wm, new object[] { waveNumber });
                Debug.Log($"[GameManager] Salto a la oleada {waveNumber} usando {name}.");
                return;
            }
        }

        // Intentar propiedad pública
        string[] candidateProps = { "CurrentWave", "WaveIndex" };
        foreach (var pName in candidateProps)
        {
            PropertyInfo p = t.GetProperty(pName, BindingFlags.Public | BindingFlags.Instance);
            if (p != null && p.CanWrite)
            {
                p.SetValue(wm, waveNumber);
                Debug.Log($"[GameManager] Seteada propiedad {pName} = {waveNumber}. " +
                          "Si tu sistema requiere iniciar/actualizar, llamalo manualmente.");
                return;
            }
        }

        Debug.LogWarning("[GameManager] No encontré una API pública en WaveManager para cambiar de oleada. " +
                         "Agregá en WaveManager: public void JumpToWave(int wave).");
    }

    // ====== NUEVOS BOTONES DEBUG ======

    /// <summary>
    /// Botón de inspector: mata todos los enemigos en pantalla
    /// y completa la oleada actual notificando a WaveManager.
    /// </summary>
    public void Debug_Editor_CompleteCurrentWaveAndKillEnemies()
    {
        var wm = WaveManager.Instance;
        if (wm == null)
        {
            Debug.LogWarning("[GameManager] No se puede completar la oleada: WaveManager.Instance es null.");
            return;
        }

        int enemiesAliveBefore = wm.GetEnemiesAlive();
        if (enemiesAliveBefore <= 0)
        {
            Debug.Log("[GameManager] Debug_CompleteWave: no hay enemigos vivos en esta oleada.");
            return;
        }

        int destroyed = 0;

        if (!string.IsNullOrEmpty(enemyTag))
        {
            var enemies = GameObject.FindGameObjectsWithTag(enemyTag);
            foreach (var e in enemies)
            {
                if (e == null) continue;
                Destroy(e);
                destroyed++;
            }
        }

        // Forzamos a WaveManager a considerar que todos los enemigos murieron
        for (int i = 0; i < enemiesAliveBefore; i++)
        {
            wm.NotifyEnemyKilled();
        }

        Debug.Log($"[GameManager] Debug_CompleteWave: destruidos {destroyed} enemigos (tag '{enemyTag}') " +
                  $"y notificados {enemiesAliveBefore} kills a WaveManager.");
    }

    /// <summary>
    /// Obtiene (y cachea) la referencia al Core en la escena actual.
    /// </summary>
    private Core GetCoreInstance()
    {
        if (cachedCore == null)
        {
            cachedCore = FindFirstObjectByType<Core>();
            if (cachedCore == null)
                Debug.LogWarning("[GameManager] No se encontró un Core en la escena actual.");
        }
        return cachedCore;
    }

    /// <summary>
    /// Botón de inspector: alterna el estado de vida infinita del Core.
    /// </summary>
    public void Debug_Editor_ToggleCoreInfiniteHealth()
    {
        var core = GetCoreInstance();
        if (core == null)
        {
            Debug.LogWarning("[GameManager] No se puede alternar vida infinita: Core no encontrado.");
            return;
        }

        coreInfiniteHealth = !coreInfiniteHealth;
        core.SetInfiniteHealth(coreInfiniteHealth);

        Debug.Log($"[GameManager] Core vida infinita: {(coreInfiniteHealth ? "ACTIVADA" : "DESACTIVADA")}.");
    }

    /// <summary>
    /// Por si querés tener un botón separado de "activar" o "desactivar" directo,
    /// este método se puede usar también desde el editor.
    /// </summary>
    public void Debug_Editor_SetCoreInfiniteHealth(bool enabled)
    {
        var core = GetCoreInstance();
        if (core == null)
        {
            Debug.LogWarning("[GameManager] No se puede setear vida infinita: Core no encontrado.");
            return;
        }

        coreInfiniteHealth = enabled;
        core.SetInfiniteHealth(coreInfiniteHealth);

        Debug.Log($"[GameManager] Core vida infinita seteada a: {coreInfiniteHealth}.");
    }
}
