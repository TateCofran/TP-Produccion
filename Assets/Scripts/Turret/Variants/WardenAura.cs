using System.Collections.Generic;
using UnityEngine;

public static class WardenRegistry
{
    private static readonly Dictionary<Turret, int> Protectors = new();

    public static int ProtectedCount => Protectors.Count;

    public static bool IsProtected(Turret turret)
    {
        return turret != null && Protectors.TryGetValue(turret, out int count) && count > 0;
    }

    public static void Add(Turret turret)
    {
        if (turret == null) return;
        Protectors.TryGetValue(turret, out int count);
        Protectors[turret] = count + 1;
    }

    public static void Remove(Turret turret)
    {
        if (turret == null || !Protectors.TryGetValue(turret, out int count)) return;
        if (count <= 1) Protectors.Remove(turret);
        else Protectors[turret] = count - 1;
    }

    public static void ShowBlocked(Turret turret)
    {
        if (turret == null) return;
        Vector3 p = turret.transform.position + Vector3.up * 1f;
        FeedbackFxPool.MuzzleFlash(p, new Color(0.4f, 0.95f, 1f, 0.9f), 2.2f, 0.4f);
        FeedbackFxPool.Lightning(p + Vector3.left * 0.8f, p + Vector3.right * 0.8f, new Color(0.5f, 1f, 1f, 1f), 0.06f, 0.3f, 0.2f, 6);
        Log.Info(nameof(WardenRegistry), $"Blocked an attack on {turret.name}");
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Protectors.Clear();
    }
}

[DisallowMultipleComponent]
public class WardenAura : MonoBehaviour, ITurretInfoProvider
{
    [Header("Area")]
    [Tooltip("Protection radius in cells. The turret data range overrides it when greater than zero.")]
    [SerializeField, Min(0.1f)] private float radiusInCells = 2.5f;
    [SerializeField, Min(0f)] private float radiusPerLevelCells = 0.25f;
    [SerializeField, Min(0.1f)] private float cellSize = 1f;
    [SerializeField, Min(0.05f)] private float refreshInterval = 0.5f;

    private readonly HashSet<Turret> protectedTurrets = new();
    private readonly List<Turret> scratch = new();
    private readonly List<Transform> linkTargets = new();
    private Turret turret;
    private TurretStats stats;
    private TurretDataHolder holder;
    private AuraVisual visual;
    private float nextRefresh;
    private float radiusMultiplier = 1f;

    public float RadiusMultiplier { get => radiusMultiplier; set => radiusMultiplier = Mathf.Max(0f, value); }
    public int ProtectedCount => protectedTurrets.Count;

    public float Radius
    {
        get
        {
            float cells = holder != null && holder.turretDataSO != null && holder.turretDataSO.range > 0f ? holder.turretDataSO.range : radiusInCells;
            int level = stats != null ? Mathf.Max(1, stats.UpgradeLevel) : 1;
            return (cells + radiusPerLevelCells * (level - 1)) * cellSize * radiusMultiplier;
        }
    }

    private void Awake()
    {
        turret = GetComponent<Turret>();
        stats = GetComponent<TurretStats>();
        holder = GetComponent<TurretDataHolder>();
        visual = GetComponent<AuraVisual>();
        if (visual == null) visual = gameObject.AddComponent<AuraVisual>();
        visual.SetColor(new Color(0.4f, 0.95f, 1f, 0.55f));
    }

    private void OnDisable()
    {
        ClearAll();
    }

    private void OnDestroy()
    {
        ClearAll();
    }

    private void Update()
    {
        if (turret != null && !turret.IsPlaced) return;
        if (Time.time < nextRefresh) return;
        nextRefresh = Time.time + refreshInterval;
        Refresh();
    }

    public void Refresh()
    {
        float r = Radius;
        float r2 = r * r;
        Vector3 c = transform.position;

        scratch.Clear();
        foreach (var t in FindObjectsByType<Turret>(FindObjectsSortMode.None))
        {
            if (t == null || t == turret || !t.IsPlaced) continue;
            Vector3 d = t.transform.position - c;
            d.y = 0f;
            if (d.sqrMagnitude <= r2) scratch.Add(t);
        }

        var toRemove = new List<Turret>();
        foreach (var t in protectedTurrets) if (t == null || !scratch.Contains(t)) toRemove.Add(t);
        foreach (var t in toRemove)
        {
            WardenRegistry.Remove(t);
            protectedTurrets.Remove(t);
        }

        linkTargets.Clear();
        foreach (var t in scratch)
        {
            if (protectedTurrets.Add(t)) WardenRegistry.Add(t);
            linkTargets.Add(t.transform);
        }

        if (visual != null)
        {
            visual.SetRadius(r);
            visual.SetLinks(linkTargets);
        }
    }

    private void ClearAll()
    {
        foreach (var t in protectedTurrets) WardenRegistry.Remove(t);
        protectedTurrets.Clear();
    }

    public string GetInfoLine()
    {
        return $"Protecting {protectedTurrets.Count} turrets (radius {Radius:0.#})";
    }
}
