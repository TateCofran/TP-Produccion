using UnityEngine;

[DefaultExecutionOrder(-100)]
public class TileLevelCapProvider : MonoBehaviour
{
    [Header("Upgrade Integration")]
    [SerializeField] private UpgradeSO tileCapUpgradeSO;
    [SerializeField] private int[] capsByUpgradeLevel = { 5, 10, 15 };

    public int GetCurrentCap()
    {
        if (tileCapUpgradeSO == null)
            return capsByUpgradeLevel[0];

        int level = UpgradeSystemBootstrap.Service.GetCurrentLevel(tileCapUpgradeSO);
        level = Mathf.Clamp(level, 0, capsByUpgradeLevel.Length - 1);
        return capsByUpgradeLevel[level];
    }
}
