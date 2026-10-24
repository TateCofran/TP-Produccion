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
    [Tooltip("Text that shows the countdown to the next wave.")]
    [SerializeField] private TextMeshProUGUI nextWaveCountdownText;

    [Tooltip("Button that starts the next wave manually.")]
    [SerializeField] private Button nextWaveButton;

    [Tooltip("Optional container that shows or hides the whole next wave block.")]
    [SerializeField] private GameObject nextWavePanel;

    private void Awake()
    {
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

        SetNextWaveCountdownVisible(false);
        SetNextWaveButtonVisible(true);
        SetNextWaveButtonInteractable(true);
    }
    private void Start()
    {
        TrySubscribeWaveManager();
    }

    private void OnEnable()
    {
        TrySubscribeWaveManager();
    }

    private void TrySubscribeWaveManager()
    {
        var wm = WaveManager.Instance;
        if (wm == null) return;

        wm.OnWaveStarted -= HandleWaveStarted;
        wm.OnWaveEnded -= HandleWaveEnded;
        wm.OnEnemiesRemainingChanged -= HandleEnemiesRemainingChanged;
        wm.OnNextWaveCountdownTick -= HandleCountdownTick;
        wm.OnNextWaveReady -= HandleNextWaveReady;
        wm.OnWaveNumberChanged -= HandleWaveNumberChanged;

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

    public void UpdateCoreHealth(int currentHealth, int maxHealth)
    {
        if (coreHealthText != null)
            coreHealthText.text = $"Core HP: {currentHealth}/{maxHealth}";
    }

    public void UpdateWave(int currentWave, int _maxWaves)
    {
        if (waveText != null)
            waveText.text = $"Wave: {currentWave}";
    }

    public void UpdateEnemiesRemaining(int count)
    {
        if (enemiesRemainingText != null)
            enemiesRemainingText.text = $"Enemies Left: {count}";
    }

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
        if (nextWaveCountdownText != null)
            nextWaveCountdownText.gameObject.SetActive(visible);

        if (nextWavePanel != null && !nextWavePanel.activeSelf)
            nextWavePanel.SetActive(true);
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

    private void HandleWaveStarted(int waveNumber, int totalEnemies)
    {
        UpdateWave(waveNumber, WaveManager.Instance != null ? WaveManager.Instance.MaxWaves : 0);
        SetNextWaveCountdownVisible(false);
        SetNextWaveButtonInteractable(false);
        SetNextWaveButtonVisible(false);
        UpdateEnemiesRemaining(totalEnemies);
    }

    private void HandleWaveEnded()
    {
        SetNextWaveCountdownVisible(true);
        SetNextWaveButtonVisible(true);
        SetNextWaveButtonInteractable(true);
    }

    private void HandleEnemiesRemainingChanged(int enemiesLeft)
    {
        UpdateEnemiesRemaining(enemiesLeft);
    }

    private void HandleCountdownTick(float secondsLeft)
    {
        UpdateNextWaveCountdown(secondsLeft);
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
