using UnityEngine;

public static class UpgradeLevels
{
    private const string KeyPrefix = "UpgradeLevel_";

    /// <summary>Devuelve el nivel persistido de la mejora con ese id.</summary>
    public static int Get(string upgradeId)
    {
        if (string.IsNullOrEmpty(upgradeId)) return 0;
        return PlayerPrefs.GetInt(KeyPrefix + upgradeId, 0);
    }
}
