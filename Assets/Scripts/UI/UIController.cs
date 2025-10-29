using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    public static UIController Instance { get; private set; }

    [Header("Refs - Core/Waves/Enemies")]
    [SerializeField] private TextMeshProUGUI coreHealthText;
    [SerializeField] private TextMeshProUGUI waveText;
    [SerializeField] private TextMeshProUGUI enemiesRemainingText;

    [Header("Next Wave UI")]
    [Tooltip("Texto que muestra la cuenta regresiva hacia la próxima oleada.")]
    [SerializeField] private TextMeshProUGUI nextWaveCountdownText;

    [Tooltip("Botón para iniciar manualmente la próxima oleada.")]
    [SerializeField] private Button nextWaveButton;

    [Tooltip("Contenedor opcional (panel) para mostrar/ocultar todo el bloque de 'Próxima Oleada'.")]
    [SerializeField] private GameObject nextWavePanel;

    private void Awake()
    {
        // Singleton simple
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (nextWaveButton != null)
        {
            nextWaveButton.onClick.RemoveAllListeners();
            nextWaveButton.onClick.AddListener(OnNextWaveButtonClicked);
        }

        // Estado inicial
        SetNextWaveCountdownVisible(false);
        SetNextWaveButtonVisible(true);
        SetNextWaveButtonInteractable(true);
    }

    private void OnEnable()
    {
        var wm = WaveManager.Instance;
        if (wm == null) return;

        wm.OnWaveStarted += HandleWaveStarted;
        wm.OnWaveEnded += HandleWaveEnded;

        wm.OnEnemiesRemainingChanged += HandleEnemiesRemainingChanged;
        wm.OnNextWaveCountdownTick += HandleCountdownTick;
        wm.OnNextWaveReady += HandleNextWaveReady;
        wm.OnWaveNumberChanged += HandleWaveNumberChanged;
    }

    private void OnDisable()
    {
        var wm = WaveManager.Instance;
        if (wm == null) return;

        wm.OnWaveStarted -= HandleWaveStarted;
        wm.OnWaveEnded -= HandleWaveEnded;

        wm.OnEnemiesRemainingChanged -= HandleEnemiesRemainingChanged;
        wm.OnNextWaveCountdownTick -= HandleCountdownTick;
        wm.OnNextWaveReady -= HandleNextWaveReady;
        wm.OnWaveNumberChanged -= HandleWaveNumberChanged;
    }

    // =========================
    //  Core / Wave / Enemies UI
    // =========================

    /// <summary>Actualiza el texto de vida del núcleo.</summary>
    public void UpdateCoreHealth(int currentHealth, int maxHealth)
    {
        if (coreHealthText != null)
            coreHealthText.text = $"Core HP: {currentHealth}/{maxHealth}";
    }

    /// <summary>Actualiza el texto de oleada.</summary>
    public void UpdateWave(int currentWave, int _maxWaves)
    {
        if (waveText != null)
            waveText.text = $"Wave: {currentWave}";
    }

    /// <summary>Actualiza el texto de enemigos restantes.</summary>
    public void UpdateEnemiesRemaining(int count)
    {
        if (enemiesRemainingText != null)
            enemiesRemainingText.text = $"Enemies Left: {count}";
    }

    // =========================
    //  Next Wave (contador + botón)
    // =========================

    public void UpdateNextWaveCountdown(float seconds)
    {
        if (!nextWaveCountdownText) return;

        if (seconds > 0f)
        {
            nextWaveCountdownText.text = $"Next wave in: {seconds:0.0}s";
            SetNextWaveCountdownVisible(true);
        }
        else
        {
            nextWaveCountdownText.text = "Ready!";
            SetNextWaveCountdownVisible(true);
        }
    }

    public void SetNextWaveCountdownVisible(bool visible)
    {
        if (nextWavePanel != null)
            nextWavePanel.SetActive(visible);

        if (nextWaveCountdownText != null && nextWavePanel == null)
            nextWaveCountdownText.gameObject.SetActive(visible);
    }

    public void SetNextWaveButtonInteractable(bool interactable)
    {
        if (nextWaveButton != null)
            nextWaveButton.interactable = interactable;
    }

    public void SetNextWaveButtonVisible(bool visible)
    {
        if (nextWaveButton != null)
        {
            nextWaveButton.gameObject.SetActive(visible);
        }      
    }

    private void OnNextWaveButtonClicked()
    {
        if (WaveManager.Instance == null) return;
        WaveManager.Instance.ForceStartNextWave();
    }

    // =========================
    //  Handlers de eventos
    // =========================

    private void HandleWaveStarted(int waveNumber, int totalEnemies)
    {
        // Actualizo texto de wave y escondo UI de próxima oleada
        UpdateWave(waveNumber, WaveManager.Instance != null ? WaveManager.Instance.MaxWaves : 0);
        SetNextWaveCountdownVisible(false);
        SetNextWaveButtonInteractable(false); // mientras corre la oleada no se fuerza otra
        SetNextWaveButtonVisible(false);
        UpdateEnemiesRemaining(totalEnemies);
    }

    private void HandleWaveEnded()
    {
        // Terminó la oleada: mostramos countdown y deshabilitamos botón hasta que esté "Ready!"
        SetNextWaveCountdownVisible(true);
        SetNextWaveButtonVisible(true);
        SetNextWaveButtonInteractable(true);   // <— habilitado apenas termina la oleada
    }

    private void HandleEnemiesRemainingChanged(int enemiesLeft)
    {
        UpdateEnemiesRemaining(enemiesLeft);
    }

    private void HandleCountdownTick(float secondsLeft)
    {
        UpdateNextWaveCountdown(secondsLeft);
        // El botón se habilita cuando el manager dispara OnNextWaveReady
    }

    private void HandleNextWaveReady()
    {
        UpdateNextWaveCountdown(0f);
        SetNextWaveButtonInteractable(true);
    }

    private void HandleWaveNumberChanged(int waveNumber)
    {
        UpdateWave(waveNumber, WaveManager.Instance != null ? WaveManager.Instance.MaxWaves : 0);
    }
}
