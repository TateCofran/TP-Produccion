using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TurretManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject turretUIPrefab;
    //public Transform turretsPanel;

    private List<Turret> activeTurrets = new List<Turret>();
    public static TurretManager Instance { get; private set; }

    // Probabilidad global de fallo (0 = nunca falla, 1 = siempre falla)
    private float globalTurretMissChance = 0f;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }
    public void RegisterTurret(Turret turret)
    {
        activeTurrets.Add(turret);
    }

    public void UnregisterTurret(Turret turret)
    {
        if (activeTurrets.Contains(turret))
        {
            activeTurrets.Remove(turret);
        }
    }
    public IReadOnlyList<Turret> GetAllTurrets()
    {
        return activeTurrets.AsReadOnly();
    }

    public void SetGlobalTurretMissChance(float value)
    {
        globalTurretMissChance = Mathf.Clamp01(value);
    }

    public float GetGlobalTurretMissChance()
    {
        return globalTurretMissChance;
    }

}