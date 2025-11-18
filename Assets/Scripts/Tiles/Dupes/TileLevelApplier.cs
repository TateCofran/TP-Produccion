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
        }

        ApplyBaseStats();
    }

    private void ApplyBaseStats()
    {
        if (tileStats == null || tileData == null) return;

        float baseValue = tileData.value;
        float baseSpawnChance = tileData.spawnChance;
        float baseSize = tileData.size;

        tileStats.ApplyLevelModifiers(baseValue, baseSpawnChance, baseSize);
    }

    public string GetLevelStatus()
    {

        return levelData?.GetStatusText() ?? "Nvl 1";
    }
}
