using UnityEngine;

public sealed class PrefabSelector : IPrefabSelector
{
    public GameObject Grass { get; }
    public GameObject PathBasic { get; }
    public GameObject PathDamage { get; }
    public GameObject PathSlow { get; }
    public GameObject PathStun { get; }

    public PrefabSelector(GameObject grass, GameObject basic, GameObject dmg, GameObject slow, GameObject stun)
    {
        Grass = grass; PathBasic = basic; PathDamage = dmg; PathSlow = slow; PathStun = stun;
    }

    public GameObject ChooseForMods(float damage, float slow, float stun)
    {
        if (stun > 0f) return PathStun;
        if (slow > 0f) return PathSlow;
        if (damage > 0f) return PathDamage;
        return PathBasic;
    }
}
