using UnityEngine;

[System.Serializable]
public class DupeProgression
{
    public const int InitialDupesRequired = 2;
    public const float DupesGrowthFactor = 1.5f;

    public int currentLevel = 1;
    public int currentDupes = 0;
    public int dupesRequiredForNextLevel = InitialDupesRequired;

    public bool AddDupe()
    {
        currentDupes++;
        if (currentDupes < dupesRequiredForNextLevel) return false;

        currentLevel++;
        currentDupes = 0;
        dupesRequiredForNextLevel = GetNextRequirement(dupesRequiredForNextLevel);
        return true;
    }

    public static int GetNextRequirement(int current)
    {
        return Mathf.CeilToInt(current * DupesGrowthFactor);
    }

    public string GetStatusText()
    {
        return $"Lv {currentLevel} ({currentDupes}/{dupesRequiredForNextLevel})";
    }
}
