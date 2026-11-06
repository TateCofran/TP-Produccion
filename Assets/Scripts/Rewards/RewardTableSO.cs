using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RewardEntry
{
    public RewardType type;
    [Min(0)] public int amount;
    [Min(0f)] public float weight = 1f;
}

[CreateAssetMenu(menuName = "Rewards/Reward Table")]
public class RewardTableSO : ScriptableObject
{
    public List<RewardEntry> entries = new List<RewardEntry>
    {
        new RewardEntry { type = RewardType.BlueEssence, amount = 25, weight = 3f },
        new RewardEntry { type = RewardType.RedEssence, amount = 25, weight = 3f },
        new RewardEntry { type = RewardType.FreeTurretDupe, amount = 1, weight = 2f },
        new RewardEntry { type = RewardType.FreeTileDupe, amount = 1, weight = 2f },
        new RewardEntry { type = RewardType.ExtraModifierPick, amount = 1, weight = 1f }
    };

    public RewardEntry PickRandom()
    {
        if (entries == null || entries.Count == 0) return null;

        float total = 0f;
        foreach (var e in entries)
            if (e != null) total += Mathf.Max(0f, e.weight);

        if (total <= 0f) return entries[0];

        float roll = Random.value * total;
        foreach (var e in entries)
        {
            if (e == null) continue;
            roll -= Mathf.Max(0f, e.weight);
            if (roll <= 0f) return e;
        }
        return entries[entries.Count - 1];
    }

    public int GetAmount(RewardType type, int fallback)
    {
        if (entries == null) return fallback;
        foreach (var e in entries)
            if (e != null && e.type == type) return e.amount;
        return fallback;
    }
}
