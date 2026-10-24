using UnityEngine;

[DefaultExecutionOrder(-95)]
public sealed class TileLevelingPermissionProvider : MonoBehaviour, ITileLevelingPermissionProvider
{
    [Header("Upgrade that unlocks tile leveling")]
    [SerializeField] private UpgradeSO tileLevelingUnlock;

    public bool IsLevelingEnabled()
    {
        if (tileLevelingUnlock == null) return false;

        int lvl = UpgradeSystemBootstrap.Service.GetCurrentLevel(tileLevelingUnlock);
        return lvl >= 1;
    }
}
