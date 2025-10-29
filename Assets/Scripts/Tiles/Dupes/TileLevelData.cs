using UnityEngine;

[System.Serializable]
public class TileLevelData
{
    public int currentLevel = 1;
    public int currentDupes = 0;
    public int dupesRequiredForNextLevel = 1;

    public float valueMultiplier = 1f;
    public float spawnChanceMultiplier = 1f;
    public float sizeMultiplier = 1f;

    // —— NUEVO: provider para saber si el leveleo está habilitado por upgrade
    private static ITileLevelingPermissionProvider _levelingProvider;

    /// <summary>
    /// Registrar el provider global que decide si se puede subir de nivel.
    /// Llamar una vez al iniciar escena (p.ej. desde TileDupeSystem.Awake).
    /// </summary>
    public static void SetLevelingProvider(ITileLevelingPermissionProvider provider)
    {
        _levelingProvider = provider;
    }

    public void AddDupe()
    {
        currentDupes++;

        // Si aún no se permite levelear, NO subir de nivel.
        if (_levelingProvider != null && !_levelingProvider.IsLevelingEnabled())
        {
            // Permitimos que se acumulen dupes, pero sin trigggear LevelUp.
            // Opcional: clamp para que la UI muestre "lleno" pero sin subir:
            currentDupes = Mathf.Min(currentDupes, dupesRequiredForNextLevel);
            return;
        }

        if (currentDupes >= dupesRequiredForNextLevel)
            LevelUp();
    }

    private void LevelUp()
    {
        currentLevel++;
        currentDupes = 0;

        // Escalado como ya usabas
        dupesRequiredForNextLevel = Mathf.CeilToInt(dupesRequiredForNextLevel * 1.5f);
        valueMultiplier += 0.2f;
        spawnChanceMultiplier += 0.15f;
        sizeMultiplier += 0.1f;
    }

    public string GetStatusText()
    {
        return $"Nvl {currentLevel} ({currentDupes}/{dupesRequiredForNextLevel})";
    }
}
