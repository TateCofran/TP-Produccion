using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class SynergyService : MonoBehaviour
{
    public static SynergyService Instance { get; private set; }

    [Header("Refs")]
    [SerializeField] private SynergyTableSO table;
    [SerializeField] private GridGenerator grid;

    [Header("Neighbour Search")]
    [Tooltip("Size of the box around the turret cell, in cells.")]
    [SerializeField, Min(0.5f)] private float searchSizeInCells = 1.5f;
    [SerializeField, Min(0.1f)] private float cellSize = 1f;
    [SerializeField] private LayerMask pathLayers = ~0;

    private static readonly Collider[] Buffer = new Collider[64];
    private readonly HashSet<PathCellEffect> cellScratch = new();

    public SynergyTableSO Table => table;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        if (grid == null) grid = FindFirstObjectByType<GridGenerator>();
    }

    private void OnEnable()
    {
        CellSlot.OnTurretPlaced += HandleTurretPlaced;
        if (grid != null) grid.OnTilePlaced += HandleTilePlaced;
    }

    private void OnDisable()
    {
        CellSlot.OnTurretPlaced -= HandleTurretPlaced;
        if (grid != null) grid.OnTilePlaced -= HandleTilePlaced;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void HandleTurretPlaced(CellSlot slot, GameObject turret)
    {
        Refresh(slot, turret);
    }

    private void HandleTilePlaced(PlacedTile tile)
    {
        RefreshAll();
    }

    public void RefreshAll()
    {
        foreach (var slot in FindObjectsByType<CellSlot>(FindObjectsSortMode.None))
            if (slot.IsOccupied) Refresh(slot, slot.GetCurrentTurret());
    }

    public void Refresh(CellSlot slot, GameObject turret)
    {
        if (slot == null || turret == null || table == null) return;
        var holder = turret.GetComponent<TurretDataHolder>();
        if (holder == null || holder.turretDataSO == null) return;

        var rules = GetActiveSynergies(slot, holder.turretDataSO, out var cells);
        var synergy = turret.GetComponent<TurretSynergy>();
        if (synergy == null)
        {
            if (rules.Count == 0) return;
            synergy = turret.AddComponent<TurretSynergy>();
        }
        synergy.SetActive(rules, cells);
    }

    public List<SynergyRule> GetActiveSynergies(CellSlot slot, TurretType type)
    {
        var fake = ScriptableObject.CreateInstance<TurretDataSO>();
        fake.turretType = type;
        var result = GetActiveSynergies(slot, fake, out _);
        Destroy(fake);
        return result;
    }

    public List<SynergyRule> GetActiveSynergies(CellSlot slot, TurretDataSO turret, out List<Vector3> sourceCells)
    {
        var result = new List<SynergyRule>();
        sourceCells = new List<Vector3>();
        if (slot == null || turret == null || table == null) return result;

        cellScratch.Clear();
        Vector3 center = slot.transform.position;
        Vector3 half = new Vector3(searchSizeInCells * cellSize * 0.5f, 1.5f, searchSizeInCells * cellSize * 0.5f);
        int count = Physics.OverlapBoxNonAlloc(center, half, Buffer, Quaternion.identity, pathLayers, QueryTriggerInteraction.Collide);
        for (int i = 0; i < count; i++)
        {
            var cell = Buffer[i].GetComponentInParent<PathCellEffect>();
            if (cell != null) cellScratch.Add(cell);
        }

        foreach (var rule in table.RulesFor(turret.turretType))
        {
            foreach (var cell in cellScratch)
            {
                if (!CellMatches(cell, rule, turret)) continue;
                if (!result.Contains(rule)) result.Add(rule);
                sourceCells.Add(cell.transform.position);
            }
        }
        return result;
    }

    private static bool CellMatches(PathCellEffect cell, SynergyRule rule, TurretDataSO turret)
    {
        foreach (var kind in cell.Kinds)
        {
            if (kind == TileEffectKind.WorldLockedSameWorld)
            {
                if (rule.requiredEffect == TileEffectKind.WorldLockedSameWorld && turret.affinity != WorldAffinity.Neutral && cell.activeIn == turret.affinity)
                    return true;
                continue;
            }
            if (rule.Accepts(kind)) return true;
        }
        return false;
    }
}
