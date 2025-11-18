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
        DontDestroyOnLoad(gameObject);
        InitializeData();
    }

    public void ConnectToSystem(TurretDupeSystem system)
    {
        if (system == null) return;

        // Evita duplicar subscripciones
        if (dupeSystem != null)
        {
            dupeSystem.OnDupeChanged -= HandleDupeChanged;
            dupeSystem.OnTurretLevelUp -= HandleTurretLevelUp;
        }

        dupeSystem = system;

        dupeSystem.OnDupeChanged += HandleDupeChanged;
        dupeSystem.OnTurretLevelUp += HandleTurretLevelUp;

        Debug.Log("[TurretDupeUI] Conectado a TurretDupeSystem.");
    }

    private void InitializeData()
    {
        turretData["Turret Basic"] = (1, 0, 2);
        turretData["Energy Turret"] = (1, 0, 2);
        turretData["Fire Turret"] = (1, 0, 2);
        turretData["Turret Tesla"] = (1, 0, 2);
        turretData["Turret Sniper"] = (1, 0, 2);
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

        Debug.Log($"[UI] Dupes actualizados para {typeName}: {data.currentDupes}/{data.dupesRequiredForNextLevel}");

        // Buscar todas las barras de duplicados activas en la escena
        var bars = FindObjectsByType<DupeBarUI>(FindObjectsSortMode.None);
        Debug.Log("bars: " + bars);
        foreach (var bar in bars)
        {
            Debug.Log("bar: ----" + bar.GetTurretTypeName());
            if (bar != null && bar.GetTurretTypeName() == typeName)
            {
                Debug.Log("Sincronizando barras");
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

        Debug.Log($"[UI] {typeName} subió al nivel {levelData.currentLevel}!");


        // Buscar todas las barras de duplicados activas en la escena
        var bars = FindObjectsByType<DupeBarUI>(FindObjectsSortMode.None);
        Debug.Log("bars: " + bars);

        foreach (var bar in bars)
        {
            Debug.Log("LEVEL UP bar: ----" + bar.GetTurretTypeName() + "---- typeName: " + typeName);
            if (bar != null && bar.GetTurretTypeName() == typeName)
            {
                Debug.Log("Sincronizando barras");
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
