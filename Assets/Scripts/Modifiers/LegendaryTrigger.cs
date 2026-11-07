using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class LegendaryTrigger : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private ModifierPanelSelection panel;
    [SerializeField] private GridGenerator grid;
    [SerializeField] private FogOfWarSystem fog;

    [Header("Settings")]
    [Tooltip("Waves after which a legendary choice is offered (boss waves of zones 1 and 2).")]
    [SerializeField] private List<int> legendaryWaves = new List<int> { 15, 30 };
    [SerializeField, Min(1)] private int optionsCount = 2;

    [Header("Cartographer")]
    [Tooltip("Fog radius revealed around every path cell, in fog tiles.")]
    [SerializeField, Min(0f)] private float cartographerRadiusTiles = 2f;

    private WaveManager subscribedWaveManager;
    private GameModifiersManager subscribedModifiers;
    private int lastOfferedWave = -1;
    private bool pausedByTrigger;

    public static LegendaryTrigger Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (panel == null) panel = FindFirstObjectByType<ModifierPanelSelection>(FindObjectsInactive.Include);
        if (grid == null) grid = FindFirstObjectByType<GridGenerator>();
        if (fog == null) fog = FindFirstObjectByType<FogOfWarSystem>();
        if (grid != null) grid.OnTilePlaced += HandleTilePlaced;
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
        if (subscribedModifiers != null)
        {
            subscribedModifiers.OnModifierApplied -= HandleModifierApplied;
            subscribedModifiers = null;
        }
        ReleaseIfPaused();
    }

    private void OnDestroy()
    {
        if (grid != null) grid.OnTilePlaced -= HandleTilePlaced;
        if (Instance == this) Instance = null;
    }

    private void TrySubscribe()
    {
        if (subscribedWaveManager == null && WaveManager.Instance != null)
        {
            subscribedWaveManager = WaveManager.Instance;
            subscribedWaveManager.OnWaveEnded += HandleWaveEnded;
        }
        if (subscribedModifiers == null && GameModifiersManager.Instance != null)
        {
            subscribedModifiers = GameModifiersManager.Instance;
            subscribedModifiers.OnModifierApplied += HandleModifierApplied;
        }
    }

    private void HandleWaveEnded()
    {
        if (subscribedWaveManager == null) return;
        OfferIfDue(subscribedWaveManager.GetCurrentWave());
    }

    public bool IsLegendaryWave(int wave) => legendaryWaves.Contains(wave);

    public void OfferIfDue(int wave)
    {
        if (wave <= 0 || wave == lastOfferedWave || !IsLegendaryWave(wave)) return;
        if (Offer(wave)) lastOfferedWave = wave;
    }

    public bool Offer(int wave)
    {
        if (panel == null || GameModifiersManager.Instance == null) return false;

        var category = GameModifiersManager.CategoryForCurrentWorld(true);
        var options = GameModifiersManager.Instance.GetRandomAvailable(category, optionsCount);
        if (options.Count == 0)
        {
            Log.Info(nameof(LegendaryTrigger), $"No legendaries left for {category} after wave {wave}");
            return false;
        }

        var targetPanel = panel;
        ModalPanelQueue.Instance.Enqueue(() =>
        {
            GameTime.RequestPause();
            pausedByTrigger = true;
            Log.Info(nameof(LegendaryTrigger), $"Offering {options.Count} {category} legendaries after wave {wave}");
            targetPanel.OpenPanel(options, ReleaseIfPaused);
        }, () => targetPanel != null && targetPanel.IsOpen, ModalPriority.Legendary, "Legendary panel");
        return true;
    }

    private void ReleaseIfPaused()
    {
        if (!pausedByTrigger) return;
        pausedByTrigger = false;
        GameTime.ReleasePause();
    }

    private void HandleModifierApplied(ModifierSO mod)
    {
        if (mod != null && mod.Affects(ModifierStat.Cartographer)) RevealAroundPath();
    }

    private void HandleTilePlaced(PlacedTile tile)
    {
        if (GameModifiersManager.Stat(ModifierStat.Cartographer) > 0f) RevealAroundPath();
    }

    public void RevealAroundPath()
    {
        if (fog == null || !fog.IsInitialized) return;
        float radius = cartographerRadiusTiles * fog.Step;
        foreach (var cell in FindObjectsByType<PathCellEffect>(FindObjectsSortMode.None))
            if (cell != null) fog.RevealRadius(cell.transform.position, radius);
    }
}
