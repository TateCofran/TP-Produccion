using System.Collections.Generic;
using UnityEngine;

public class TileDupeUI : MonoBehaviour
{
    public static TileDupeUI Instance { get; private set; }

    private readonly Dictionary<string, (int level, int currentDupes, int maxDupes)> tileData =
        new();

    private TileDupeSystem dupeSystem;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (dupeSystem != null)
        {
            dupeSystem.OnDupeChanged -= HandleDupeChanged;
            dupeSystem.OnTileLevelUp -= HandleTileLevelUp;
        }

        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        if (dupeSystem == null)
        {
            var system = FindFirstObjectByType<TileDupeSystem>();
            if (system != null)
            {
                ConnectToSystem(system);
            }
            else
            {
                Debug.LogWarning("[TileDupeUI] No TileDupeSystem found in scene.");
            }
        }
    }

    public void ConnectToSystem(TileDupeSystem system)
    {
        if (system == null) return;

        if (dupeSystem != null)
        {
            dupeSystem.OnDupeChanged -= HandleDupeChanged;
            dupeSystem.OnTileLevelUp -= HandleTileLevelUp;
        }

        dupeSystem = system;
        tileData.Clear();

        dupeSystem.OnDupeChanged += HandleDupeChanged;
        dupeSystem.OnTileLevelUp += HandleTileLevelUp;

        Log.Info("TileDupeUI", "Connected to TileDupeSystem.");
    }

    private void HandleDupeChanged(TileLayout layout)
    {
        if (layout == null || dupeSystem == null) return;

        string typeName = GetTileTypeName(layout);
        var data = dupeSystem.GetTileLevelData(layout);

        tileData[typeName] = (
            data.currentLevel,
            data.currentDupes,
            data.dupesRequiredForNextLevel
        );

        SyncBars(typeName);
    }

    private void HandleTileLevelUp(TileLayout layout, TileLevelData levelData)
    {
        if (layout == null) return;

        string typeName = GetTileTypeName(layout);

        tileData[typeName] = (
            levelData.currentLevel,
            levelData.currentDupes,
            levelData.dupesRequiredForNextLevel
        );

        Log.Info("TileDupeUI", $"{typeName} reached level {levelData.currentLevel}.");

        SyncBars(typeName);
    }

    private void SyncBars(string typeName)
    {
        var bars = FindObjectsByType<TileDupeBarUI>(FindObjectsSortMode.None);
        foreach (var bar in bars)
        {
            if (bar != null && bar.GetTileTypeName() == typeName)
            {
                bar.UpdateDupeBar();
            }
        }
    }

    public static string GetTileTypeName(TileLayout layout)
    {
        return layout != null ? layout.name : string.Empty;
    }

    public (int level, int currentDupes, int maxDupes) GetTileData(string tileType)
    {
        if (tileData.TryGetValue(tileType, out var data))
            return data;

        return (1, 0, DupeProgression.InitialDupesRequired);
    }
}
