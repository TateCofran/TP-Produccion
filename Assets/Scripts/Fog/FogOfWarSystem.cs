using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class FogOfWarSystem : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GridGenerator grid;
    [Tooltip("Fog tile prefab. Its root must cover 1x1 units on the XZ plane, have no collider and use the Fog layer. If empty, a flat quad is created at runtime.")]
    [SerializeField] private GameObject fogTilePrefab;
    [SerializeField] private Transform fogRoot;

    [Header("Grid")]
    [SerializeField, Min(1)] private int radiusInTiles = 8;
    [SerializeField] private float heightOffset = 0.3f;
    [Tooltip("Small inset so neighbour tiles that only touch at the gap do not count as overlapping.")]
    [SerializeField, Min(0f)] private float overlapEpsilon = 0.01f;

    [Header("Materials")]
    [SerializeField] private Material normalWorldMaterial;
    [SerializeField] private Material otherWorldMaterial;

    [Header("Layer")]
    [SerializeField] private string fogLayerName = "Fog";

    [Header("Dissolve")]
    [Tooltip("Seconds each fog tile takes to dissolve after a tile is placed over it.")]
    [SerializeField, Min(0f)] private float dissolveDuration = 0.4f;
    [Tooltip("Random extra delay per fog tile so the edge looks noisy.")]
    [SerializeField, Min(0f)] private float dissolveNoiseDelay = 0.08f;
    [Tooltip("Extra delay per world unit of distance from the placed tile, so the dissolve travels away from it.")]
    [SerializeField, Min(0f)] private float dissolveDelayPerUnit = 0.02f;
    [SerializeField] private float dissolveSink = 0.15f;

    public event Action<int> OnFogRevealed;
    public event Action OnGridInitialized;

    private readonly Dictionary<Vector2Int, GameObject> activeTiles = new Dictionary<Vector2Int, GameObject>();
    private readonly HashSet<Vector2Int> revealed = new HashSet<Vector2Int>();
    private readonly Queue<GameObject> pool = new Queue<GameObject>();
    private readonly List<GameObject> allTiles = new List<GameObject>();
    private readonly List<(Vector3 min, Vector3 max)> placedBounds = new List<(Vector3, Vector3)>();
    private readonly List<Vector2Int> toReveal = new List<Vector2Int>();

    private struct Dissolving
    {
        public GameObject go;
        public Vector3 center;
        public Vector3 away;
        public float start;
        public float duration;
        public bool hasProperty;
    }

    private static readonly int DissolveId = Shader.PropertyToID("_Dissolve");
    private static readonly int DissolveDirectionId = Shader.PropertyToID("_DissolveDirection");
    private readonly List<Dissolving> dissolving = new List<Dissolving>();
    private MaterialPropertyBlock dissolveBlock;

    public int DissolvingCount => dissolving.Count;

    private bool initialized;
    private Vector3 gridOrigin;
    private float step;
    private float footprint;
    private int fogLayer = -1;
    private WorldSwitchPoints essences;

    public int ActiveFogCount => activeTiles.Count;
    public bool IsInitialized => initialized;
    public float Footprint => footprint;
    public float Step => step;
    public float FogHeight => gridOrigin.y;
    public bool IsFogged(Vector2Int index) => activeTiles.ContainsKey(index);
    public IEnumerable<Vector2Int> FoggedIndices => activeTiles.Keys;

    private void Awake()
    {
        if (grid == null) grid = FindFirstObjectByType<GridGenerator>();
        if (fogRoot == null)
        {
            var root = new GameObject("FogTiles");
            root.transform.SetParent(transform, false);
            fogRoot = root.transform;
        }
        fogLayer = LayerMask.NameToLayer(fogLayerName);
        if (fogLayer < 0)
            Debug.LogWarning($"[FogOfWarSystem] Layer '{fogLayerName}' not found. Fog tiles stay on their prefab layer");
    }

    private void OnEnable()
    {
        if (grid != null) grid.OnTilePlaced += HandleTilePlaced;
        WorldState.OnWorldChanged += HandleWorldChanged;
    }

    private void OnDisable()
    {
        if (grid != null) grid.OnTilePlaced -= HandleTilePlaced;
        WorldState.OnWorldChanged -= HandleWorldChanged;
    }

    private void Start()
    {
        if (initialized || grid == null || grid.PlacedTileCount == 0) return;

        for (int i = 0; i < grid.PlacedTileCount; i++)
            HandleTilePlaced(grid.GetPlacedTile(i));
    }

    private void HandleTilePlaced(PlacedTile tile)
    {
        placedBounds.Add((tile.aabbMin, tile.aabbMax));

        if (!initialized)
            InitializeGrid(tile);

        RevealOverlapping(tile.aabbMin, tile.aabbMax);
        EnsureRadiusAround(CenterOf(tile));
    }

    private void InitializeGrid(PlacedTile first)
    {
        footprint = grid.TileFootprint;
        step = footprint + grid.TileGap;
        Vector3 center = (first.aabbMin + first.aabbMax) * 0.5f;
        gridOrigin = new Vector3(center.x, first.aabbMin.y + heightOffset, center.z);
        initialized = true;

        EnsureRadiusAround(Vector2Int.zero);
        Debug.Log($"[FogOfWarSystem] Fog grid created with {activeTiles.Count} tiles (step {step})");
        OnGridInitialized?.Invoke();
    }

    public Vector2Int CenterOf(PlacedTile tile)
    {
        Vector3 c = (tile.aabbMin + tile.aabbMax) * 0.5f;
        return WorldToIndex(c);
    }

    public Vector2Int WorldToIndex(Vector3 world)
    {
        return new Vector2Int(
            Mathf.RoundToInt((world.x - gridOrigin.x) / step),
            Mathf.RoundToInt((world.z - gridOrigin.z) / step));
    }

    public Vector3 IndexToWorld(Vector2Int index)
    {
        return new Vector3(gridOrigin.x + index.x * step, gridOrigin.y, gridOrigin.z + index.y * step);
    }

    private void EnsureRadiusAround(Vector2Int center)
    {
        for (int x = -radiusInTiles; x <= radiusInTiles; x++)
        {
            for (int z = -radiusInTiles; z <= radiusInTiles; z++)
            {
                var index = new Vector2Int(center.x + x, center.y + z);
                if (activeTiles.ContainsKey(index) || revealed.Contains(index)) continue;

                if (OverlapsAnyPlaced(index))
                {
                    revealed.Add(index);
                    continue;
                }

                SpawnFogTile(index);
            }
        }
    }

    private bool OverlapsAnyPlaced(Vector2Int index)
    {
        GetFogBounds(index, out var min, out var max);
        foreach (var (pMin, pMax) in placedBounds)
            if (Overlaps(min, max, pMin, pMax)) return true;
        return false;
    }

    public void GetFogBounds(Vector2Int index, out Vector3 min, out Vector3 max)
    {
        Vector3 c = IndexToWorld(index);
        float half = footprint * 0.5f;
        min = new Vector3(c.x - half, 0f, c.z - half);
        max = new Vector3(c.x + half, 0f, c.z + half);
    }

    public bool Overlaps(Vector3 aMin, Vector3 aMax, Vector3 bMin, Vector3 bMax)
    {
        return aMin.x + overlapEpsilon < bMax.x && aMax.x - overlapEpsilon > bMin.x
            && aMin.z + overlapEpsilon < bMax.z && aMax.z - overlapEpsilon > bMin.z;
    }

    public void RevealRadius(Vector3 center, float radius)
    {
        if (!initialized) return;
        var r = new Vector3(radius, 1000f, radius);
        RevealOverlapping(center - r, center + r);
    }

    public int CountFogOverlapping(Vector3 min, Vector3 max)
    {
        int count = 0;
        foreach (var index in activeTiles.Keys)
        {
            GetFogBounds(index, out var fMin, out var fMax);
            if (Overlaps(fMin, fMax, min, max)) count++;
        }
        return count;
    }

    private void RevealOverlapping(Vector3 min, Vector3 max)
    {
        toReveal.Clear();
        foreach (var kv in activeTiles)
        {
            GetFogBounds(kv.Key, out var fMin, out var fMax);
            if (Overlaps(fMin, fMax, min, max)) toReveal.Add(kv.Key);
        }

        Vector3 source = (min + max) * 0.5f;
        foreach (var index in toReveal)
        {
            var go = activeTiles[index];
            activeTiles.Remove(index);
            revealed.Add(index);
            StartDissolve(go, source);
        }

        if (toReveal.Count > 0)
        {
            OnFogRevealed?.Invoke(toReveal.Count);
            GrantFogEssence(toReveal.Count);
        }
    }

    private void StartDissolve(GameObject go, Vector3 source)
    {
        if (go == null) return;
        if (dissolveDuration <= 0f)
        {
            go.SetActive(false);
            pool.Enqueue(go);
            return;
        }

        Vector3 center = go.transform.position;
        Vector3 away = center - source;
        away.y = 0f;
        if (Mathf.Abs(away.x) >= Mathf.Abs(away.z)) away = new Vector3(Mathf.Sign(away.x == 0f ? 1f : away.x), 0f, 0f);
        else away = new Vector3(0f, 0f, Mathf.Sign(away.z));

        float distance = Vector3.Distance(new Vector3(center.x, 0f, center.z), new Vector3(source.x, 0f, source.z));
        float delay = UnityEngine.Random.Range(0f, dissolveNoiseDelay) + distance * dissolveDelayPerUnit;

        bool hasProperty = false;
        foreach (var r in go.GetComponentsInChildren<Renderer>())
            if (r.sharedMaterial != null && r.sharedMaterial.HasProperty(DissolveId)) { hasProperty = true; break; }

        dissolving.Add(new Dissolving
        {
            go = go,
            center = center,
            away = away,
            start = Time.time + delay,
            duration = dissolveDuration,
            hasProperty = hasProperty
        });
    }

    private void Update()
    {
        if (dissolving.Count == 0) return;
        float now = Time.time;

        for (int i = dissolving.Count - 1; i >= 0; i--)
        {
            var d = dissolving[i];
            if (d.go == null)
            {
                dissolving.RemoveAt(i);
                continue;
            }

            float k = Mathf.Clamp01((now - d.start) / d.duration);
            if (k <= 0f) continue;

            if (d.hasProperty) ApplyDissolveProperty(d, k);
            else ApplyDissolveTransform(d, k);

            if (k < 1f) continue;

            d.go.SetActive(false);
            d.go.transform.position = d.center;
            d.go.transform.localScale = new Vector3(footprint, 1f, footprint);
            if (d.hasProperty) ApplyDissolveProperty(d, 0f);
            pool.Enqueue(d.go);
            dissolving.RemoveAt(i);
        }
    }

    private void ApplyDissolveProperty(Dissolving d, float k)
    {
        dissolveBlock ??= new MaterialPropertyBlock();
        foreach (var r in d.go.GetComponentsInChildren<Renderer>())
        {
            r.GetPropertyBlock(dissolveBlock);
            dissolveBlock.SetFloat(DissolveId, k);
            dissolveBlock.SetVector(DissolveDirectionId, d.away);
            r.SetPropertyBlock(dissolveBlock);
        }
    }

    private void ApplyDissolveTransform(Dissolving d, float k)
    {
        float eased = k * k * (3f - 2f * k);
        float remaining = Mathf.Max(0.001f, 1f - eased);
        bool alongX = Mathf.Abs(d.away.x) > 0.5f;
        var scale = new Vector3(alongX ? footprint * remaining : footprint, 1f, alongX ? footprint : footprint * remaining);
        d.go.transform.localScale = scale;
        Vector3 shift = d.away * (footprint * 0.5f * (1f - remaining));
        d.go.transform.position = d.center + shift + Vector3.down * dissolveSink * eased;
    }

    private void GrantFogEssence(int count)
    {
        float perTile = GameModifiersManager.Stat(ModifierStat.FogRevealEssence);
        if (perTile <= 0f) return;

        if (essences == null) essences = FindFirstObjectByType<WorldSwitchPoints>();
        if (essences == null) return;

        int amount = Mathf.RoundToInt(perTile * count);
        if (amount <= 0) return;

        essences.AddBlueEssence(amount);
        Log.Info("FogOfWarSystem", $"Granted {amount} blue essences for {count} revealed fog tiles");
    }

    private void SpawnFogTile(Vector2Int index)
    {
        GameObject go = pool.Count > 0 ? pool.Dequeue() : CreateFogTile();
        go.transform.SetParent(fogRoot, false);
        go.transform.position = IndexToWorld(index);
        go.transform.rotation = Quaternion.identity;
        go.transform.localScale = new Vector3(footprint, 1f, footprint);
        go.SetActive(true);
        activeTiles[index] = go;
    }

    private GameObject CreateFogTile()
    {
        GameObject go;
        if (fogTilePrefab != null)
        {
            go = Instantiate(fogTilePrefab, fogRoot);
        }
        else
        {
            go = new GameObject("FogTile");
            go.transform.SetParent(fogRoot, false);
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "FogQuad";
            quad.transform.SetParent(go.transform, false);
            quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        }

        foreach (var c in go.GetComponentsInChildren<Collider>(true))
        {
            c.enabled = false;
            Destroy(c);
        }

        if (fogLayer >= 0)
            foreach (var t in go.GetComponentsInChildren<Transform>(true))
                t.gameObject.layer = fogLayer;

        ApplyMaterial(go, CurrentMaterial());
        allTiles.Add(go);
        return go;
    }

    private Material CurrentMaterial()
    {
        return WorldState.IsOther && otherWorldMaterial != null ? otherWorldMaterial : normalWorldMaterial;
    }

    private void HandleWorldChanged(ShiftingWorldMechanic.World world)
    {
        var mat = CurrentMaterial();
        if (mat == null) return;
        foreach (var go in allTiles)
            if (go != null) ApplyMaterial(go, mat);
    }

    private static void ApplyMaterial(GameObject go, Material mat)
    {
        if (mat == null) return;
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            r.sharedMaterial = mat;
    }
}
