using UnityEngine;

public sealed class TileLevelCapProviderSO : MonoBehaviour, ITileLevelCapProvider
{
    [Header("Config")]
    [SerializeField] private TileLevelCapConfig config;

    [Header("Refs")]
    [SerializeField] private UpgradeSO tileLevelCapUpgradeSO;

    public int GetMaxTileLevel()
    {
        if (config == null || tileLevelCapUpgradeSO == null) return 5;

        int level = UpgradeSystemBootstrap.Service.GetCurrentLevel(tileLevelCapUpgradeSO);

        return config.GetCapForTier(Mathf.Clamp(level, 0, 3));
    }
}
