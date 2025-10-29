// TurretLevelApplier.cs
using UnityEngine;

[RequireComponent(typeof(TurretStats))]
public class TurretLevelApplier : MonoBehaviour
{
    [SerializeField] private TurretDataSO turretData;
    [SerializeField] private TurretLevelData levelData;

    private TurretStats turretStats;
    private ITurretDupeSystem dupeSystem;

    private void Awake()
    {
        turretStats = GetComponent<TurretStats>();
        dupeSystem = FindFirstObjectByType<TurretDupeSystem>();
    }

    public void Initialize(TurretDataSO data)
    {
        turretData = data;

        if (dupeSystem != null)
        {
            levelData = dupeSystem.GetTurretLevelData(data);
            ApplyLevelBonuses();
        }
    }

    private void ApplyLevelBonuses()
    {
        if (turretStats == null || levelData == null) return;

        // Guardar los valores base del SO
        float baseDamage = turretData.damage;
        float baseRange = turretData.range;
        float baseFireRate = turretData.fireRate;

        // Aplicar multiplicadores de nivel
        float finalDamage = baseDamage * levelData.damageMultiplier;
        float finalRange = baseRange * levelData.rangeMultiplier;
        float finalFireRate = baseFireRate * levelData.fireRateMultiplier;

        // Aplicar al TurretStats
        turretStats.ApplyLevelModifiers(finalDamage, finalRange, finalFireRate);

        Debug.Log($"Torreta nivel {levelData.currentLevel} aplicada: " +
                 $"Daño: {finalDamage}, Rango: {finalRange}, Velocidad: {finalFireRate}");
    }

    public string GetLevelStatus()
    {
        return levelData?.GetStatusText() ?? "Nvl 1";
    }
}