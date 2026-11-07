using UnityEngine;

public sealed class PrefabSelector : IPrefabSelector
{
    public GameObject Grass { get; }
    public GameObject PathBasic { get; }
    public GameObject PathDamage { get; }
    public GameObject PathSlow { get; }
    public GameObject PathStun { get; }
    public GameObject PathExposure { get; }
    public GameObject PathAntiHeal { get; }

    public PrefabSelector(GameObject grass, GameObject basic, GameObject dmg, GameObject slow, GameObject stun)
        : this(grass, basic, dmg, slow, stun, null, null)
    {
    }

    public PrefabSelector(GameObject grass, GameObject basic, GameObject dmg, GameObject slow, GameObject stun, GameObject exposure, GameObject antiHeal)
    {
        Grass = grass; PathBasic = basic; PathDamage = dmg; PathSlow = slow; PathStun = stun;
        PathExposure = exposure != null ? exposure : slow;
        PathAntiHeal = antiHeal != null ? antiHeal : dmg;
    }

    public GameObject ChooseForMods(TileLayout.PathModifiers mods)
    {
        if (mods.exposure) return PathExposure;
        if (mods.antiHeal) return PathAntiHeal;
        return ChooseForMods(mods.dps, mods.slow, mods.stun);
    }

    public GameObject ChooseForMods(float damage, float slow, float stun)
    {
        if (stun > 0f) return PathStun;
        if (slow > 0f) return PathSlow;
        if (damage > 0f) return PathDamage;
        return PathBasic;
    }
}
