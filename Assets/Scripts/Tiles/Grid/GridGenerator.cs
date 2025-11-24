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

    [Header("Parent / Salida")][SerializeField] private Transform tilesRoot; [SerializeField] private string rootName = "TileChainRoot"; [SerializeField] private string tileGroupBaseName = "Tile_";

    [Header("Cadena")][SerializeField] private bool clearOnStartGenerate = true; [SerializeField] private int connectionTries = 24;

    [Header("Offsets por celda")][SerializeField] private Vector3 grassCellOffset = Vector3.zero; [SerializeField] private Vector3 pathCellOffset = Vector3.zero;

    [Header("Offset entre tiles")][SerializeField] private float tileGap = 0.25f; [SerializeField] private Vector3 extraTileOffset = Vector3.zero;

    [Header("Orientaciones")][SerializeField] private bool allowRotations = true; [SerializeField] private bool allowFlip = true; [SerializeField] private bool worldPlusZUsesYZeroEdge = true;

    [Header("UI / Exits")][SerializeField] private int selectedExitIndex = 0;

    [Header("Spawn/Runner")][SerializeField] private float minDistanceToCore = 0f; [SerializeField] private bool excludeCoreNeighbors = true;
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
    private ISpawnPointsService spawns;
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

        float baseCellSize = (initialLayout != null) ? initialLayout.cellSize : 1f;

        spawns = new SpawnPointsService(
            exits, chain, core, orient,
            baseCellSize,
            minDistanceToCore,
            excludeCoreNeighbors
        );

        runners = new RunnerService(
            exits, chain, core, orient,
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
        if (initialLayout == null) { Debug.LogError("[GridGenerator] Asigná initialLayout."); return; }

        if (clearOnStartGenerate)
        {
            ClearRootChildren();
            exits.Clear();
            chain.Clear();
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
            // Si querés escalonar entre tiles, podés pasar un startDelay distinto:
            float startDelay = placementSequencer.GetNextTileStartDelay(chain.Count); // 0 para el primero
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

        int abiertos = exits.IndicesDisponibles().Count();
        //Debug.Log($"[GridGenerator] Primer tile instanciado ({count} celdas). Exits disponibles: {abiertos}");
        // Re-scan de exits por si el primer tile ya bloquea alguna (raro pero por las dudas)
        UpdatePermanentSpawnPointsForAllExits();

    }

    public void AppendNextUsingSelectedExit()
    {
        int global = exits.GlobalIndexFromAvailable(selectedExitIndex);
        if (global < 0) { Debug.LogWarning("[GridGenerator] No hay EXITS disponibles."); return; }
        var chosen = exits.Get(global);
        if (TryAppendAtExit(chosen)) { exits.MarkUsed(global); exits.Relabel(); if (autoSpawnRunnersOnAppend) runners.SpawnRunnersAtAllOpenExits(); }
        else if (!HasAnyValidPlacementForExit(chosen)) { exits.MarkClosed(global); exits.Relabel(); selectedExitIndex = 0; }
    }

    public IEnumerator AppendNextAsync()
    {
        int global = exits.GlobalIndexFromAvailable(selectedExitIndex);
        if (global < 0 || exits.Count == 0) yield break;
        var chosen = exits.Get(global); if (chosen.Used) yield break;
        bool success = false;
        for (int i = 0; i < connectionTries; i++) { if (TryAppendAtExit(chosen)) { success = true; break; } yield return null; }
        if (success) { exits.MarkUsed(global); exits.Relabel(); if (autoSpawnRunnersOnAppend) runners.SpawnRunnersAtAllOpenExits(); }
        else if (!HasAnyValidPlacementForExit(chosen)) { exits.MarkClosed(global); exits.Relabel(); selectedExitIndex = 0; }
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
            previews.Add(new PlacementPreview { origin = newOrigin, sizeXZ = new Vector2(o.w * candidate.cellSize, o.h * candidate.cellSize), valid = noOverlap, status = status, note = $"{candidate.name} | {rot * 90}° | flip={(flip ? 1 : 0)}", cellSize = candidate.cellSize });
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
        return exits.IndicesDisponibles().Select(i => exits.Get(i).label).ToList();
    }

    public void UI_SetExitIndex(int index) => selectedExitIndex = Mathf.Max(0, index);
    public void UI_SetExitByLabel(string label) { int idx = exits.AvailableIndexByLabel(label); if (idx >= 0) selectedExitIndex = idx; }
    public void UI_GenerateFirst() => GenerateFirst();
    public void UI_AppendNext() => AppendNextUsingSelectedExit();
    public void UI_AppendNextAsync() => StartCoroutine(AppendNextAsync());

    private bool TryAppendAtExit(ExitRecord chosen)
    {
        var prevTile = chain.Get(chosen.tileIndex); var layoutPrev = prevTile.layout; var exitCell = chosen.cell;
        if (!layoutPrev.IsInside(exitCell) || !layoutPrev.IsPath(exitCell)) return false;
        var nb = FindSinglePathNeighbor(layoutPrev, exitCell); if (!nb.HasValue) return false;

        Vector2Int dirOut = ClampToOrtho(orient.ApplyToDir(exitCell - nb.Value, prevTile.rotSteps, prevTile.flipped));
        Vector3 prevExitWorld = prevTile.worldOrigin + orient.CellToWorldLocal(exitCell, layoutPrev, prevTile.rotSteps, prevTile.flipped);

        var shuffled = candidates.GetValidCandidates(candidateLayouts, dirOut, allowRotations, allowFlip, orient).OrderBy(_ => rng.Next());
        int attempts = 0;
        foreach (var (candidate, rot, flip) in shuffled)
        {
            var o = orient.GetOrientedData(candidate, rot, flip);
            var newOrigin = placement.ComputeNewOrigin(candidate, rot, o.entryOriented, dirOut, prevTile.aabbMin, prevTile.aabbMax, prevExitWorld);
            placement.ComputeAABB(newOrigin, o.w, o.h, candidate.cellSize, out var nMin, out var nMax);
            if (overlap.OverlapsAny(nMin, nMax)) { attempts++; if (attempts >= connectionTries) break; continue; }

            var parent = CreateChild(tilesRoot, $"{tileGroupBaseName}{chain.Count}");
            int count = instantiator.InstantiateLayout(candidate, newOrigin, rot, flip, parent);

            if (placementSequencer != null)
            {
                float startDelay = placementSequencer.GetNextTileStartDelay(chain.Count);
                placementSequencer.PlayForGroup(parent, startDelay);
            }

            var placed = new PlacedTile { layout = candidate, worldOrigin = newOrigin, rotSteps = rot, flipped = flip, parent = parent, aabbMin = nMin, aabbMax = nMax };
            chain.Add(placed); overlap.Add(placed, chain.Count - 1);
            AddTileExitsToPool(chain.Count - 1, candidate.entry);
            AttachAndApplyDupe(parent, candidate);
    
            UpdatePermanentSpawnPointsForAllExits();
            Debug.Log($"[GridGenerator] Conectado via EXIT {chosen.label} → Nuevo tile #{chain.Count - 1} (rot={rot * 90}°, flip={flip}). Instancias: {count}");

            TutorialEventHub.RaiseTilePlaced();
            return true;
        }
        return false;
    }

    private bool HasAnyValidPlacementForExit(ExitRecord chosen)
    {
        var prevTile = chain.Get(chosen.tileIndex); var layoutPrev = prevTile.layout; var exitCell = chosen.cell;
        if (!layoutPrev.IsInside(exitCell) || !layoutPrev.IsPath(exitCell)) return false;
        var nb = FindSinglePathNeighbor(layoutPrev, exitCell); if (!nb.HasValue) return false;
        Vector2Int dirOut = ClampToOrtho(orient.ApplyToDir(exitCell - nb.Value, prevTile.rotSteps, prevTile.flipped));
        Vector3 prevExitWorld = prevTile.worldOrigin + orient.CellToWorldLocal(exitCell, layoutPrev, prevTile.rotSteps, prevTile.flipped);

        foreach (var (candidate, rot, flip) in candidates.GetValidCandidates(candidateLayouts, dirOut, allowRotations, allowFlip, orient))
        {
            var o = orient.GetOrientedData(candidate, rot, flip);
            var newOrigin = placement.ComputeNewOrigin(candidate, rot, o.entryOriented, dirOut, prevTile.aabbMin, prevTile.aabbMax, prevExitWorld);
            placement.ComputeAABB(newOrigin, o.w, o.h, candidate.cellSize, out var nMin, out var nMax);
            if (!overlap.OverlapsAny(nMin, nMax)) return true;
        }
        return false;
    }

    private void AddTileExitsToPool(int tileIndex, Vector2Int? excludeAssetCell)
    {
        var pt = chain.Get(tileIndex);
        var exitsList = pt.layout.exits ?? new List<Vector2Int>();

        foreach (var ex in exitsList)
        {
            if (!pt.layout.IsInside(ex) || !pt.layout.IsPath(ex)) continue;
            if (excludeAssetCell.HasValue && ex == excludeAssetCell.Value) continue;
            if (IsExitBlockedByAnotherTile(tileIndex, ex, out var dirOut))
            {
                Vector3 exitWorld = pt.worldOrigin + orient.CellToWorldLocal(ex, pt.layout, pt.rotSteps, pt.flipped);

                if (!_permanentSpawnPoints.Any(p => Vector3.Distance(p, exitWorld) < 0.05f))
                {
                    _permanentSpawnPoints.Add(exitWorld);

                    // Queremos que el portal mire EN DIRECCIÓN DEL CAMINO (hacia adentro del mapa).
                    // dirOut apunta "hacia afuera", así que usamos -dirOut como forward.
                    Vector3 forward = new Vector3(-dirOut.x, 0f, -dirOut.y);
                    CreatePermanentSpawnVisual(exitWorld, forward);
                }

                continue; // no agregar como exit disponible
            }



            // Caso normal: sí es exit disponible
            exits.Add(new ExitRecord
            {
                tileIndex = tileIndex,
                cell = ex,
                flags = 0,
                label = string.Empty
            });
        }

        exits.Relabel();
    }


    private static bool PointInAABB(Vector3 p, Vector3 min, Vector3 max, float eps = 0.0001f)
    {
        return p.x >= (min.x - eps) && p.x <= (max.x + eps) &&
               p.z >= (min.z - eps) && p.z <= (max.z + eps);
    }

    /// <summary>
    /// Devuelve true si inmediatamente afuera de la exit hay otra tile ocupando la siguiente celda.
    /// </summary>
    private bool IsExitBlockedByAnotherTile(int tileIndex, Vector2Int exitCell, out Vector2Int dirOut)
    {
        dirOut = Vector2Int.zero;

        var pt = chain.Get(tileIndex);
        var layout = pt.layout;

        // La exit debe tener 1 vecino path "hacia adentro"
        var nb = FindSinglePathNeighbor(layout, exitCell);
        if (!nb.HasValue) return false;

        // Dirección de salida en mundo (cardinal)
        dirOut = ClampToOrtho(orient.ApplyToDir(exitCell - nb.Value, pt.rotSteps, pt.flipped));

        // Centro de la celda de la EXIT
        Vector3 exitWorld = pt.worldOrigin + orient.CellToWorldLocal(exitCell, layout, pt.rotSteps, pt.flipped);

        // Centro de la siguiente celda fuera del tile
        float cs = layout.cellSize;
        Vector3 nextCellWorld = exitWorld + new Vector3(dirOut.x, 0f, dirOut.y) * cs;

        // ¿Esa próxima celda cae dentro del AABB de alguna otra tile?
        for (int i = 0; i < chain.Count; i++)
        {
            if (i == tileIndex) continue;
            var other = chain.Get(i);
            if (PointInAABB(nextCellWorld, other.aabbMin, other.aabbMax))
                return true;
        }
        return false;
    }



    private Transform EnsurePermanentSpawnRoot()
    {
        if (permanentSpawnRoot) return permanentSpawnRoot;
        var rootGO = GameObject.Find("PermanentSpawnRoot") ?? new GameObject("PermanentSpawnRoot");
        permanentSpawnRoot = rootGO.transform;
        return permanentSpawnRoot;
    }

    /// <summary>
    /// Revisa TODAS las exits abiertas. Si ahora alguna quedó bloqueada por otro tile,
    /// la convierte en spawn permanente y marca la exit como Cerrada.
    /// </summary>
    private void UpdatePermanentSpawnPointsForAllExits()
    {
        if (exits == null || chain == null) return;

        for (int i = 0; i < exits.Count; i++)
        {
            var rec = exits.Get(i);

            // Solo nos importan las que todavía están abiertas
            if (rec.Used || rec.Closed)
                continue;

            // Ahora necesitamos también la dirección de salida
            if (IsExitBlockedByAnotherTile(rec.tileIndex, rec.cell, out var dirOut))
            {
                var pt = chain.Get(rec.tileIndex);
                Vector3 exitWorld = pt.worldOrigin +
                                    orient.CellToWorldLocal(rec.cell, pt.layout, pt.rotSteps, pt.flipped);

                // Evitar duplicados de spawn
                if (!_permanentSpawnPoints.Any(p => Vector3.Distance(p, exitWorld) < 0.05f))
                {
                    _permanentSpawnPoints.Add(exitWorld);

                    // Queremos que el portal mire HACIA ADENTRO del mapa:
                    // dirOut apunta hacia afuera, así que usamos -dirOut.
                    Vector3 forward = new Vector3(-dirOut.x, 0f, -dirOut.y);
                    CreatePermanentSpawnVisual(exitWorld, forward);
                }

                // Cerramos la exit para que no vuelva a aparecer en la UI
                exits.MarkClosed(i, "SPAWN_PERMANENTE");
            }
        }
    }



    private void CreatePermanentSpawnVisual(Vector3 worldPos, Vector3 forwardDir)
    {
        if (!permanentSpawnPrefab) return;

        // Evita duplicados por posición
        foreach (var existing in _spawnPrefabsInScene)
        {
            if (existing && Vector3.Distance(existing.transform.position, worldPos) < 0.05f)
                return;
        }

        var parent = EnsurePermanentSpawnRoot();
        var pos = worldPos + Vector3.up * permanentSpawnYOffset;

        // Normalizamos la dirección por las dudas
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

        chain.Clear(); exits.Clear(); overlap.Clear(); core.ClearCore();

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
        if (!ok) Debug.LogError("[GridGenerator] Asigná todos los prefabs de path y grass.");
        return ok;
    }

    private static Transform EnsureRoot(Transform root, string rootName)
    {
        if (root) return root; var found = GameObject.Find(rootName); if (!found) found = new GameObject(rootName); return found.transform;
    }

    private static Transform CreateChild(Transform root, string name)
    { var go = new GameObject(name); go.transform.SetParent(root, false); return go.transform; }

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
            if (c != null) pool.Add(c);

        for (int i = 0; i < pool.Count; i++)
        {
            int j = rng.Next(i, pool.Count);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        if (pool.Count > count)
            pool.RemoveRange(count, pool.Count - count);

        return pool;
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
        overlap.Clear();
        core?.ClearCore();
        Debug.Log("[GridGenerator] Caches limpiados.");
    }

    public List<Vector3> GetSpawnPoints()
    {
        var result = new List<Vector3>();

        // 1) permanentes
        foreach (var p in _permanentSpawnPoints)
            if (!result.Any(r => Vector3.Distance(r, p) < 0.05f))
                result.Add(p);

        // 2) exits abiertos
        foreach (var (_, wpos) in exits.GetAvailableWorld(chain.Get))
            if (!result.Any(r => Vector3.Distance(r, wpos) < 0.05f))
                result.Add(wpos);

        // 3) filtros opcionales (distancia a core, vecinos) — si los tenías, aplicalos acá

        // 4) fallback opcional si no hay nada
        if (result.Count == 0 && chain.Count > 0)
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

        // round-robin local
        int idx = _rrIndex % pts.Count;
        _rrIndex = (idx + 1) % pts.Count;

        // aseguramos altura cómoda para el spawn
        var p = pts[idx];
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

        var prevTile = chain.Get(chosen.tileIndex);
        var layoutPrev = prevTile.layout;
        var exitCell = chosen.cell;

        if (!layoutPrev.IsInside(exitCell) || !layoutPrev.IsPath(exitCell)) return false;

        var nb = FindSinglePathNeighbor(layoutPrev, exitCell);
        if (!nb.HasValue) return false;

        Vector2Int dirOut = ClampToOrtho(orient.ApplyToDir(exitCell - nb.Value, prevTile.rotSteps, prevTile.flipped));
        Vector3 prevExitWorld = prevTile.worldOrigin +
                                orient.CellToWorldLocal(exitCell, layoutPrev, prevTile.rotSteps, prevTile.flipped);

        int rotMax = allowRotations ? 4 : 1;
        int flipMax = allowFlip ? 2 : 1;

        for (int rot = 0; rot < rotMax; rot++)
        {
            for (int f = 0; f < flipMax; f++)
            {
                bool flip = (allowFlip && f == 1);

                var o = orient.GetOrientedData(forced, rot, flip);

                bool okBorde = false;
                if (dirOut == Vector2Int.right) okBorde = (o.entryOriented.x == 0);
                else if (dirOut == Vector2Int.left) okBorde = (o.entryOriented.x == o.w - 1);
                else if (dirOut == Vector2Int.up) okBorde = (o.entryOriented.y == (worldPlusZUsesYZeroEdge ? 0 : o.h - 1));
                else okBorde = (o.entryOriented.y == (worldPlusZUsesYZeroEdge ? o.h - 1 : 0));

                if (!okBorde) continue;

                var newOrigin = placement.ComputeNewOrigin(
                                    forced, rot, o.entryOriented, dirOut,
                                    prevTile.aabbMin, prevTile.aabbMax, prevExitWorld);

                placement.ComputeAABB(newOrigin, o.w, o.h, forced.cellSize, out var nMin, out var nMax);
                if (overlap.OverlapsAny(nMin, nMax)) continue;

                var parent = CreateTileParent(tilesRoot, $"{tileGroupBaseName}{chain.Count}");
                int count = instantiator.InstantiateLayout(forced, newOrigin, rot, flip, parent);

                if (placementSequencer != null)
                {
                    float startDelay = placementSequencer.GetNextTileStartDelay(chain.Count);
                    placementSequencer.PlayForGroup(parent, startDelay);
                }

                var placed = new PlacedTile
                {
                    layout = forced,
                    worldOrigin = newOrigin,
                    rotSteps = rot,
                    flipped = flip,
                    parent = parent,
                    aabbMin = nMin,
                    aabbMax = nMax
                };

                chain.Add(placed);
                overlap.Add(placed, chain.Count - 1);

                AddTileExitsToPool(chain.Count - 1, forced.entry);

                exits.MarkUsed(global);
                exits.Relabel();

                AttachAndApplyDupe(parent, forced);
                // Actualizar spawns permanentes con todas las tiles ya colocadas
                UpdatePermanentSpawnPointsForAllExits();


                TutorialEventHub.RaiseTilePlaced();
                return true;
            }
        }

        if (!HasAnyValidPlacementForExit(chosen))
        {
            exits.MarkClosed(global);
            exits.Relabel();
            selectedExitIndex = 0;
        }

        return false;
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
