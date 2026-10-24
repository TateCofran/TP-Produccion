using System;
using UnityEngine;

public enum WorldAffinity { Neutral, Normal, Other }

public static class WorldState
{
    public static ShiftingWorldMechanic.World Current { get; private set; } = ShiftingWorldMechanic.World.Normal;
    public static float TimeInPreviousWorld { get; private set; }
    public static float TimeInCurrentWorld => Time.time - _enteredAt;
    public static bool IsOther => Current == ShiftingWorldMechanic.World.Other;

    public static event Action<ShiftingWorldMechanic.World> OnWorldChanged;

    private static float _enteredAt;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Current = ShiftingWorldMechanic.World.Normal;
        TimeInPreviousWorld = 0f;
        _enteredAt = 0f;
        OnWorldChanged = null;
    }

    public static void Initialize(ShiftingWorldMechanic.World world)
    {
        Current = world;
        TimeInPreviousWorld = 0f;
        _enteredAt = Time.time;
    }

    public static void Set(ShiftingWorldMechanic.World world)
    {
        if (world == Current) return;

        float now = Time.time;
        TimeInPreviousWorld = now - _enteredAt;
        _enteredAt = now;
        Current = world;

        OnWorldChanged?.Invoke(world);
    }

    public static bool Matches(WorldAffinity affinity)
    {
        switch (affinity)
        {
            case WorldAffinity.Normal: return Current == ShiftingWorldMechanic.World.Normal;
            case WorldAffinity.Other: return Current == ShiftingWorldMechanic.World.Other;
            default: return true;
        }
    }
}
