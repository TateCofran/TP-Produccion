using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Enemies/Elite Spawn Rules", fileName = "EliteSpawnRules")]
public class EliteSpawnRules : ScriptableObject
{
    [System.Serializable]
    public class AffixWeight
    {
        public EliteAffix affix;
        [Min(0f)] public float weight = 1f;
    }

    [Tooltip("Elite chance per zone. Index 0 = zone 1.")]
    public List<float> chancePerZone = new List<float> { 0f, 0.06f, 0.12f };
    public List<AffixWeight> affixWeights = new List<AffixWeight>
    {
        new AffixWeight { affix = EliteAffix.Hasted, weight = 1f },
        new AffixWeight { affix = EliteAffix.Armored, weight = 1f },
        new AffixWeight { affix = EliteAffix.Regenerating, weight = 1f },
        new AffixWeight { affix = EliteAffix.Shielded, weight = 1f },
        new AffixWeight { affix = EliteAffix.Splitter, weight = 1f }
    };
    [Tooltip("Enemy types that are never elite.")]
    public List<EnemyType> excludedTypes = new List<EnemyType>
    {
        EnemyType.MiniBoss, EnemyType.MiniBoss2, EnemyType.MiniBoss3,
        EnemyType.Boss, EnemyType.Boss2, EnemyType.Boss3
    };
    [Min(1)] public int wavesPerZone = 15;

    public int ZoneForWave(int wave) => Mathf.Max(1, (Mathf.Max(1, wave) - 1) / wavesPerZone + 1);

    public float ChanceForWave(int wave)
    {
        int zone = ZoneForWave(wave);
        if (chancePerZone == null || chancePerZone.Count == 0) return 0f;
        return chancePerZone[Mathf.Clamp(zone - 1, 0, chancePerZone.Count - 1)];
    }

    public bool CanBeElite(EnemyType type) => excludedTypes == null || !excludedTypes.Contains(type);

    public bool TryRoll(int wave, EnemyType type, out EliteAffix affix)
    {
        affix = EliteAffix.Hasted;
        if (!CanBeElite(type)) return false;
        if (Random.value >= ChanceForWave(wave)) return false;
        affix = PickAffix();
        return true;
    }

    public EliteAffix PickAffix()
    {
        float total = 0f;
        foreach (var w in affixWeights) total += Mathf.Max(0f, w.weight);
        if (total <= 0f) return EliteAffix.Hasted;
        float r = Random.value * total;
        foreach (var w in affixWeights)
        {
            r -= Mathf.Max(0f, w.weight);
            if (r <= 0f) return w.affix;
        }
        return affixWeights[affixWeights.Count - 1].affix;
    }
}
