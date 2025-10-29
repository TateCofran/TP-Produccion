using UnityEngine;

[System.Serializable]
public class TileLevelData
{
    public int currentLevel = 1;
    public int currentDupes = 0;
    public int dupesRequiredForNextLevel = 1;

    public float valueMultiplier = 1f;
    public float spawnChanceMultiplier = 1f;
    public float sizeMultiplier = 1f;

    // Nuevo: referencia global al proveedor de cap
    private static TileLevelCapProvider capProvider;

    public static void SetCapProvider(TileLevelCapProvider provider)
    {
        capProvider = provider;
    }

    public void AddDupe()
    {
        currentDupes++;
        if (currentDupes >= dupesRequiredForNextLevel)
            TryLevelUp();
    }

    private void TryLevelUp()
    {
        int maxCap = capProvider != null ? capProvider.GetCurrentCap() : 5;
        if (currentLevel >= maxCap)
        {
            // Alcanzó el cap, no sube más
            currentDupes = dupesRequiredForNextLevel;
            return;
        }

        LevelUp();
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
