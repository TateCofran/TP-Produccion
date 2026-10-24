using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameModifiersManager : MonoBehaviour
{
    public static GameModifiersManager Instance;

    public float turretRangeMultiplier = 1f;
    public float turretFireRateMultiplier = 1f;
    public float turretDamageMultiplier = 1f;

    public float enemyCountMultiplier = 1f;
    public float enemySpeedMultiplier = 1f;
    public float enemyDamageTakenMultiplier = 1f;

    public List<IGameModifier> modifiersNormalWorld = new List<IGameModifier>();
    public List<IGameModifier> modifiersOtherWorld = new List<IGameModifier>();
    public List<IGameModifier> modifiersShiftWorld = new List<IGameModifier>();

    public List<IGameModifier> allModifiers = new List<IGameModifier>();

    private List<IGameModifier> appliedModifiers = new List<IGameModifier>();

    private ModifierPanelSelection modifierPanelSelection;

    private Dictionary<System.Type, int> modifierUsageCount = new Dictionary<System.Type, int>();

    public void InjectModifierPanelSelection(ModifierPanelSelection panel)
    {
        modifierPanelSelection = panel;
    }

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void ApplyModifier(IGameModifier modifier)
    {
        appliedModifiers.Add(modifier);
        modifier.Apply(this);

        var modType = modifier.GetType();
        if (modifierUsageCount.ContainsKey(modType))
            modifierUsageCount[modType]++;
        else
            modifierUsageCount[modType] = 1;
    }

    public IReadOnlyList<IGameModifier> GetModifiers() => appliedModifiers.AsReadOnly();

    public bool HasModifier<T>() where T : IGameModifier
    {
        return appliedModifiers.Exists(m => m is T);
    }

    public int GetModifierStacks(IGameModifier modifier)
    {
        int count = 0;
        foreach (var mod in appliedModifiers)
        {
            if (mod.GetType() == modifier.GetType())
                count++;
        }
        return count;
    }

    public int GetModifierUsageCount<T>() where T : IGameModifier
    {
        var modType = typeof(T);
        return modifierUsageCount.TryGetValue(modType, out int count) ? count : 0;
    }

    public Dictionary<string, int> GetAllModifierUsageCounts()
    {
        return modifierUsageCount.ToDictionary(k => k.Key.Name, v => v.Value);
    }
}
