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

        Debug.Log("[TurretDupeUI] Connected to TurretDupeSystem");
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

        Debug.Log($"[TurretDupeUI] Dupes updated for {typeName}: {data.currentDupes}/{data.dupesRequiredForNextLevel}");

        var bars = FindObjectsByType<DupeBarUI>(FindObjectsSortMode.None);
        Debug.Log("bars: " + bars);
        foreach (var bar in bars)
        {
            Debug.Log("bar: ----" + bar.GetTurretTypeName());
            if (bar != null && bar.GetTurretTypeName() == typeName)
            {
                Debug.Log("[TurretDupeUI] Syncing bars");
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

        Debug.Log($"[TurretDupeUI] {typeName} reached level {levelData.currentLevel}");

        var bars = FindObjectsByType<DupeBarUI>(FindObjectsSortMode.None);
        Debug.Log("bars: " + bars);

        foreach (var bar in bars)
        {
            Debug.Log("LEVEL UP bar: ----" + bar.GetTurretTypeName() + "[TurretDupeUI] typeName: " + typeName);
            if (bar != null && bar.GetTurretTypeName() == typeName)
            {
                Debug.Log("[TurretDupeUI] Syncing bars");
                bar.UpdateDupeBar();
            }
        }
    }

    public (int level, int currentDupes, int maxDupes) GetTurretData(string turretType)
    {
        if (turretData.TryGetValue(turretType, out var data))
            return data;

        return (0, 0, 0);
    }
}
