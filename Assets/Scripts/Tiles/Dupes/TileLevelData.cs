using UnityEngine;

[System.Serializable]
public class TileLevelData
{
    [SerializeField] private DupeProgression progression = new DupeProgression();

    public float damageMultiplier = 1f;
    public float slowMultiplier = 1f;
    public float stunMultiplier = 1f;

    public int currentLevel => progression.currentLevel;
    public int currentDupes => progression.currentDupes;
    public int dupesRequiredForNextLevel => progression.dupesRequiredForNextLevel;

    public bool AddDupe()
    {
        bool leveledUp = progression.AddDupe();
        if (leveledUp) RecalculateMultipliers();
        return leveledUp;
    }

    private void RecalculateMultipliers()
    {
        int steps = Mathf.Max(0, currentLevel - 1);
        damageMultiplier = 1f + 0.2f * steps;
        slowMultiplier = 1f + 0.1f * steps;
        stunMultiplier = 1f + 0.1f * steps;
    }

    public string GetStatusText() => progression.GetStatusText();
}
