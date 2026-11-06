using UnityEngine;

public interface ITurretDupeSystem
{
    TurretLevelData GetTurretLevelData(TurretDataSO turret);
    void AddDupe(TurretDataSO turret);
    event System.Action<TurretDataSO, TurretLevelData> OnTurretLevelUp;
    public event System.Action<TurretDataSO> OnDupeChanged;
}

[System.Serializable]
public class TurretLevelData
{
    public int currentLevel = 1;
    public int currentDupes = 0;
    public int dupesRequiredForNextLevel = 2;

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

        var bonus = GetLevelBonus(currentLevel);
        damageMultiplier += bonus.damage;
        rangeMultiplier += bonus.range;
        fireRateMultiplier += bonus.fireRate;
    }

    public static (float damage, float range, float fireRate) GetLevelBonus(int level)
    {
        return (0.2f, 0.1f, 0.15f);
    }

    public string GetStatusText()
    {
        return $"Lv {currentLevel} ({currentDupes}/{dupesRequiredForNextLevel})";
    }
}
