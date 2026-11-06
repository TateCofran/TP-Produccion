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
    [SerializeField] private DupeProgression progression = new DupeProgression();

    public float damageMultiplier = 1f;
    public float rangeMultiplier = 1f;
    public float fireRateMultiplier = 1f;

    public int currentLevel => progression.currentLevel;
    public int currentDupes => progression.currentDupes;
    public int dupesRequiredForNextLevel => progression.dupesRequiredForNextLevel;

    public void AddDupe()
    {
        if (progression.AddDupe())
            RecalculateMultipliers();
    }

    private void RecalculateMultipliers()
    {
        damageMultiplier = 1f;
        rangeMultiplier = 1f;
        fireRateMultiplier = 1f;

        for (int level = 2; level <= currentLevel; level++)
        {
            var bonus = GetLevelBonus(level);
            damageMultiplier += bonus.damage;
            rangeMultiplier += bonus.range;
            fireRateMultiplier += bonus.fireRate;
        }
    }

    public static (float damage, float range, float fireRate) GetLevelBonus(int level)
    {
        return (0.2f, 0.1f, 0.15f);
    }

    public string GetStatusText() => progression.GetStatusText();
}
