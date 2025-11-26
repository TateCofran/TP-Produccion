using UnityEngine;

public static class TurretPermanentUpgrades
{
    private const string GLOBAL_DAMAGE_ID = "turret_global_damage";
    private const string GLOBAL_RANGE_ID = "turret_global_range";
    private const string GLOBAL_FIRE_RATE_ID = "turret_global_fire_rate";

    private const float DAMAGE_PER_LEVEL = 0.10f;   // +10% por nivel
    private const float RANGE_PER_LEVEL = 0.05f;    // +5% por nivel
    private const float FIRE_RATE_PER_LEVEL = 0.08f; // +8% por nivel

    public static float GetDamageMultiplier()
    {
        int lvl = UpgradeLevels.Get(GLOBAL_DAMAGE_ID);
        return 1f + (lvl * DAMAGE_PER_LEVEL);
    }

    public static float GetRangeMultiplier()
    {
        int lvl = UpgradeLevels.Get(GLOBAL_RANGE_ID);
        return 1f + (lvl * RANGE_PER_LEVEL);
    }

    public static float GetFireRateMultiplier()
    {
        int lvl = UpgradeLevels.Get(GLOBAL_FIRE_RATE_ID);
        return 1f + (lvl * FIRE_RATE_PER_LEVEL);
    }

    public static void ApplyPermanentUpgrades(ref float damage, ref float range, ref float fireRate)
    {
        damage *= GetDamageMultiplier();
        range *= GetRangeMultiplier();
        fireRate *= GetFireRateMultiplier();
    }
}
