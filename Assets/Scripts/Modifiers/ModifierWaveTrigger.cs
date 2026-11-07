using UnityEngine;

public class ModifierWaveTrigger : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private ModifierPanelSelection panel;

    [Header("Settings")]
    [Tooltip("A modifier choice is offered after every N completed waves.")]
    [SerializeField, Min(1)] private int everyNWaves = 3;
    [SerializeField, Min(1)] private int optionsCount = 3;

    private WaveManager subscribedWaveManager;
    private bool pausedByTrigger;

    private void Start()
    {
        if (panel == null) panel = FindFirstObjectByType<ModifierPanelSelection>(FindObjectsInactive.Include);
        TrySubscribe();
    }

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void OnDisable()
    {
        if (subscribedWaveManager != null)
        {
            subscribedWaveManager.OnWaveEnded -= HandleWaveEnded;
            subscribedWaveManager.OnWaveStarted -= HandleWaveStarted;
            subscribedWaveManager = null;
        }

        ReleaseIfPaused();
    }

    private void TrySubscribe()
    {
        if (subscribedWaveManager != null || WaveManager.Instance == null) return;
        subscribedWaveManager = WaveManager.Instance;
        subscribedWaveManager.OnWaveEnded += HandleWaveEnded;
        subscribedWaveManager.OnWaveStarted += HandleWaveStarted;
        RefreshCountdown();
    }

    private void HandleWaveStarted(int wave, int enemies)
    {
        RefreshCountdown();
    }

    public int WavesUntilNextOffer()
    {
        var waves = subscribedWaveManager != null ? subscribedWaveManager : WaveManager.Instance;
        if (waves == null || panel == null || GameModifiersManager.Instance == null) return 0;
        if (GameModifiersManager.Instance.GetAvailable(GameModifiersManager.CategoryForCurrentWorld()).Count == 0) return 0;

        int current = Mathf.Max(0, waves.GetCurrentWave());
        int completed = waves.WaveInProgress ? current - 1 : current;
        int nextOffer = (completed / everyNWaves + 1) * everyNWaves;
        if (nextOffer >= waves.MaxWaves) return 0;
        return nextOffer - completed;
    }

    private void RefreshCountdown()
    {
        if (UIController.Instance != null) UIController.Instance.UpdateModifierCountdown(WavesUntilNextOffer());
    }

    private void HandleWaveEnded()
    {
        var waves = subscribedWaveManager;
        if (waves == null) return;
        RefreshCountdown();

        int wave = waves.GetCurrentWave();
        LegendaryTrigger.Instance?.OfferIfDue(wave);
        if (wave <= 0 || wave % everyNWaves != 0 || waves.IsLastWave()) return;

        if (panel == null)
        {
            Debug.LogWarning("[ModifierWaveTrigger] ModifierPanelSelection is not assigned");
            return;
        }

        if (GameModifiersManager.Instance == null)
        {
            Debug.LogWarning("[ModifierWaveTrigger] GameModifiersManager not found in the scene");
            return;
        }

        var category = GameModifiersManager.CategoryForCurrentWorld();
        var options = GameModifiersManager.Instance.GetRandomAvailable(category, optionsCount);
        if (options.Count == 0)
        {
            Debug.Log($"[ModifierWaveTrigger] No modifiers left for {category} after wave {wave}");
            return;
        }

        var targetPanel = panel;
        ModalPanelQueue.Instance.Enqueue(() =>
        {
            GameTime.RequestPause();
            pausedByTrigger = true;
            Debug.Log($"[ModifierWaveTrigger] Offering {options.Count} {category} modifiers after wave {wave}");
            targetPanel.OpenPanel(options, ReleaseIfPaused);
        }, () => targetPanel != null && targetPanel.IsOpen, ModalPriority.Modifier, "Modifier panel");
    }

    private void ReleaseIfPaused()
    {
        if (!pausedByTrigger) return;
        pausedByTrigger = false;
        GameTime.ReleasePause();
    }
}
