using System;
using System.Collections.Generic;
using UnityEngine;

public static class EscapeRouter
{
    public const int PriorityPopup = 300;
    public const int PriorityPlacement = 200;
    public const int PrioritySelection = 100;

    private struct Entry
    {
        public int priority;
        public Func<bool> handler;
    }

    private static readonly List<Entry> entries = new();

    public static void Register(Func<bool> handler, int priority)
    {
        if (handler == null) return;
        Unregister(handler);
        entries.Add(new Entry { priority = priority, handler = handler });
        entries.Sort((a, b) => b.priority.CompareTo(a.priority));
    }

    public static void Unregister(Func<bool> handler)
    {
        if (handler == null) return;
        entries.RemoveAll(e => e.handler == handler);
    }

    public static bool TryConsume()
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].handler()) return true;
        }
        return false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        entries.Clear();
    }
}
