using UnityEngine;
using UnityEngine.Events;

public class WorldSwitchPoints : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("ShiftingWorldMechanic from the scene.")]
    [SerializeField] private ShiftingWorldMechanic mechanic;
    [SerializeField] private EssencePopupUI essenceUI;

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
        if (essenceUI == null) return;
        essenceUI.SetWorld(world == ShiftingWorldMechanic.World.Normal);
        essenceUI.SetTotals(totalBlueEssences, totalRedEssences);
    }

    private void HandleEnemyKilled(Enemy e)
    {
        if (e == null || e.ReachedCore || e.IsSummoned || e.Data == null) return;

        bool isNormal = WorldState.Current == ShiftingWorldMechanic.World.Normal;
        float multiplier = isNormal ? GetBlueMultiplier() : GetRedMultiplier();
        if (e.LastHitSource is ReaperTurret reaper && reaper != null) multiplier *= reaper.EssenceMultiplier;
        if (e.IsElite) multiplier *= EliteController.EssenceMultiplier;
        int amount = Mathf.Max(1, Mathf.RoundToInt(e.Data.essenceReward * multiplier));

        if (isNormal)
        {
            totalBlueEssences += amount;
            OnBlueEssenceGained?.Invoke(amount, totalBlueEssences);
        }
        else
        {
            totalRedEssences += amount;
            OnRedEssenceGained?.Invoke(amount, totalRedEssences);
        }

        var orbs = EssenceOrbFX.Instance;
        if (orbs != null && orbs.CanSpawn)
            orbs.Spawn(e.transform.position, amount, isNormal);
        else if (essenceUI != null)
            essenceUI.QueueEssence(amount, isNormal ? totalBlueEssences : totalRedEssences);

        EssenceChime.PlayKill();
    }

    private float GetBlueMultiplier()
    {
        return GameModifiersManager.Instance != null ? GameModifiersManager.Instance.blueEssenceMultiplier : 1f;
    }

    private float GetRedMultiplier()
    {
        return GameModifiersManager.Instance != null ? GameModifiersManager.Instance.redEssenceMultiplier : 1f;
    }

    public void GrantWithOrb(int amount, bool blue, Vector3 worldPosition)
    {
        if (amount <= 0) return;
        if (blue)
        {
            totalBlueEssences += amount;
            OnBlueEssenceGained?.Invoke(amount, totalBlueEssences);
        }
        else
        {
            totalRedEssences += amount;
            OnRedEssenceGained?.Invoke(amount, totalRedEssences);
        }

        var orbs = EssenceOrbFX.Instance;
        if (orbs != null && orbs.CanSpawn) orbs.Spawn(worldPosition, amount, blue);
        else NotifyExternalGain(amount, blue);
    }

    public void AddBlueEssence(int amount)
    {
        if (amount <= 0) return;
        totalBlueEssences += amount;
        OnBlueEssenceGained?.Invoke(amount, totalBlueEssences);
        NotifyExternalGain(amount, true);
    }

    public void AddRedEssence(int amount)
    {
        if (amount <= 0) return;
        totalRedEssences += amount;
        OnRedEssenceGained?.Invoke(amount, totalRedEssences);
        NotifyExternalGain(amount, false);
    }

    private void NotifyExternalGain(int amount, bool isBlue)
    {
        if (essenceUI == null) return;

        bool showingBlue = WorldState.Current == ShiftingWorldMechanic.World.Normal;
        if (showingBlue == isBlue)
            essenceUI.QueueEssence(amount, isBlue ? totalBlueEssences : totalRedEssences);
        else
            essenceUI.SetTotals(totalBlueEssences, totalRedEssences);
    }

    public void ResetTotals()
    {
        totalBlueEssences = 0;
        totalRedEssences = 0;
        if (essenceUI != null) essenceUI.ResetTotals();
    }
}
