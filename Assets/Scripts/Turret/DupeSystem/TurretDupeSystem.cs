using System.Collections.Generic;
using UnityEngine;

public class TurretDupeSystem : MonoBehaviour, ITurretDupeSystem
{
    private Dictionary<string, TurretLevelData> turretLevels = new Dictionary<string, TurretLevelData>();

    public event System.Action<TurretDataSO, TurretLevelData> OnTurretLevelUp;
    public event System.Action<TurretDataSO> OnDupeChanged;

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
        AddDupeInternal(turret);

        if (GameModifiersManager.Stat(ModifierStat.Arsenal) <= 0f || turret == null) return;
        string sourceId = GetTurretId(turret);
        var done = new HashSet<string> { sourceId };
        foreach (var stats in FindObjectsByType<TurretStats>(FindObjectsSortMode.None))
        {
            if (stats == null || stats.Source == null) continue;
            string id = GetTurretId(stats.Source);
            if (!done.Add(id)) continue;
            Log.Info(nameof(TurretDupeSystem), $"Arsenal: +1 dupe for {id}");
            AddDupeInternal(stats.Source);
        }
    }

    private void AddDupeInternal(TurretDataSO turret)
    {
        string turretId = GetTurretId(turret);

        if (!turretLevels.ContainsKey(turretId))
        {
            turretLevels[turretId] = new TurretLevelData();
        }

        TurretLevelData levelData = turretLevels[turretId];
        int previousLevel = levelData.currentLevel;

        levelData.AddDupe();

        float extraChance = GameModifiersManager.Stat(ModifierStat.TurretExtraDupeChance);
        if (extraChance > 0f && Random.value < extraChance)
        {
            Log.Info("TurretDupeSystem", $"Extra dupe granted for {turretId}");
            levelData.AddDupe();
        }

        OnDupeChanged?.Invoke(turret);

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
