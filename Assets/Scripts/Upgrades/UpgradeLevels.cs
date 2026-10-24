using UnityEngine;

public static class UpgradeLevels
{
    private const string KeyPrefix = "UpgradeLevel_";

    public static int Get(string upgradeId)
    {
        if (string.IsNullOrEmpty(upgradeId)) return 0;
        return PlayerPrefs.GetInt(KeyPrefix + upgradeId, 0);
    }
}
