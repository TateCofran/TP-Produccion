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
            subscribedWaveManager = null;
        }

        ReleaseIfPaused();
    }

    private void TrySubscribe()
    {
        if (subscribedWaveManager != null || WaveManager.Instance == null) return;
        subscribedWaveManager = WaveManager.Instance;
        subscribedWaveManager.OnWaveEnded += HandleWaveEnded;
    }

    private void HandleWaveEnded()
    {
        var waves = subscribedWaveManager;
        if (waves == null) return;

        int wave = waves.GetCurrentWave();
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

        GameTime.RequestPause();
        pausedByTrigger = true;
        Debug.Log($"[ModifierWaveTrigger] Offering {options.Count} {category} modifiers after wave {wave}");

        panel.OpenPanel(options, ReleaseIfPaused);
    }

    private void ReleaseIfPaused()
    {
        if (!pausedByTrigger) return;
        pausedByTrigger = false;
        GameTime.ReleasePause();
    }
}
