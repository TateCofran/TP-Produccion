using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class TileGhostPreview : MonoBehaviour
{
    public static TileGhostPreview Instance { get; private set; }

    private class GhostEntry
    {
        public Transform root;
        public Transform cells;
        public List<GhostLayoutInstantiator.GhostCell> cellData;
    }

    [Header("Refs")]
    [SerializeField] private GridGenerator grid;
    [SerializeField] private RewardZoneSystem rewardZones;
    [SerializeField] private FogOfWarSystem fog;
    [SerializeField] private Camera cam;

    [Header("Look")]
    [SerializeField] private Material ghostMaterial;
    [SerializeField] private TileIconPalette palette;
    [SerializeField] private string ghostLayerName = GhostVisuals.DefaultLayerName;
    [SerializeField, Range(0f, 1f)] private float targetAlpha = 0.6f;
    [SerializeField, Min(0f)] private float yLift = 0.02f;

    [Header("Enter Animation (unscaled)")]
    [SerializeField, Min(0.01f)] private float enterDuration = 0.12f;
    [SerializeField, Range(0.1f, 1f)] private float enterStartScale = 0.9f;

    [Header("Labels")]
    [SerializeField, Min(0f)] private float labelHeight = 2.5f;
    [SerializeField, Min(0.1f)] private float labelFontSize = 6f;
    [SerializeField] private Color rewardLabelColor = new Color(1f, 0.9f, 0.3f, 1f);
    [SerializeField] private Color fogLabelColor = new Color(0.6f, 0.85f, 1f, 1f);
    [SerializeField, Min(0.1f)] private float rewardIconSize = 1.2f;

    private readonly Dictionary<(TileLayout, int, bool), GhostEntry> cache = new();
    private GhostEntry current;
    private RewardZone highlightedZone;
    private Coroutine enterRoutine;
    private GhostLayoutInstantiator instantiator;
    private int ghostLayer;

    private Transform labelRoot;
    private TextMeshPro rewardLabel;
    private TextMeshPro fogLabel;
    private SpriteRenderer rewardIcon;

    public bool IsShowing => current != null && current.root != null && current.root.gameObject.activeSelf;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        if (grid == null) grid = FindFirstObjectByType<GridGenerator>();
        if (rewardZones == null) rewardZones = FindFirstObjectByType<RewardZoneSystem>();
        if (fog == null) fog = FindFirstObjectByType<FogOfWarSystem>();
        ghostLayer = GhostVisuals.ResolveLayer(ghostLayerName);
        BuildLabels();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void LateUpdate()
    {
        if (labelRoot == null || !labelRoot.gameObject.activeSelf) return;
        var c = cam ? cam : Camera.main;
        if (c != null) labelRoot.rotation = Quaternion.LookRotation(labelRoot.position - c.transform.position);
    }

    public void Show(TilePlacementInfo info)
    {
        if (info.layout == null) return;

        var entry = GetOrBuild(info);
        if (entry == null) return;

        if (current != null && current != entry) current.root.gameObject.SetActive(false);
        current = entry;

        Vector3 center = (info.aabbMin + info.aabbMax) * 0.5f;
        center.y = info.origin.y + yLift;
        entry.root.position = center;
        entry.cells.localPosition = info.origin - new Vector3(center.x, info.origin.y, center.z);
        entry.root.gameObject.SetActive(true);

        UpdateExtras(info, center);

        if (enterRoutine != null) StopCoroutine(enterRoutine);
        enterRoutine = StartCoroutine(EnterRoutine(entry));
    }

    public void Hide()
    {
        if (enterRoutine != null)
        {
            StopCoroutine(enterRoutine);
            enterRoutine = null;
        }

        if (current != null && current.root != null) current.root.gameObject.SetActive(false);
        current = null;

        SetHighlightedZone(null);
        if (labelRoot != null) labelRoot.gameObject.SetActive(false);
    }

    public void ClearCache()
    {
        Hide();
        foreach (var entry in cache.Values)
            if (entry.root != null) Destroy(entry.root.gameObject);
        cache.Clear();
    }

    private GhostEntry GetOrBuild(TilePlacementInfo info)
    {
        var key = (info.layout, info.rotSteps, info.flip);
        if (cache.TryGetValue(key, out var existing) && existing.root != null) return existing;

        if (instantiator == null && grid != null) instantiator = grid.CreateGhostInstantiator();
        if (instantiator == null)
        {
            Debug.LogWarning("[TileGhostPreview] GridGenerator is not ready. The ghost cannot be built");
            return null;
        }

        var root = new GameObject($"TileGhost_{info.layout.name}_r{info.rotSteps}{(info.flip ? "_f" : "")}").transform;
        root.SetParent(transform, false);
        var cells = new GameObject("Cells").transform;
        cells.SetParent(root, false);

        instantiator.InstantiateLayout(info.layout, Vector3.zero, info.rotSteps, info.flip, cells);

        var material = GhostVisuals.ResolveMaterial(ghostMaterial);
        GhostVisuals.ApplyGhost(root.gameObject, material, ghostLayer);

        var entry = new GhostEntry
        {
            root = root,
            cells = cells,
            cellData = new List<GhostLayoutInstantiator.GhostCell>(instantiator.LastCells)
        };
        ApplyAlpha(entry, 0f);

        root.gameObject.SetActive(false);
        cache[key] = entry;
        return entry;
    }

    private IEnumerator EnterRoutine(GhostEntry entry)
    {
        float t = 0f;
        while (t < enterDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / enterDuration);
            float eased = 1f - (1f - k) * (1f - k);
            entry.root.localScale = Vector3.one * Mathf.Lerp(enterStartScale, 1f, eased);
            ApplyAlpha(entry, targetAlpha * eased);
            yield return null;
        }

        entry.root.localScale = Vector3.one;
        ApplyAlpha(entry, targetAlpha);
        enterRoutine = null;
    }

    private void ApplyAlpha(GhostEntry entry, float alpha)
    {
        var pal = palette != null ? palette : TileIconGenerator.Palette;
        foreach (var cell in entry.cellData)
        {
            var tint = pal.GetGhostTint(cell.isPath, cell.mods);
            foreach (var r in cell.renderers)
                GhostVisuals.SetTint(r, tint, alpha);
        }
    }

    private void UpdateExtras(TilePlacementInfo info, Vector3 center)
    {
        RewardZone zone = rewardZones != null ? rewardZones.FindZoneOverlapping(info.aabbMin, info.aabbMax) : null;
        SetHighlightedZone(zone);

        int fogCount = 0;
        if (fog != null && fog.IsInitialized && GameModifiersManager.Stat(ModifierStat.FogRevealEssence) > 0f)
            fogCount = fog.CountFogOverlapping(info.aabbMin, info.aabbMax);

        bool showReward = zone != null;
        bool showFog = fogCount > 0;

        if (labelRoot == null) return;
        labelRoot.gameObject.SetActive(showReward || showFog);
        if (!showReward && !showFog) return;

        labelRoot.position = center + Vector3.up * labelHeight;

        rewardLabel.gameObject.SetActive(showReward);
        rewardIcon.gameObject.SetActive(showReward && rewardIcon.sprite != null);
        if (showReward)
        {
            rewardIcon.sprite = rewardZones.GetIcon(zone.Reward);
            rewardIcon.gameObject.SetActive(rewardIcon.sprite != null);
            rewardLabel.text = "Reward!";
        }

        fogLabel.gameObject.SetActive(showFog);
        if (showFog)
        {
            float perTile = GameModifiersManager.Stat(ModifierStat.FogRevealEssence);
            fogLabel.text = $"+{Mathf.RoundToInt(fogCount * perTile)}";
        }
    }

    private void SetHighlightedZone(RewardZone zone)
    {
        if (highlightedZone == zone) return;
        if (highlightedZone != null) highlightedZone.SetHighlighted(false);
        highlightedZone = zone;
        if (highlightedZone != null) highlightedZone.SetHighlighted(true);
    }

    private void BuildLabels()
    {
        labelRoot = new GameObject("TileGhostLabels").transform;
        labelRoot.SetParent(transform, false);

        rewardIcon = new GameObject("RewardIcon").AddComponent<SpriteRenderer>();
        rewardIcon.transform.SetParent(labelRoot, false);
        rewardIcon.transform.localPosition = new Vector3(0f, 1.2f, 0f);
        rewardIcon.transform.localScale = Vector3.one * rewardIconSize;

        rewardLabel = CreateLabel("RewardLabel", new Vector3(0f, 0f, 0f), rewardLabelColor);
        fogLabel = CreateLabel("FogLabel", new Vector3(0f, -0.9f, 0f), fogLabelColor);

        GhostVisuals.SetLayerRecursive(labelRoot, ghostLayer);
        labelRoot.gameObject.SetActive(false);
    }

    private TextMeshPro CreateLabel(string name, Vector3 localPos, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(labelRoot, false);
        go.transform.localPosition = localPos;
        var tmp = go.AddComponent<TextMeshPro>();
        tmp.fontSize = labelFontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.fontStyle = FontStyles.Bold;
        tmp.rectTransform.sizeDelta = new Vector2(8f, 1.5f);
        return tmp;
    }
}
