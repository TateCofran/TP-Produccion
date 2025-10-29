using UnityEngine;

[RequireComponent(typeof(TurretStats))]
public class TurretDataHolder : MonoBehaviour
{
    [Header("SO con los datos de la torreta")]
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
            Debug.LogWarning($"[TurretDataHolder] No se asignó un TurretDataSO en {gameObject.name}");
    }

    public void ApplyDataSO(TurretDataSO data)
    {
        if (data == null) return;

        turretDataSO = data;
        stats.InitializeFromSO(data);
        levelApplier.Initialize(data);
    }
}
