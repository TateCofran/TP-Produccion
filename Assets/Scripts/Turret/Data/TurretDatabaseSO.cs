using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "TD/Turret Database (SO)", fileName = "TurretDatabaseSO")]
public class TurretDatabaseSO : ScriptableObject
{
    [Header("Todas las torretas (SO)")]
    public List<TurretDataSO> allTurrets = new();

    private Dictionary<string, TurretDataSO> _byId;
    void OnEnable()
    {
        _byId = new Dictionary<string, TurretDataSO>();
        foreach (var t in allTurrets)
        {
            if (t == null) continue;
            if (!string.IsNullOrEmpty(t.id)) _byId[t.id] = t;
        }
    }

    public TurretDataSO GetById(string id)
        => (!string.IsNullOrEmpty(id) && _byId != null && _byId.TryGetValue(id, out var so)) ? so : null;
}
