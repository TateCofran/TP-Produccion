using System.Collections.Generic;
using UnityEngine;


public static class WorldEnergyRegistry
{
    private static readonly HashSet<IWorldEnergySource> sources = new HashSet<IWorldEnergySource>();

    public static void Register(IWorldEnergySource source)
    {
        if (source == null) return;
        sources.Add(source);
    }

    public static void Unregister(IWorldEnergySource source)
    {
        if (source == null) return;
        sources.Remove(source);
    }

    public static float GetTotalChargePerSecond()
    {
        float total = 0f;

        foreach (var source in sources)
        {
            if (source is Object unityObj && unityObj == null)
                continue;

            total += source.GetChargePerSecond();
        }

        return total;
    }
}
