// TileDupeUI.cs
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
        DontDestroyOnLoad(gameObject);
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

        // Evitar dobles suscripciones
        if (dupeSystem != null)
        {
            dupeSystem.OnDupeChanged -= HandleDupeChanged;
            dupeSystem.OnTileLevelUp -= HandleTileLevelUp;
        }

        dupeSystem = system;

        dupeSystem.OnDupeChanged += HandleDupeChanged;
        dupeSystem.OnTileLevelUp += HandleTileLevelUp;

        Debug.Log("[TileDupeUI] Connected to TileDupeSystem.");
    }

    private void HandleDupeChanged(TileDataSO tile)
    {
        if (tile == null || dupeSystem == null) return;

        string typeName = GetTileTypeName(tile);
        var data = dupeSystem.GetTileLevelData(tile);

        tileData[typeName] = (
            data.currentLevel,
            data.currentDupes,
            data.dupesRequiredForNextLevel
        );

        SyncBars(typeName);
    }

    private void HandleTileLevelUp(TileDataSO tile, TileLevelData levelData)
    {
        if (tile == null) return;

        string typeName = GetTileTypeName(tile);

        tileData[typeName] = (
            levelData.currentLevel,
            levelData.currentDupes,
            levelData.dupesRequiredForNextLevel
        );

        Debug.Log($"[TileDupeUI] {typeName} reached level {levelData.currentLevel}.");

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

    private string GetTileTypeName(TileDataSO tile)
    {
        // Si querés otro display (por ejemplo un campo "displayName"),
        // solo cambiás esto.
        return !string.IsNullOrEmpty(tile.id) ? tile.id : tile.name;
    }

    public (int level, int currentDupes, int maxDupes) GetTileData(string tileType)
    {
        if (tileData.TryGetValue(tileType, out var data))
            return data;

        // Default visual
        return (1, 0, 1);
    }
}
