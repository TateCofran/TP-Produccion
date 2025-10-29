using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameModifiersManager : MonoBehaviour
{
    public static GameModifiersManager Instance;

    // Stats globales
    public float turretRangeMultiplier = 1f;
    public float turretFireRateMultiplier = 1f;
    public float turretDamageMultiplier = 1f;

    public float enemyCountMultiplier = 1f;
    public float enemySpeedMultiplier = 1f;
    public float enemyDamageTakenMultiplier = 1f; // Agregala junto a las otras globales

    // Listas separadas de modificadores según categoría
    public List<IGameModifier> modifiersNormalWorld = new List<IGameModifier>();
    public List<IGameModifier> modifiersOtherWorld = new List<IGameModifier>();
    public List<IGameModifier> modifiersShiftWorld = new List<IGameModifier>();
    //public List<IGameModifier> modifiersgeneric = new List<IGameModifier>();

    // Todos juntos si necesitás iterar o mostrar todos
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
        /*
        modifiersNormalWorld = new List<IGameModifier>
        {
            new LightTreasureModifier(),
            new ApparentPeaceModifier(),
            new SolarEnergyModifier(),
        };

        modifiersOtherWorld = new List<IGameModifier>
        {
            new VoidWaveModifier(),
            new EnergeticChaosModifier(),
            new BulletHellModifier(),
        };

        modifiersShiftWorld = new List<IGameModifier>
        {
            new EnergySynchronyModifier(),
            new ChaosAddictionModifier(),
            new DimensionalExplorerModifier(),
            new GoldBonusWithFasterEnemiesModifier(),
            new InstantGoldBoostModifier(),
        };

        // Si querés tener una lista combinada para usar en UI, random, etc.
        allModifiers = new List<IGameModifier>();
        allModifiers.AddRange(modifiersNormalWorld);
        allModifiers.AddRange(modifiersOtherWorld);
        allModifiers.AddRange(modifiersShiftWorld);*/
    }

    // Aplicar un modificador (puede recibirlo de una fábrica, UI, etc.)
    public void ApplyModifier(IGameModifier modifier)
    {
        appliedModifiers.Add(modifier);
        modifier.Apply(this);

        // Registrar el conteo
        var modType = modifier.GetType();
        if (modifierUsageCount.ContainsKey(modType))
            modifierUsageCount[modType]++;
        else
            modifierUsageCount[modType] = 1;
        /*
        // Chequeo legendario Normal
        if (!legendaryNormalGiven &&
            modifier.Category == ModifierCategory.NormalWorld &&
            appliedModifiers.Count(m => m.Category == ModifierCategory.NormalWorld) == 5)
        {
            AddLegendaryModifier(ModifierCategory.LegendaryNormal);
            legendaryNormalGiven = true;
            DisableLegendaryOfCategory(ModifierCategory.LegendaryOther);
        }

        // Chequeo legendario Other
        if (!legendaryOtherGiven &&
            modifier.Category == ModifierCategory.OtherWorld &&
            appliedModifiers.Count(m => m.Category == ModifierCategory.OtherWorld) == 5)
        {
            AddLegendaryModifier(ModifierCategory.LegendaryOther);
            legendaryOtherGiven = true;
            DisableLegendaryOfCategory(ModifierCategory.LegendaryNormal);
        }

        // Notificar a todas las torretas para actualizar stats
        foreach (var turret in TurretManager.Instance.GetAllTurrets())
            turret.Stats.RecalculateStats();        */
    }

    /*
        private void AddLegendaryModifier(ModifierCategory category)
        {
            if (appliedModifiers.Count(m => m.Category == category) >= MaxLegendary)
                return;

            IGameModifier legendary = null;
            if (category == ModifierCategory.LegendaryNormal)
                legendary = new LegendaryNormalModifier();
            else if (category == ModifierCategory.LegendaryOther)
                legendary = new LegendaryOtherModifier();

            if (legendary != null)
            {
                appliedModifiers.Add(legendary);
                legendary.Apply(this);

                // Mostrar la carta en pantalla
                modifierPanelSelection?.ShowLegendaryModifier(legendary);
            }
        }

        private void DisableLegendaryOfCategory(ModifierCategory category)
        {
            // Esto depende de tu UI. Lo mínimo: remové del pool para que no se vuelva a elegir.
            allModifiers.RemoveAll(m => m.Category == category);
        }

        public bool WillAddLegendaryAfter(IGameModifier modifier)
        {
            if (modifier.Category == ModifierCategory.NormalWorld)
                return !legendaryNormalGiven &&
                       appliedModifiers.Count(m => m.Category == ModifierCategory.NormalWorld) == 4;
            if (modifier.Category == ModifierCategory.OtherWorld)
                return !legendaryOtherGiven &&
                       appliedModifiers.Count(m => m.Category == ModifierCategory.OtherWorld) == 4;
            return false;
        }*/

    // Si necesitás saber qué modificadores hay:
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

    // Método para obtener cuántas veces se utilizó un modificador específico
    public int GetModifierUsageCount<T>() where T : IGameModifier
    {
        var modType = typeof(T);
        return modifierUsageCount.TryGetValue(modType, out int count) ? count : 0;
    }

    // Método para obtener diccionario completo (para Analytics)
    public Dictionary<string, int> GetAllModifierUsageCounts()
    {
        return modifierUsageCount.ToDictionary(k => k.Key.Name, v => v.Value);
    }
}
