// TileLevelApplier.cs
using UnityEngine;

[RequireComponent(typeof(TileStats))]
public class TileLevelApplier : MonoBehaviour
{
    [SerializeField] private TileDataSO tileData;
    [SerializeField] private TileLevelData levelData;

    private TileStats tileStats;
    private ITileDupeSystem dupeSystem;

    private void Awake()
    {
        tileStats = GetComponent<TileStats>();
        dupeSystem = FindFirstObjectByType<TileDupeSystem>();
    }

    public void Initialize(TileDataSO data)
    {
        tileData = data;
        if (dupeSystem != null)
        {
            levelData = dupeSystem.GetTileLevelData(data);
            ApplyLevelBonuses();
        }
    }

    private void ApplyLevelBonuses()
    {
        if (tileStats == null || levelData == null) return;

        float baseValue = tileData.value;
        float baseSpawnChance = tileData.spawnChance;
        float baseSize = tileData.size;

        float finalValue = baseValue * levelData.valueMultiplier;
        float finalSpawnChance = baseSpawnChance * levelData.spawnChanceMultiplier;
        float finalSize = baseSize * levelData.sizeMultiplier;

        tileStats.ApplyLevelModifiers(finalValue, finalSpawnChance, finalSize);
    }

    public string GetLevelStatus()
    {
        return levelData?.GetStatusText() ?? "Nvl 1";
    }
}
