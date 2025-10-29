// ITurretDupeSystem.cs
using UnityEngine;

public interface ITurretDupeSystem
{
    TurretLevelData GetTurretLevelData(TurretDataSO turret);
    void AddDupe(TurretDataSO turret);
    event System.Action<TurretDataSO, TurretLevelData> OnTurretLevelUp;
}

[System.Serializable]
public class TurretLevelData
{
    public int currentLevel = 1;
    public int currentDupes = 0;
    public int dupesRequiredForNextLevel = 1;

    public float damageMultiplier = 1f;
    public float rangeMultiplier = 1f;
    public float fireRateMultiplier = 1f;

    public void AddDupe()
    {
        currentDupes++;

        if (currentDupes >= dupesRequiredForNextLevel)
        {
            LevelUp();
        }
    }

    private void LevelUp()
    {
        currentLevel++;
        currentDupes = 0;
        dupesRequiredForNextLevel = Mathf.CeilToInt(dupesRequiredForNextLevel * 1.5f);

        damageMultiplier += 0.2f;
        rangeMultiplier += 0.1f;
        fireRateMultiplier += 0.15f;
    }

    public string GetStatusText()
    {
        return $"Nvl {currentLevel} ({currentDupes}/{dupesRequiredForNextLevel})";
    }
}