using UnityEngine;

public static class TurretDamageCalculator
{
    public const float ExecuteHealthThreshold = 0.25f;

    public static float Compute(float baseDamage, Enemy target)
    {
        if (target == null || GameModifiersManager.Instance == null) return baseDamage;

        float damage = baseDamage;

        float executeBonus = GameModifiersManager.Stat(ModifierStat.ExecuteBonus);
        if (executeBonus != 0f && target.Health != null)
        {
            float max = target.Health.GetMaxHealth();
            if (max > 0f && target.Health.GetCurrentHealth() / max < ExecuteHealthThreshold)
                damage *= 1f + executeBonus;
        }

        float bossBonus = GameModifiersManager.Stat(ModifierStat.BossBonus);
        if (bossBonus != 0f && IsBossType(target.Type))
            damage *= 1f + bossBonus;

        return Mathf.Max(0f, damage);
    }

    public static int ComputeInt(float baseDamage, Enemy target)
    {
        return Mathf.Max(1, Mathf.RoundToInt(Compute(baseDamage, target)));
    }

    public static bool IsBossType(EnemyType type)
    {
        switch (type)
        {
            case EnemyType.MiniBoss:
            case EnemyType.MiniBoss2:
            case EnemyType.MiniBoss3:
            case EnemyType.Boss:
            case EnemyType.Boss2:
            case EnemyType.Boss3:
                return true;
            default:
                return false;
        }
    }
}
