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

    private static ITileLevelingPermissionProvider _levelingProvider;

    public static void SetLevelingProvider(ITileLevelingPermissionProvider provider)
    {
        _levelingProvider = provider;
    }

    public void AddDupe()
    {
        currentDupes++;

        if (_levelingProvider != null && !_levelingProvider.IsLevelingEnabled())
        {
            currentDupes = Mathf.Min(currentDupes, dupesRequiredForNextLevel);
            return;
        }

        if (currentDupes >= dupesRequiredForNextLevel)
            LevelUp();
    }

    private void LevelUp()
    {
        currentLevel++;
        currentDupes = 0;

        dupesRequiredForNextLevel = Mathf.CeilToInt(dupesRequiredForNextLevel * 1.5f);
    }

    public string GetStatusText()
    {
        return $"Nvl {currentLevel} ({currentDupes}/{dupesRequiredForNextLevel})";
    }
}
