using UnityEngine;

public enum UpgradeCategory { Lab, Workshop }

[CreateAssetMenu(fileName = "Upgrade", menuName = "Upgrades/Upgrade")]
public class UpgradeSO : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string upgradeId;
    [SerializeField] private string displayName;

    [TextArea(2, 6)]
    [SerializeField] private string description;

    [Header("Grouping")]
    [SerializeField] private UpgradeCategory category = UpgradeCategory.Lab;

    [Header("Progression")]
    [Min(1)][SerializeField] private int maxLevel = 1;

    [Tooltip("Cost per level (index 0 = level 1). Missing entries repeat the last one.")]
    [SerializeField] private int[] costBluePerLevel;
    [SerializeField] private int[] costRedPerLevel;

    public string Id => upgradeId;
    public string DisplayName => displayName;
    public string Description => description;
    public UpgradeCategory Category => category;
    public int MaxLevel => maxLevel;

    public (int blue, int red) GetCostForLevel(int nextLevel)
    {
        int index = Mathf.Max(0, nextLevel - 1);
        return (GetCost(costBluePerLevel, index), GetCost(costRedPerLevel, index));
    }

    private static int GetCost(int[] costs, int index)
    {
        if (costs == null || costs.Length == 0) return 0;
        return costs[Mathf.Min(index, costs.Length - 1)];
    }
}
