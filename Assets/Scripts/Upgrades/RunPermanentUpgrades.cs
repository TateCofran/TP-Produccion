public static class RunPermanentUpgrades
{
    public const string TileBarSpeedId = "upgrade_tileBarSpeed";
    public const string RewardZoneBonusId = "upgrade_rewardZoneBonus";
    public const string CoreHealthId = "upgrade_coreHealth";

    public const float TileBarSpeedPerLevel = 0.10f;
    public const float RewardZoneBonusPerLevel = 0.20f;
    public const int CoreHealthPerLevel = 2;

    public static float TileBarSpeedBonus => UpgradeLevels.Get(TileBarSpeedId) * TileBarSpeedPerLevel;
    public static float RewardZoneBonus => UpgradeLevels.Get(RewardZoneBonusId) * RewardZoneBonusPerLevel;
    public static int CoreHealthBonus => UpgradeLevels.Get(CoreHealthId) * CoreHealthPerLevel;
}
