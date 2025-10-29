// ITileDupeSystem.cs
using UnityEngine;

public interface ITileDupeSystem
{
    TileLevelData GetTileLevelData(TileDataSO tile);
    void AddDupe(TileDataSO tile);
    event System.Action<TileDataSO, TileLevelData> OnTileLevelUp;
}

[System.Serializable]
public class TileLevelData
{
    public int currentLevel = 1;
    public int currentDupes = 0;
    public int dupesRequiredForNextLevel = 1;

    public float valueMultiplier = 1f;
    public float spawnChanceMultiplier = 1f;
    public float sizeMultiplier = 1f;

    public void AddDupe()
    {
        currentDupes++;
        if (currentDupes >= dupesRequiredForNextLevel) LevelUp();
    }

    private void LevelUp()
    {
        currentLevel++;
        currentDupes = 0;
        dupesRequiredForNextLevel = Mathf.CeilToInt(dupesRequiredForNextLevel * 1.5f);
        valueMultiplier += 0.2f;
        spawnChanceMultiplier += 0.15f;
        sizeMultiplier += 0.1f;
    }

    public string GetStatusText()
    {
        return $"Nvl {currentLevel} ({currentDupes}/{dupesRequiredForNextLevel})";
    }
}
