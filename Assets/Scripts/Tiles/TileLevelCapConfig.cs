using UnityEngine;

[CreateAssetMenu(fileName = "TileLevelCapConfig", menuName = "Tiles/LevelCapConfig")]
public class TileLevelCapConfig : ScriptableObject
{
    [SerializeField] private string tileCapUpgradeId = "tile_level_cap";
    [Tooltip("Cap por tier (index = nivel del upgrade). Index 0 se usa si no compraste el upgrade.")]
    [SerializeField] private int[] capByTier = new[] { 5, 10, 15 };

    public string UpgradeId => tileCapUpgradeId;

    public int GetCapForTier(int tier)
    {
        if (capByTier == null || capByTier.Length == 0) return 5;
        tier = Mathf.Clamp(tier, 0, capByTier.Length - 1);
        return capByTier[tier];
    }
}
