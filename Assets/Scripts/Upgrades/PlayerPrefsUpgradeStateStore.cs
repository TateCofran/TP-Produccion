using UnityEngine;

public interface IUpgradeStateStore
{
    int GetLevel(string upgradeId);
    void SetLevel(string upgradeId, int level);
}

public class PlayerPrefsUpgradeStateStore : IUpgradeStateStore
{
    private const string KeyPrefix = "UpgradeLevel_";

    public int GetLevel(string upgradeId)
    {
        if (string.IsNullOrEmpty(upgradeId)) return 0;
        return PlayerPrefs.GetInt(KeyPrefix + upgradeId, 0);
    }

    public void SetLevel(string upgradeId, int level)
    {
        if (string.IsNullOrEmpty(upgradeId)) return;
        PlayerPrefs.SetInt(KeyPrefix + upgradeId, Mathf.Max(0, level));
        PlayerPrefs.Save();
    }
}
