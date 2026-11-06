using UnityEngine;
using UnityEngine.Events;

public class WorldSwitchPoints : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("ShiftingWorldMechanic from the scene.")]
    [SerializeField] private ShiftingWorldMechanic mechanic;
    [SerializeField] private EssencePopupUI essenceUI;

    [Header("Permanent Upgrade Bonuses")]
    [Tooltip("ID of the Lab upgrade that increases blue essences per kill.")]
    [SerializeField] private string labBlueUpgradeId = "upgrade_moreEssencePerSwitchTile";
    [Tooltip("Bonus per level (0.1 = +10% per level).")]
    [Range(0f, 5f)][SerializeField] private float blueBonusPerLevel = 0.1f;

    [Tooltip("ID of the Workshop upgrade that increases red essences per kill.")]
    [SerializeField] private string workshopRedUpgradeId = "upgrade_moreEssencePerSwitchTurret";
    [Range(0f, 5f)][SerializeField] private float redBonusPerLevel = 0.1f;

    [Header("Totals")]
    [SerializeField] private int totalBlueEssences = 0;
    [SerializeField] private int totalRedEssences = 0;

    [System.Serializable] public class EssenceEvent : UnityEvent<int, int> { }
    public EssenceEvent OnBlueEssenceGained;
    public EssenceEvent OnRedEssenceGained;

    public int TotalBlue => totalBlueEssences;
    public int TotalRed => totalRedEssences;

    private void Reset()
    {
        if (mechanic == null) mechanic = FindFirstObjectByType<ShiftingWorldMechanic>();
    }

    private void Awake()
    {
        if (mechanic == null) mechanic = FindFirstObjectByType<ShiftingWorldMechanic>();
    }

    private void OnEnable()
    {
        Enemy.OnAnyEnemyKilled += HandleEnemyKilled;
        WorldState.OnWorldChanged += HandleWorldChanged;
    }

    private void OnDisable()
    {
        Enemy.OnAnyEnemyKilled -= HandleEnemyKilled;
        WorldState.OnWorldChanged -= HandleWorldChanged;
    }

    private void Start()
    {
        if (essenceUI != null)
            essenceUI.SetWorld(WorldState.Current == ShiftingWorldMechanic.World.Normal);
    }

    private void HandleWorldChanged(ShiftingWorldMechanic.World world)
    {
        if (essenceUI != null)
            essenceUI.SetWorld(world == ShiftingWorldMechanic.World.Normal);
    }

    private void HandleEnemyKilled(Enemy e)
    {
        if (e == null || e.ReachedCore || e.IsSummoned || e.Data == null) return;

        bool isNormal = WorldState.Current == ShiftingWorldMechanic.World.Normal;
        float multiplier = isNormal ? GetBlueMultiplier() : GetRedMultiplier();
        int amount = Mathf.Max(1, Mathf.RoundToInt(e.Data.essenceReward * multiplier));

        if (isNormal)
        {
            totalBlueEssences += amount;
            OnBlueEssenceGained?.Invoke(amount, totalBlueEssences);
            if (essenceUI != null) essenceUI.QueueEssence(amount, totalBlueEssences);
        }
        else
        {
            totalRedEssences += amount;
            OnRedEssenceGained?.Invoke(amount, totalRedEssences);
            if (essenceUI != null) essenceUI.QueueEssence(amount, totalRedEssences);
        }
    }

    private float GetBlueMultiplier()
    {
        int level = UpgradeLevels.Get(labBlueUpgradeId);
        float modifier = GameModifiersManager.Instance != null ? GameModifiersManager.Instance.blueEssenceMultiplier : 1f;
        return (1f + blueBonusPerLevel * level) * modifier;
    }

    private float GetRedMultiplier()
    {
        int level = UpgradeLevels.Get(workshopRedUpgradeId);
        float modifier = GameModifiersManager.Instance != null ? GameModifiersManager.Instance.redEssenceMultiplier : 1f;
        return (1f + redBonusPerLevel * level) * modifier;
    }

    public void AddBlueEssence(int amount)
    {
        if (amount <= 0) return;
        totalBlueEssences += amount;
        OnBlueEssenceGained?.Invoke(amount, totalBlueEssences);
    }

    public void AddRedEssence(int amount)
    {
        if (amount <= 0) return;
        totalRedEssences += amount;
        OnRedEssenceGained?.Invoke(amount, totalRedEssences);
    }

    public void ResetTotals()
    {
        totalBlueEssences = 0;
        totalRedEssences = 0;
        if (essenceUI != null) essenceUI.ResetTotals();
    }
}
