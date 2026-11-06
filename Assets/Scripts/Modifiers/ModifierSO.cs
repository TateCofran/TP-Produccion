using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ModifierEntry
{
    public ModifierStat stat;
    public float value;
}

[CreateAssetMenu(menuName = "Modifiers/Modifier")]
public class ModifierSO : ScriptableObject, IGameModifier
{
    [Header("Identity")]
    public string id;
    public string displayName;
    [TextArea] public string description;
    public Sprite icon;
    public ModifierCategory category = ModifierCategory.NormalWorld;

    [Header("Effects")]
    public List<ModifierEntry> entries = new List<ModifierEntry>();

    public string Name => displayName;
    public string Description => description;
    public ModifierCategory Category => category;

    public void Apply(GameModifiersManager manager)
    {
        if (manager == null || entries == null) return;
        foreach (var entry in entries)
        {
            if (entry == null) continue;
            manager.AddStat(entry.stat, entry.value);
        }
    }

    public string GetStackDescription(int stacks) => description;

    public bool Affects(ModifierStat stat)
    {
        if (entries == null) return false;
        foreach (var entry in entries)
            if (entry != null && entry.stat == stat) return true;
        return false;
    }
}
