using UnityEngine;

[CreateAssetMenu(menuName = "Progression/XP Curve")]
public class XpCurveSO : ScriptableObject
{
    [Tooltip("XP needed to go from level 1 to level 2.")]
    [Min(1f)] public float baseXp = 100f;
    [Tooltip("Growth of the XP needed per level: baseXp * level ^ exponent.")]
    [Min(0.1f)] public float exponent = 1.4f;

    public int XpToNext(int level)
    {
        return Mathf.Max(1, Mathf.RoundToInt(baseXp * Mathf.Pow(Mathf.Max(1, level), exponent)));
    }

    public int TotalXpForLevel(int level)
    {
        int total = 0;
        for (int l = 1; l < level; l++) total += XpToNext(l);
        return total;
    }

    public int LevelForXp(int xp, out int xpIntoLevel, out int xpNeeded)
    {
        int level = 1;
        int remaining = Mathf.Max(0, xp);
        while (level < 999)
        {
            int need = XpToNext(level);
            if (remaining < need)
            {
                xpIntoLevel = remaining;
                xpNeeded = need;
                return level;
            }
            remaining -= need;
            level++;
        }
        xpIntoLevel = remaining;
        xpNeeded = XpToNext(level);
        return level;
    }
}
