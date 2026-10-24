using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GridGenerator : MonoBehaviour, ITileGenerator
{
    [Header("Dependencies")][SerializeField] private TilePoolManager tilePoolManager;
    [SerializeField] private SpatialPartitioner spatialPartitioner;
    [SerializeField] private TileOrientationCalculator orientationCalculator;
    [SerializeField] private bool generateOnStart = true;

    [Header("Layouts")][SerializeField] private TileLayout initialLayout;
    [SerializeField] private List<TileLayout> candidateLayouts = new();

    [Header("Prefabs")][SerializeField] private GameObject grassPrefab, pathBasicPrefab, pathDamagePrefab, pathSlowPrefab, pathStunPrefab;

    [Header("Core")][SerializeField] private GameObject corePrefab; [SerializeField] private Vector3 coreOffset = Vector3.zero;

    [Header("Parent / Output")][SerializeField] private Transform tilesRoot; [SerializeField] private string rootName = "TileChainRoot"; [SerializeField] private string tileGroupBaseName = "Tile_";

    [Header("Chain")][SerializeField] private bool clearOnStartGenerate = true; [SerializeField] private int connectionTries = 24;

    [Header("Per Cell Offsets")][SerializeField] private Vector3 grassCellOffset = Vector3.zero; [SerializeField] private Vector3 pathCellOffset = Vector3.zero;

    [Header("Offset Between Tiles")][SerializeField] private float tileGap = 0.25f; [SerializeField] private Vector3 extraTileOffset = Vector3.zero;

    [Header("Orientations")][SerializeField] private bool allowRotations = true; [SerializeField] private bool allowFlip = true; [SerializeField] private bool worldPlusZUsesYZeroEdge = true;

    [Header("UI / Exits")][SerializeField] private int selectedExitIndex = 0;

    [Header("Spawn/Runner")]
    [Tooltip("Spawn points with a shorter route to the core (in cells) are skipped while farther ones exist. Blocked exits this close become dead ends instead of spawns.")]
    [SerializeField, Min(0)] private int minSpawnRouteCells = 5;
    [Tooltip("Max distance, as a fraction of a cell, to join a blocked exit with a path cell of the neighbouring tile.")]
    [SerializeField, Range(0.1f, 1f)] private float joinTolerance = 0.6f;
    [SerializeField] private GameObject exitRunnerPrefab; [SerializeField] private float runnerYOffset = 0.05f; [SerializeField] private bool autoSpawnRunnersOnAppend = false;

    [Header("Tile Dupe System")][SerializeField] private TileDupeSystem tileDupeSystem;
    [SerializeField] private TileDataSO defaultTileData;
    [SerializeField] private List<TileDataBinding> tileDataBindings = new();

    [SerializeField] private TilePlacementSequencer placementSequencer;

    [Header("Permanent Spawn Points")]
    [SerializeField] private GameObject permanentSpawnPrefab;
    [SerializeField] private float permanentSpawnYOffset = 0.1f;
    [SerializeField] private Transform permanentSpawnRoot;
    private int _rrIndex = 0;

    private readonly List<Vector3> _permanentSpawnPoints = new();
    private readonly List<GameObject> _spawnPrefabsInScene = new();

    private IChainRepository chain;
    private IExitRepository exits;
    private ISpatialOverlapChecker overlap;
    private IOrientationService orient;

    private IPrefabSelector prefabs;
    private ILayoutInstantiator instantiator;
    private IPlacementCalculator placement;
    private ICandidateProvider candidates;

    private ICoreService core;
    private TileLinkRepository links;
    private IRunnerService runners;

    private System.Random rng = new System.Random();

    [Serializable]
    public class TileDataBinding
    {
        public TileLayout layout;
        public TileDataSO tileData;
    }

    private void Awake()
    {
        InitializeDependencies();
        if (tileDupeSystem == null) tileDupeSystem = FindFirstObjectByType<TileDupeSystem>();
    }

    private void InitializeDependencies()
    {
        if (tilePoolManager == null) tilePoolManager = GetComponent<TilePoolManager>();
        if (spatialPartitioner == null) spatialPartitioner = GetComponent<SpatialPartitioner>();
        if (orientationCalculator == null) orientationCalculator = GetComponent<TileOrientationCalculator>();

        orient = new OrientationAdapter(orientationCalculator);
        chain = new ChainRepository();
        exits = new ExitRepository(orient);

        overlap = new SpatialOverlapAdapter(spatialPartitioner);

        if (tilePoolManager != null)
        {
            tilePoolManager.InitializePool(grassPrefab, 64);
            tilePoolManager.InitializePool(pathBasicPrefab, 64);
            tilePoolManager.InitializePool(pathDamagePrefab, 32);
            tilePoolManager.InitializePool(pathSlowPrefab, 32);
            tilePoolManager.InitializePool(pathStunPrefab, 32);
        }

        prefabs = new PrefabSelector(grassPrefab, pathBasicPrefab, pathDamagePrefab, pathSlowPrefab, pathStunPrefab);
        instantiator = new LayoutInstantiator(prefabs, orient, tilePoolManager, grassCellOffset, pathCellOffset);

        placement = new PlacementCalculator(tileGap, extraTileOffset);
        candidates = new CandidateProvider(worldPlusZUsesYZeroEdge);

        core = new CoreService(corePrefab, tilesRoot);

        links = new TileLinkRepository();

        runners = new RunnerService(
            exits, chain, core, orient,
            links,
            exitRunnerPrefab,
            runnerYOffset
        );

        if (placementSequencer == null) placementSequencer = GetComponent<TilePlacementSequencer>();
    }

    private void Start()
    {
        if (generateOnStart) GenerateFirst();
    }

    private float GetGlobalCellSize() => (initialLayout ? initialLayout.cellSize : 1f);

    public void GenerateFirst()
    {
        if (!CheckPrefabs()) return;
        if (initialLayout == null) { Debug.LogError("[GridGenerator] initialLayout is not assigned"); return; }

        if (clearOnStartGenerate)
        {
            ClearRootChildren();
            exits.Clear();
            chain.Clear();
            links.Clear();
            overlap.Clear();
            core?.ClearCore();
        }

        tilesRoot = EnsureRoot(tilesRoot, rootName);

        var group = CreateTileParent(tilesRoot, $"{tileGroupBaseName}{chain.Count}");

        int rot = 0;
        bool flip = false;
        Vector3 origin = initialLayout.origin;

        int count = instantiator.InstantiateLayout(initialLayout, origin, rot, flip, group);

        if (placementSequencer != null)
        {
            float startDelay = placementSequencer.GetNextTileStartDelay(chain.Count);
            placementSequencer.PlayForGroup(group, startDelay);
        }
        if (corePrefab != null)
        {
            Vector2Int coreCell = initialLayout.hasCore ? initialLayout.GetCoreCell() : initialLayout.entry;
            if (initialLayout.IsInside(coreCell) && initialLayout.IsPath(coreCell))
            {
                Vector3 corePos = origin + orient.CellToWorldLocal(coreCell, initialLayout, rot, flip) + coreOffset;
                core.SpawnCore(corePos);
            }
        }

        var od = orient.GetOrientedData(initialLayout, rot, flip);
        placement.ComputeAABB(origin, od.w, od.h, initialLayout.cellSize, out var aMin, out var aMax);

        var placed = new PlacedTile
        {
            layout = initialLayout,
            worldOrigin = origin,
            rotSteps = rot,
            flipped = flip,
            parent = group,
            aabbMin = aMin,
            aabbMax = aMax
        };
        chain.Add(placed);
        overlap.Add(placed, chain.Count - 1);

        AddTileExitsToPool(chain.Count - 1, excludeAssetCell: null);
        exits.Relabel();

        AttachAndApplyDupe(group, initialLayout);

        ResolveBlockedExits();
        exits.Relabel();
    }

    public void AppendNextUsingSelectedExit()
    {
        int global = exits.GlobalIndexFromAvailable(selectedExitIndex);
        if (global < 0) { Debug.LogWarning("[GridGenerator] No exits available"); return; }
        if (TryAppendAtExit(global)) { if (autoSpawnRunnersOnAppend) runners.SpawnRunnersAtAllOpenExits(); }
        else if (!HasAnyValidPlacementForExit(exits.Get(global))) { exits.MarkClosed(global); exits.Relabel(); selectedExitIndex = 0; }
    }

    public IEnumerator AppendNextAsync()
    {
        int global = exits.GlobalIndexFromAvailable(selectedExitIndex);
        if (global < 0 || exits.Count == 0) yield break;
        if (exits.Get(global).Used) yield break;
        bool success = false;
        for (int i = 0; i < connectionTries; i++) { if (TryAppendAtExit(global)) { success = true; break; } yield return null; }
        if (success) { if (autoSpawnRunnersOnAppend) runners.SpawnRunnersAtAllOpenExits(); }
        else if (!HasAnyValidPlacementForExit(exits.Get(global))) { exits.MarkClosed(global); exits.Relabel(); selectedExitIndex = 0; }
    }

    public List<PlacementPreview> GetPlacementPreviews()
    {
        var previews = new List<PlacementPreview>();
        int global = exits.GlobalIndexFromAvailable(selectedExitIndex);
        if (global < 0 || exits.Count == 0) return previews;
        var chosen = exits.Get(global); if (chosen.Used) return previews;

        var prevTile = chain.Get(chosen.tileIndex); var layoutPrev = prevTile.layout; var exitCell = chosen.cell;
        if (!layoutPrev.IsInside(exitCell) || !layoutPrev.IsPath(exitCell)) return previews;

        var nb = FindSinglePathNeighbor(layoutPrev, exitCell); if (!nb.HasValue) return previews;
        Vector2Int dirOut = ClampToOrtho(orient.ApplyToDir(exitCell - nb.Value, prevTile.rotSteps, prevTile.flipped));
        Vector3 prevExitWorld = prevTile.worldOrigin + orient.CellToWorldLocal(exitCell, layoutPrev, prevTile.rotSteps, prevTile.flipped);

        foreach (var (candidate, rot, flip) in candidates.GetValidCandidates(candidateLayouts, dirOut, allowRotations, allowFlip, orient))
        {
            var o = orient.GetOrientedData(candidate, rot, flip);
            var newOrigin = placement.ComputeNewOrigin(candidate, rot, o.entryOriented, dirOut, prevTile.aabbMin, prevTile.aabbMax, prevExitWorld);
            placement.ComputeAABB(newOrigin, o.w, o.h, candidate.cellSize, out var nMin, out var nMax);
            bool noOverlap = !overlap.OverlapsAny(nMin, nMax);
            var status = noOverlap ? PreviewStatus.Valid : PreviewStatus.Overlap;
            previews.Add(new PlacementPreview { origin = newOrigin, sizeXZ = new Vector2(o.w * candidate.cellSize, o.h * candidate.cellSize), valid = noOverlap, status = status, note = $"{candidate.name} | {rot * 90} deg | flip={(flip ? 1 : 0)}", cellSize = candidate.cellSize });
        }
        return previews;
    }

    public bool TryGetSelectedExitWorld(out Vector3 pos, out Vector2Int dirOutWorld)
    {
        pos = Vector3.zero; dirOutWorld = Vector2Int.zero;
        int global = exits.GlobalIndexFromAvailable(selectedExitIndex);
        if (global < 0 || exits.Count == 0) return false;
        var chosen = exits.Get(global); if (chosen.Used) return false;

        var prevTile = chain.Get(chosen.tileIndex); var layoutPrev = prevTile.layout; var exitCell = chosen.cell;
        if (!layoutPrev.IsInside(exitCell) || !layoutPrev.IsPath(exitCell)) return false;
        var nb = FindSinglePathNeighbor(layoutPrev, exitCell); if (!nb.HasValue) return false;

        Vector2Int d = ClampToOrtho(orient.ApplyInverseToDir(exitCell - nb.Value, prevTile.rotSteps, prevTile.flipped));
        pos = prevTile.worldOrigin + orient.CellToWorldLocal(exitCell, layoutPrev, prevTile.rotSteps, prevTile.flipped);
        dirOutWorld = d; return true;
    }

    public List<(string label, Vector3 worldPos)> GetAvailableExits()
    {
        return exits.GetAvailableWorld(chain.Get).ToList();
    }

    public List<string> GetAvailableExitLabels()
    {
        return exits.AvailableIndices().Select(i => exits.Get(i).label).ToList();
    }

    public void UI_SetExitIndex(int index) => selectedExitIndex = Mathf.Max(0, index);
    public void UI_SetExitByLabel(string label) { int idx = exits.AvailableIndexByLabel(label); if (idx >= 0) selectedExitIndex = idx; }
    public void UI_GenerateFirst() => GenerateFirst();
    public void UI_AppendNext() => AppendNextUsingSelectedExit();
    public void UI_AppendNextAsync() => StartCoroutine(AppendNextAsync());

    private bool TryGetExitFrame(ExitRecord rec, out Vector2Int dirOut, out Vector3 exitWorld)
    {
        dirOut = Vector2Int.zero;
        exitWorld = Vector3.zero;

        var pt = chain.Get(rec.tileIndex);
        var layout = pt.layout;
        if (layout == null || !layout.IsInside(rec.cell) || !layout.IsPath(rec.cell)) return false;

        var nb = FindSinglePathNeighbor(layout, rec.cell);
        if (!nb.HasValue) return false;

        dirOut = ClampToOrtho(orient.ApplyToDir(rec.cell - nb.Value, pt.rotSteps, pt.flipped));
        exitWorld = pt.worldOrigin + orient.CellToWorldLocal(rec.cell, layout, pt.rotSteps, pt.flipped);
        return true;
    }

    private bool TryFindPlacement(
        ExitRecord rec, IEnumerable<TileLayout> layouts, bool shuffle,
        out TileLayout layout, out int rot, out bool flip, out Vector3 origin, out Vector3 nMin, out Vector3 nMax)
    {
        layout = null;
        rot = 0;
        flip = false;
        origin = Vector3.zero;
        nMin = Vector3.zero;
        nMax = Vector3.zero;

        if (!TryGetExitFrame(rec, out var dirOut, out var exitWorld)) return false;
        var prevTile = chain.Get(rec.tileIndex);

        IEnumerable<(TileLayout layout, int rot, bool flip)> options =
            candidates.GetValidCandidates(layouts, dirOut, allowRotations, allowFlip, orient);
        if (shuffle) options = options.OrderBy(_ => rng.Next());

        int attempts = 0;
        foreach (var (candidate, r, f) in options)
        {
            var o = orient.GetOrientedData(candidate, r, f);
            var newOrigin = placement.ComputeNewOrigin(candidate, r, o.entryOriented, dirOut, prevTile.aabbMin, prevTile.aabbMax, exitWorld);
            placement.ComputeAABB(newOrigin, o.w, o.h, candidate.cellSize, out var mn, out var mx);

            if (overlap.OverlapsAny(mn, mx))
            {
                if (shuffle && ++attempts >= connectionTries) break;
                continue;
            }

            layout = candidate;
            rot = r;
            flip = f;
            origin = newOrigin;
            nMin = mn;
            nMax = mx;
            return true;
        }

        return false;
    }

    private bool TryAppendAtExit(int global)
    {
        var rec = exits.Get(global);
        if (rec.Used || rec.Closed) return false;

        if (!TryFindPlacement(rec, candidateLayouts, true, out var layout, out var rot, out var flip, out var origin, out var mn, out var mx))
            return false;

        PlaceTile(global, rec, layout, rot, flip, origin, mn, mx);
        return true;
    }

    private bool HasAnyValidPlacementForExit(ExitRecord rec)
        => TryFindPlacement(rec, candidateLayouts, false, out _, out _, out _, out _, out _, out _);

    private void PlaceTile(int exitGlobal, ExitRecord fromExit, TileLayout layout, int rot, bool flip, Vector3 origin, Vector3 nMin, Vector3 nMax)
    {
        var parent = CreateTileParent(tilesRoot, $"{tileGroupBaseName}{chain.Count}");
        int count = instantiator.InstantiateLayout(layout, origin, rot, flip, parent);

        if (placementSequencer != null)
        {
            float startDelay = placementSequencer.GetNextTileStartDelay(chain.Count);
            placementSequencer.PlayForGroup(parent, startDelay);
        }

        var placed = new PlacedTile { layout = layout, worldOrigin = origin, rotSteps = rot, flipped = flip, parent = parent, aabbMin = nMin, aabbMax = nMax };
        chain.Add(placed);
        int newIndex = chain.Count - 1;
        overlap.Add(placed, newIndex);

        links.Add(fromExit.tileIndex, fromExit.cell, newIndex, layout.entry);
        exits.MarkUsed(exitGlobal);

        AddTileExitsToPool(newIndex, layout.entry);
        ResolveBlockedExits();
        exits.Relabel();

        AttachAndApplyDupe(parent, layout);

        Debug.Log($"[GridGenerator] Connected via exit {fromExit.label}, new tile #{newIndex} (rot={rot * 90} deg, flip={flip}), instances: {count}");
        TutorialEventHub.RaiseTilePlaced();
    }

    private void AddTileExitsToPool(int tileIndex, Vector2Int? excludeAssetCell)
    {
        var pt = chain.Get(tileIndex);
        if (pt.layout == null || pt.layout.exits == null) return;

        foreach (var ex in pt.layout.exits)
        {
            if (!pt.layout.IsInside(ex) || !pt.layout.IsPath(ex)) continue;
            if (excludeAssetCell.HasValue && ex == excludeAssetCell.Value) continue;

            exits.Add(new ExitRecord
            {
                tileIndex = tileIndex,
                cell = ex,
                flags = 0,
                label = string.Empty
            });
        }
    }

    private static bool PointInAABB(Vector3 p, Vector3 min, Vector3 max, float eps = 0.0001f)
    {
        return p.x >= (min.x - eps) && p.x <= (max.x + eps) &&
               p.z >= (min.z - eps) && p.z <= (max.z + eps);
    }

    private bool ProbeExit(ExitRecord rec, out Vector2Int dirOut, out Vector3 exitWorld, out bool blocked, out int otherTile, out Vector2Int otherCell)
    {
        blocked = false;
        otherTile = -1;
        otherCell = default;

        if (!TryGetExitFrame(rec, out dirOut, out exitWorld)) return false;

        float cs = chain.Get(rec.tileIndex).layout.cellSize;
        Vector3 dir = new Vector3(dirOut.x, 0f, dirOut.y);
        Vector3 nextCell = exitWorld + dir * cs;
        Vector3 probe = exitWorld + dir * (cs + tileGap);

        float tolerance = cs * joinTolerance;
        float bestD2 = tolerance * tolerance;

        for (int i = 0; i < chain.Count; i++)
        {
            if (i == rec.tileIndex) continue;

            var other = chain.Get(i);
            if (other.layout == null) continue;
            if (!PointInAABB(nextCell, other.aabbMin, other.aabbMax) && !PointInAABB(probe, other.aabbMin, other.aabbMax)) continue;

            blocked = true;

            foreach (var t in other.layout.tiles)
            {
                if (t.type != TileLayout.TileType.Path) continue;

                Vector3 w = other.worldOrigin + orient.CellToWorldLocal(t.grid, other.layout, other.rotSteps, other.flipped);
                float dx = w.x - probe.x;
                float dz = w.z - probe.z;
                float d2 = dx * dx + dz * dz;
                if (d2 <= bestD2)
                {
                    bestD2 = d2;
                    otherTile = i;
                    otherCell = t.grid;
                }
            }
        }

        return true;
    }

    private int FindOpenExit(int tileIndex, Vector2Int cell)
    {
        for (int i = 0; i < exits.Count; i++)
        {
            var e = exits.Get(i);
            if (e.tileIndex == tileIndex && e.cell == cell && !e.Used && !e.Closed) return i;
        }
        return -1;
    }

    private void ResolveBlockedExits()
    {
        if (exits == null || chain == null) return;

        for (int i = 0; i < exits.Count; i++)
        {
            var rec = exits.Get(i);
            if (rec.Used || rec.Closed) continue;
            if (!ProbeExit(rec, out _, out _, out bool blocked, out int otherTile, out Vector2Int otherCell)) continue;
            if (!blocked || otherTile < 0) continue;

            links.Add(rec.tileIndex, rec.cell, otherTile, otherCell);
            exits.MarkUsed(i);

            int otherExit = FindOpenExit(otherTile, otherCell);
            if (otherExit >= 0) exits.MarkUsed(otherExit);

            Debug.Log($"[GridGenerator] Joined exit of tile #{rec.tileIndex} with the path of tile #{otherTile}");
        }

        for (int i = 0; i < exits.Count; i++)
        {
            var rec = exits.Get(i);
            if (rec.Used || rec.Closed) continue;
            if (!ProbeExit(rec, out var dirOut, out var exitWorld, out bool blocked, out _, out _)) continue;
            if (!blocked) continue;

            bool farEnough = runners != null && runners.TryGetRouteLength(exitWorld, out int cells) && cells >= minSpawnRouteCells;
            if (!farEnough)
            {
                exits.MarkClosed(i, "DEAD_END");
                Debug.Log($"[GridGenerator] Exit of tile #{rec.tileIndex} is blocked too close to the core. Closed as a dead end");
                continue;
            }

            if (!_permanentSpawnPoints.Any(p => Vector3.Distance(p, exitWorld) < 0.05f))
            {
                _permanentSpawnPoints.Add(exitWorld);
                CreatePermanentSpawnVisual(exitWorld, new Vector3(-dirOut.x, 0f, -dirOut.y));
            }

            exits.MarkClosed(i, "PERMANENT_SPAWN");
        }
    }

    private Transform EnsurePermanentSpawnRoot()
    {
        if (permanentSpawnRoot) return permanentSpawnRoot;
        var rootGO = GameObject.Find("PermanentSpawnRoot") ?? new GameObject("PermanentSpawnRoot");
        permanentSpawnRoot = rootGO.transform;
        return permanentSpawnRoot;
    }

    private void CreatePermanentSpawnVisual(Vector3 worldPos, Vector3 forwardDir)
    {
        if (!permanentSpawnPrefab) return;

        foreach (var existing in _spawnPrefabsInScene)
        {
            if (existing && Vector3.Distance(existing.transform.position, worldPos) < 0.05f)
                return;
        }

        var parent = EnsurePermanentSpawnRoot();
        var pos = worldPos + Vector3.up * permanentSpawnYOffset;

        if (forwardDir.sqrMagnitude < 0.0001f)
            forwardDir = Vector3.forward;

        var rot = Quaternion.LookRotation(forwardDir.normalized, Vector3.up);
        var go = Instantiate(permanentSpawnPrefab, pos, rot, parent);
        _spawnPrefabsInScene.Add(go);
    }

    private void ClearAll()
    {
        if (tilePoolManager) { for (int i = tilesRoot.childCount - 1; i >= 0; i--) Destroy(tilesRoot.GetChild(i).gameObject); }
        else { for (int i = tilesRoot.childCount - 1; i >= 0; i--) DestroyImmediate(tilesRoot.GetChild(i).gameObject); }

        chain.Clear(); links.Clear(); exits.Clear(); overlap.Clear(); core.ClearCore();

        for (int i = _spawnPrefabsInScene.Count - 1; i >= 0; i--)
        {
            var go = _spawnPrefabsInScene[i];
            if (go)
            {
#if UNITY_EDITOR
                DestroyImmediate(go);
#else
        Destroy(go);
#endif
            }
        }
        _spawnPrefabsInScene.Clear();
        _permanentSpawnPoints.Clear();

        if (permanentSpawnRoot)
        {
#if UNITY_EDITOR
            DestroyImmediate(permanentSpawnRoot.gameObject);
#else
    Destroy(permanentSpawnRoot.gameObject);
#endif
            permanentSpawnRoot = null;
        }
    }

    private bool CheckPrefabs()
    {
        bool ok = grassPrefab && pathBasicPrefab && pathDamagePrefab && pathSlowPrefab && pathStunPrefab;
        if (!ok) Debug.LogError("[GridGenerator] Assign every path and grass prefab");
        return ok;
    }

    private static Transform EnsureRoot(Transform root, string rootName)
    {
        if (root) return root; var found = GameObject.Find(rootName); if (!found) found = new GameObject(rootName); return found.transform;
    }

    private static Vector2Int? FindSinglePathNeighbor(TileLayout layout, Vector2Int cell)
    {
        int count = 0; Vector2Int found = default;
        foreach (var d in TileLayout.OrthoDirs) { var n = cell + d; if (layout.IsPath(n)) { count++; found = n; } }
        return count == 1 ? found : (Vector2Int?)null;
    }

    private static Vector2Int ClampToOrtho(Vector2Int v)
    {
        if (Mathf.Abs(v.x) > Mathf.Abs(v.y)) return new Vector2Int(Mathf.Sign(v.x) >= 0 ? 1 : -1, 0);
        if (Mathf.Abs(v.y) > 0) return new Vector2Int(0, Mathf.Sign(v.y) >= 0 ? 1 : -1);
        return Vector2Int.up;
    }

    private static Transform CreateTileParent(Transform root, string name)
    {
        var go = new GameObject(name);
        if (root != null)
            go.transform.SetParent(root, false);
        return go.transform;
    }

    private void ClearRootChildren()
    {
        if (tilesRoot == null) return;

        if (tilePoolManager == null)
        {
#if UNITY_EDITOR
            for (int i = tilesRoot.childCount - 1; i >= 0; i--)
                DestroyImmediate(tilesRoot.GetChild(i).gameObject);
#else
        for (int i = tilesRoot.childCount - 1; i >= 0; i--)
            Destroy(tilesRoot.GetChild(i).gameObject);
#endif
            return;
        }

        for (int i = tilesRoot.childCount - 1; i >= 0; i--)
        {
            var child = tilesRoot.GetChild(i).gameObject;

            if (child.CompareTag("Cell"))
            {
                tilePoolManager.ReturnToPool(grassPrefab, child);
                continue;
            }

            var eff = child.GetComponent<PathCellEffect>();
            if (eff != null)
            {
                GameObject prefabRef;
                if (eff.stun > 0f) prefabRef = pathStunPrefab;
                else if (eff.slow > 0f) prefabRef = pathSlowPrefab;
                else if (eff.damagePerHit > 0) prefabRef = pathDamagePrefab;
                else prefabRef = pathBasicPrefab;

                tilePoolManager.ReturnToPool(prefabRef, child);
                continue;
            }

#if UNITY_EDITOR
            DestroyImmediate(child);
#else
        Destroy(child);
#endif
        }
    }

    public TileLayout CurrentLayout
    {
        get
        {
            if (chain != null && chain.Count > 0)
                return chain.Get(chain.Count - 1).layout;
            return initialLayout;
        }
    }

    public List<TileLayout> GetRandomCandidateSet(int count)
    {
        var pool = new List<TileLayout>();
        foreach (var c in candidateLayouts)
            if (c != null && !pool.Contains(c) && LayoutFitsAnyExit(c)) pool.Add(c);

        for (int i = 0; i < pool.Count; i++)
        {
            int j = rng.Next(i, pool.Count);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        if (pool.Count > count)
            pool.RemoveRange(count, pool.Count - count);

        return pool;
    }

    public bool LayoutFitsAnyExit(TileLayout layout)
    {
        if (layout == null || exits == null) return false;

        var single = new[] { layout };
        foreach (int i in exits.AvailableIndices())
        {
            if (TryFindPlacement(exits.Get(i), single, false, out _, out _, out _, out _, out _, out _))
                return true;
        }
        return false;
    }

    public List<(string label, Vector3 worldPos)> GetAvailableExitsFor(TileLayout layout)
    {
        var result = new List<(string label, Vector3 worldPos)>();
        if (layout == null || exits == null) return result;

        var single = new[] { layout };
        foreach (int i in exits.AvailableIndices())
        {
            var rec = exits.Get(i);
            if (!TryFindPlacement(rec, single, false, out _, out _, out _, out _, out _, out _)) continue;

            var pt = chain.Get(rec.tileIndex);
            result.Add((rec.label, pt.worldOrigin + orient.CellToWorldLocal(rec.cell, pt.layout, pt.rotSteps, pt.flipped)));
        }
        return result;
    }

    public Vector3 CurrentWorldOrigin
    {
        get
        {
            if (chain != null && chain.Count > 0)
                return chain.Get(chain.Count - 1).worldOrigin;
            return Vector3.zero;
        }
    }

    public int CurrentRotSteps
    {
        get
        {
            if (chain != null && chain.Count > 0)
                return chain.Get(chain.Count - 1).rotSteps;
            return 0;
        }
    }

    public bool CurrentFlipped
    {
        get
        {
            if (chain != null && chain.Count > 0)
                return chain.Get(chain.Count - 1).flipped;
            return false;
        }
    }

    public void UI_CleanupCaches()
    {
        exits.Clear();
        chain.Clear();
        links.Clear();
        overlap.Clear();
        core?.ClearCore();
        Debug.Log("[GridGenerator] Caches cleared");
    }

    public List<Vector3> GetSpawnPoints()
    {
        var raw = new List<Vector3>();

        foreach (var p in _permanentSpawnPoints)
            if (!raw.Any(r => Vector3.Distance(r, p) < 0.05f))
                raw.Add(p);

        foreach (var (_, wpos) in exits.GetAvailableWorld(chain.Get))
            if (!raw.Any(r => Vector3.Distance(r, wpos) < 0.05f))
                raw.Add(wpos);

        var farEnough = new List<Vector3>();
        var reachable = new List<Vector3>();
        foreach (var p in raw)
        {
            if (runners == null || !runners.TryGetRouteLength(p, out int cells)) continue;
            reachable.Add(p);
            if (cells >= minSpawnRouteCells) farEnough.Add(p);
        }

        if (farEnough.Count > 0) return farEnough;
        if (reachable.Count > 0) return reachable;
        if (raw.Count > 0) return raw;

        var result = new List<Vector3>();
        if (chain.Count > 0)
        {
            var last = chain.Get(chain.Count - 1);
            if (last.layout?.exits != null)
            {
                foreach (var ex in last.layout.exits)
                {
                    var w = last.worldOrigin + orient.CellToWorldLocal(ex, last.layout, last.rotSteps, last.flipped);
                    if (!result.Any(r => Vector3.Distance(r, w) < 0.05f))
                        result.Add(w);
                }
            }
        }

        return result;
    }

    public Vector3 GetNextSpawnRoundRobin()
    {
        var pts = GetSpawnPoints();
        if (pts == null || pts.Count == 0)
            return Vector3.zero;

        int idx = _rrIndex % pts.Count;
        _rrIndex = (idx + 1) % pts.Count;

        return ApplySpawnHeight(pts[idx]);
    }

    public Vector3 GetFarthestSpawnPoint()
    {
        var pts = GetSpawnPoints();
        if (pts == null || pts.Count == 0)
            return Vector3.zero;

        Vector3 corePos = GetCorePosition();
        Vector3 best = pts[0];
        int bestCells = int.MinValue;
        float bestDistance = -1f;

        foreach (var p in pts)
        {
            int cells = (runners != null && runners.TryGetRouteLength(p, out int c)) ? c : -1;
            float distance = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(corePos.x, corePos.z));

            if (cells > bestCells || (cells == bestCells && distance > bestDistance))
            {
                best = p;
                bestCells = cells;
                bestDistance = distance;
            }
        }

        return ApplySpawnHeight(best);
    }

    private Vector3 ApplySpawnHeight(Vector3 p)
    {
        p.y = (core != null && core.HasCore) ? core.Position.y + runnerYOffset : p.y + runnerYOffset;
        return p;
    }

    public bool TryGetRouteExitToCore(Vector3 exitWorldPos, out List<Vector3> route)
    {
        route = null;
        return runners != null && runners.TryGetRouteExitToCore(exitWorldPos, out route);
    }

    public bool HasCore()
    {
        return core != null && core.HasCore;
    }

    public Vector3 GetCorePosition()
    {
        return core != null ? core.Position : Vector3.zero;
    }

    public float GetRunnerYOffset()
    {
        return runnerYOffset;
    }

    public bool AppendNextUsingSelectedExitWithLayout(TileLayout forced)
    {
        if (forced == null) return false;

        int global = exits.GlobalIndexFromAvailable(selectedExitIndex);
        if (global < 0 || exits.Count == 0) return false;

        var chosen = exits.Get(global);
        if (chosen.Used || chosen.Closed) return false;

        if (!TryFindPlacement(chosen, new[] { forced }, false, out var layout, out var rot, out var flip, out var origin, out var mn, out var mx))
            return false;

        PlaceTile(global, chosen, layout, rot, flip, origin, mn, mx);
        return true;
    }

    private void AttachAndApplyDupe(Transform parent, TileLayout layout)
    {
        var data = ResolveTileData(layout);
        if (data == null) return;
        var applier = parent.GetComponent<TileLevelApplier>();
        if (applier == null) applier = parent.gameObject.AddComponent<TileLevelApplier>();
        applier.Initialize(data);
        if (tileDupeSystem == null) tileDupeSystem = FindFirstObjectByType<TileDupeSystem>();
        tileDupeSystem?.AddDupe(data);
    }

    private TileDataSO ResolveTileData(TileLayout layout)
    {
        for (int i = 0; i < tileDataBindings.Count; i++)
        {
            var b = tileDataBindings[i];
            if (b != null && b.layout == layout && b.tileData != null) return b.tileData;
        }
        return defaultTileData;
    }
}
