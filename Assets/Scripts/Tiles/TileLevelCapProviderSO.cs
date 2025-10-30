using UnityEngine;

public sealed class TileLevelCapProviderSO : MonoBehaviour, ITileLevelCapProvider
{
    [Header("Config")]
    [SerializeField] private TileLevelCapConfig config;

    [Header("Refs")]
    [SerializeField] private UpgradeSO tileLevelCapUpgradeSO; // arrastrá tu "Tile Level Cap" aquí

    public int GetMaxTileLevel()
    {
        if (config == null || tileLevelCapUpgradeSO == null) return 5;

        // Nivel del upgrade (0..3)
        int level = UpgradeSystemBootstrap.Service.GetCurrentLevel(tileLevelCapUpgradeSO);
        // Mapear nivel → cap
        return config.GetCapForTier(Mathf.Clamp(level, 0, 3));
    }
}
