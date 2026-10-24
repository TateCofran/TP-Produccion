using UnityEngine;

[RequireComponent(typeof(TurretStats))]
public class TurretDataHolder : MonoBehaviour
{
    [Header("Turret Data SO")]
    public TurretDataSO turretDataSO;

    private TurretStats stats;
    private TurretLevelApplier levelApplier;

    void Awake()
    {
        stats = GetComponent<TurretStats>();
        levelApplier = GetComponent<TurretLevelApplier>();

        if (turretDataSO != null)
            ApplyDataSO(turretDataSO);
        else
            Debug.LogWarning($"[TurretDataHolder] No TurretDataSO assigned on {gameObject.name}");
    }

    public void ApplyDataSO(TurretDataSO data)
    {
        if (data == null) return;

        turretDataSO = data;
        if (stats == null) stats = GetComponent<TurretStats>();
        if (levelApplier == null) levelApplier = GetComponent<TurretLevelApplier>();

        stats.InitializeFromSO(data);
        levelApplier?.Initialize(data);
    }
}
