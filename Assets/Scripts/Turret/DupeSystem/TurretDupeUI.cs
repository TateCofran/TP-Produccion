using System.Collections.Generic;
using UnityEngine;

public class TurretDupeUI : MonoBehaviour
{
    public static TurretDupeUI Instance { get; private set; }

    private Dictionary<string, (int level, int currentDupes, int maxDupes)> turretData =
        new Dictionary<string, (int, int, int)>();

    private TurretDupeSystem dupeSystem;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        InitializeData();
    }

    private void OnDestroy()
    {
        if (dupeSystem != null)
        {
            dupeSystem.OnDupeChanged -= HandleDupeChanged;
            dupeSystem.OnTurretLevelUp -= HandleTurretLevelUp;
        }

        if (Instance == this) Instance = null;
    }

    public void ConnectToSystem(TurretDupeSystem system)
    {
        if (system == null) return;

        if (dupeSystem != null)
        {
            dupeSystem.OnDupeChanged -= HandleDupeChanged;
            dupeSystem.OnTurretLevelUp -= HandleTurretLevelUp;
        }

        dupeSystem = system;
        turretData.Clear();
        InitializeData();

        dupeSystem.OnDupeChanged += HandleDupeChanged;
        dupeSystem.OnTurretLevelUp += HandleTurretLevelUp;

        Log.Info("TurretDupeUI", "Connected to TurretDupeSystem");
    }

    private void InitializeData()
    {
        int initial = DupeProgression.InitialDupesRequired;
        turretData["Turret Basic"] = (1, 0, initial);
        turretData["Energy Turret"] = (1, 0, initial);
        turretData["Fire Turret"] = (1, 0, initial);
        turretData["Turret Tesla"] = (1, 0, initial);
        turretData["Turret Sniper"] = (1, 0, initial);
    }

    private void HandleDupeChanged(TurretDataSO turret)
    {
        string typeName = turret.displayName;
        var data = dupeSystem.GetTurretLevelData(turret);

        turretData[typeName] = (
            data.currentLevel,
            data.currentDupes,
            data.dupesRequiredForNextLevel
        );

        Log.Info("TurretDupeUI", $"Dupes updated for {typeName}: {data.currentDupes}/{data.dupesRequiredForNextLevel}");

        var bars = FindObjectsByType<DupeBarUI>(FindObjectsSortMode.None);
        foreach (var bar in bars)
        {
            if (bar != null && bar.GetTurretTypeName() == typeName)
            {
                Log.Info("TurretDupeUI", "Syncing bars");
                bar.UpdateDupeBar();
            }
        }
    }

    private void HandleTurretLevelUp(TurretDataSO turret, TurretLevelData levelData)
    {
        string typeName = turret.displayName;
        turretData[typeName] = (
            levelData.currentLevel,
            levelData.currentDupes,
            levelData.dupesRequiredForNextLevel
        );

        Log.Info("TurretDupeUI", $"{typeName} reached level {levelData.currentLevel}");

        var bars = FindObjectsByType<DupeBarUI>(FindObjectsSortMode.None);

        foreach (var bar in bars)
        {
            if (bar != null && bar.GetTurretTypeName() == typeName)
            {
                Log.Info("TurretDupeUI", "Syncing bars");
                bar.UpdateDupeBar();
            }
        }
    }

    public (int level, int currentDupes, int maxDupes) GetTurretData(string turretType)
    {
        if (turretData.TryGetValue(turretType, out var data))
            return data;

        return (1, 0, DupeProgression.InitialDupesRequired);
    }
}
