// TurretDupeSystem.cs
using System.Collections.Generic;
using UnityEngine;

public class TurretDupeSystem : MonoBehaviour, ITurretDupeSystem
{
    private Dictionary<string, TurretLevelData> turretLevels = new Dictionary<string, TurretLevelData>();

    public event System.Action<TurretDataSO, TurretLevelData> OnTurretLevelUp;

    public TurretLevelData GetTurretLevelData(TurretDataSO turret)
    {
        string turretId = GetTurretId(turret);

        if (turretLevels.ContainsKey(turretId))
        {
            return turretLevels[turretId];
        }

        return new TurretLevelData();
    }

    public void AddDupe(TurretDataSO turret)
    {
        string turretId = GetTurretId(turret);

        if (!turretLevels.ContainsKey(turretId))
        {
            turretLevels[turretId] = new TurretLevelData();
        }

        TurretLevelData levelData = turretLevels[turretId];
        int previousLevel = levelData.currentLevel;

        levelData.AddDupe();

        if (levelData.currentLevel > previousLevel)
        {
            OnTurretLevelUp?.Invoke(turret, levelData);
        }
    }

    private string GetTurretId(TurretDataSO turret)
    {
        return !string.IsNullOrEmpty(turret.id) ? turret.id : turret.name;
    }

    public void ResetAllLevels()
    {
        turretLevels.Clear();
    }
}