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

    [Header("Modifier Countdown")]
    [Tooltip("Text that shows how many waves are left until the next modifier choice.")]
    [SerializeField] private TextMeshProUGUI modifierCountdownText;
    [SerializeField, Min(1f)] private float modifierPulseScale = 1.15f;
    [SerializeField, Min(0.1f)] private float modifierPulseSpeed = 6f;
    [SerializeField] private Color modifierPulseColor = new Color(1f, 0.85f, 0.2f, 1f);

    [Header("Core Health Pulse")]
    [SerializeField, Min(1f)] private float coreHealthPulseScale = 1.25f;
    [SerializeField, Min(0.01f)] private float coreHealthPulseDuration = 0.25f;
    [SerializeField] private Color coreHealthPulseColor = new Color(1f, 0.25f, 0.2f, 1f);

    private float coreHealthPulseStart = -999f;
    private Color coreHealthBaseColor = Color.white;
    private Vector3 coreHealthBaseScale = Vector3.one;
    private bool coreHealthPulsing;

    private bool modifierPulsing;
    private Color modifierBaseColor = Color.white;
    private Vector3 modifierBaseScale = Vector3.one;

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

        if (coreHealthText != null)
        {
            coreHealthBaseColor = coreHealthText.color;
            coreHealthBaseScale = coreHealthText.rectTransform.localScale;
        }

        if (modifierCountdownText != null)
        {
            modifierBaseColor = modifierCountdownText.color;
            modifierBaseScale = modifierCountdownText.rectTransform.localScale;
            modifierCountdownText.gameObject.SetActive(false);
        }
    }

    public void PulseCoreHealth()
    {
        if (coreHealthText == null) return;
        coreHealthPulseStart = Time.unscaledTime;
        coreHealthPulsing = true;
    }

    private void UpdateCoreHealthPulse()
    {
        if (!coreHealthPulsing || coreHealthText == null) return;
        float k = (Time.unscaledTime - coreHealthPulseStart) / coreHealthPulseDuration;
        if (k >= 1f)
        {
            coreHealthPulsing = false;
            coreHealthText.rectTransform.localScale = coreHealthBaseScale;
            coreHealthText.color = coreHealthBaseColor;
            return;
        }
        float s = k < 0.3f ? Mathf.Lerp(1f, coreHealthPulseScale, k / 0.3f) : Mathf.Lerp(coreHealthPulseScale, 1f, (k - 0.3f) / 0.7f);
        coreHealthText.rectTransform.localScale = coreHealthBaseScale * s;
        coreHealthText.color = Color.Lerp(coreHealthPulseColor, coreHealthBaseColor, k);
    }

    private void Update()
    {
        UpdateCoreHealthPulse();
        if (!modifierPulsing || modifierCountdownText == null) return;
        float k = (Mathf.Sin(Time.unscaledTime * modifierPulseSpeed) + 1f) * 0.5f;
        modifierCountdownText.rectTransform.localScale = modifierBaseScale * Mathf.Lerp(1f, modifierPulseScale, k);
        modifierCountdownText.color = Color.Lerp(modifierBaseColor, modifierPulseColor, k);
    }

    public void UpdateModifierCountdown(int wavesLeft)
    {
        if (modifierCountdownText == null) return;

        if (wavesLeft <= 0)
        {
            modifierCountdownText.gameObject.SetActive(false);
            modifierPulsing = false;
            return;
        }

        modifierCountdownText.gameObject.SetActive(true);
        modifierCountdownText.text = wavesLeft == 1 ? "Modifier in 1 wave" : $"Modifier in {wavesLeft} waves";
        modifierPulsing = wavesLeft == 1;
        if (!modifierPulsing)
        {
            modifierCountdownText.rectTransform.localScale = modifierBaseScale;
            modifierCountdownText.color = modifierBaseColor;
        }
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
