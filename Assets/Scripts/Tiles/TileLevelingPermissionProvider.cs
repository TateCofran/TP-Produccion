using UnityEngine;

[DefaultExecutionOrder(-95)]
public sealed class TileLevelingPermissionProvider : MonoBehaviour, ITileLevelingPermissionProvider
{
    [Header("Upgrade que habilita el leveleo de tiles")]
    [SerializeField] private UpgradeSO tileLevelingUnlock; // arrastrá aquí tu UpgradeSO "tile_leveling_unlock"

    public bool IsLevelingEnabled()
    {
        if (tileLevelingUnlock == null) return false;
        // Si el nivel actual >= 1, habilita el leveleo
        int lvl = UpgradeSystemBootstrap.Service.GetCurrentLevel(tileLevelingUnlock);
        return lvl >= 1;
    }
}
