using UnityEngine;

public enum UpgradeCategory { Lab, Workshop }

[CreateAssetMenu(fileName = "Upgrade", menuName = "Upgrades/Upgrade")]
public class UpgradeSO : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string upgradeId;   // único, estable
    [SerializeField] private string displayName;

    [TextArea(2, 6)]
    [SerializeField] private string description;

    [Header("Grouping")]
    [SerializeField] private UpgradeCategory category = UpgradeCategory.Lab;

    [Header("Progression")]
    [Min(1)][SerializeField] private int maxLevel = 1;

    [Tooltip("Costos por nivel (índice 0 = nivel 1). Si faltan entradas, se repite la última.")]
    [SerializeField] private int[] costBluePerLevel;
    [SerializeField] private int[] costRedPerLevel;

    public string Id => upgradeId;
    public string DisplayName => displayName;
    public string Description => description;
    public UpgradeCategory Category => category;
    public int MaxLevel => maxLevel;

    public (int blue, int red) GetCostForLevel(int nextLevel)
    {
        if (nextLevel < 1) nextLevel = 1;
        int idx = Mathf.Clamp(nextLevel - 1, 0, Mathf.Max(costBluePerLevel.Length, costRedPerLevel.Length) - 1);
        int blue = costBluePerLevel != null && costBluePerLevel.Length > 0
            ? costBluePerLevel[Mathf.Clamp(idx, 0, costBluePerLevel.Length - 1)]
            : 0;
        int red = costRedPerLevel != null && costRedPerLevel.Length > 0
            ? costRedPerLevel[Mathf.Clamp(idx, 0, costRedPerLevel.Length - 1)]
            : 0;
        return (blue, red);
    }
}
