using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class RewardZoneSystem : MonoBehaviour
{
    [System.Serializable]
    public class RewardIcon
    {
        public RewardType type;
        public Sprite sprite;
    }

    [Header("Refs")]
    [SerializeField] private GridGenerator grid;
    [SerializeField] private FogOfWarSystem fog;
    [SerializeField] private CameraController cameraController;
    [SerializeField] private RewardTableSO rewardTable;
    [Tooltip("Prefab with a RewardZone component. If empty, a zone is created at runtime.")]
    [SerializeField] private RewardZone zonePrefab;
    [SerializeField] private Transform zoneRoot;

    [Header("Spawn")]
    [SerializeField, Min(0)] private int initialZones = 3;
    [SerializeField, Min(0)] private int minDistanceTiles = 2;
    [SerializeField, Min(1)] private int maxDistanceTiles = 6;
    [SerializeField, Min(1)] private int respawnEveryNWaves = 5;
    [SerializeField, Min(1)] private int maxActiveZones = 3;
    [Tooltip("Height of the border above the fog tiles.")]
    [SerializeField] private float heightAboveFog = 0.15f;

    [Header("Visibility")]
    [SerializeField, Range(0f, 1f)] private float visibleZoomThreshold = 0.6f;

    [Header("Feedback")]
    [SerializeField] private List<RewardIcon> icons = new List<RewardIcon>();
    [SerializeField] private GameObject claimVfxPrefab;
    [SerializeField, Min(0f)] private float claimVfxLifetime = 2f;
    [SerializeField] private AudioSource claimAudioSource;
    [SerializeField] private AudioClip claimClip;
    [SerializeField] private TMP_Text toastText;
    [SerializeField, Min(0f)] private float toastSeconds = 2.5f;

    [System.Serializable]
    public class RewardClip
    {
        public RewardType type;
        public AudioClip clip;
    }

    [Header("Claim Juice")]
    [Tooltip("Particle burst tinted with the reward color. Pooled with VfxPool.")]
    [SerializeField] private GameObject claimBurstPrefab;
    [SerializeField, Min(0.1f)] private float claimBurstLifetime = 1.5f;
    [SerializeField, Min(0f)] private float beamHeight = 8f;
    [SerializeField, Min(0.01f)] private float beamWidth = 0.9f;
    [SerializeField, Min(0.01f)] private float beamDuration = 0.6f;
    [Tooltip("Optional clip per reward type. A generated tone is used when missing.")]
    [SerializeField] private List<RewardClip> claimClips = new List<RewardClip>();
    [SerializeField, Range(0f, 1f)] private float claimVolume = 0.6f;
    [SerializeField] private Canvas toastCanvas;

    private AudioClip generatedClaimTone;

    [Header("Fallback Amounts")]
    [SerializeField, Min(0)] private int fallbackEssenceAmount = 25;

    private readonly List<RewardZone> activeZones = new List<RewardZone>();
    private readonly Queue<RewardZone> pool = new Queue<RewardZone>();
    private readonly List<RewardZone> claimed = new List<RewardZone>();

    private WaveManager subscribedWaves;
    private WorldSwitchPoints essences;
    private Coroutine toastRoutine;
    private float currentZoom;

    public IReadOnlyList<RewardZone> ActiveZones => activeZones;

    private void Awake()
    {
        if (grid == null) grid = FindFirstObjectByType<GridGenerator>();
        if (fog == null) fog = FindFirstObjectByType<FogOfWarSystem>();
        if (cameraController == null) cameraController = FindFirstObjectByType<CameraController>();
        if (zoneRoot == null)
        {
            var root = new GameObject("RewardZones");
            root.transform.SetParent(transform, false);
            zoneRoot = root.transform;
        }
        if (toastText != null) toastText.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (grid != null) grid.OnTilePlaced += HandleTilePlaced;
        if (fog != null) fog.OnGridInitialized += HandleFogInitialized;
        if (cameraController != null) cameraController.OnZoomChanged += HandleZoomChanged;
        TrySubscribeWaves();
    }

    private void OnDisable()
    {
        if (grid != null) grid.OnTilePlaced -= HandleTilePlaced;
        if (fog != null) fog.OnGridInitialized -= HandleFogInitialized;
        if (cameraController != null) cameraController.OnZoomChanged -= HandleZoomChanged;
        if (subscribedWaves != null)
        {
            subscribedWaves.OnWaveEnded -= HandleWaveEnded;
            subscribedWaves = null;
        }
    }

    private void Start()
    {
        TrySubscribeWaves();
        if (cameraController != null) currentZoom = cameraController.ZoomNormalized;
        if (fog != null && fog.IsInitialized && activeZones.Count == 0) HandleFogInitialized();
    }

    private void Update()
    {
        RefreshVisibility();
    }

    private void TrySubscribeWaves()
    {
        if (subscribedWaves != null || WaveManager.Instance == null) return;
        subscribedWaves = WaveManager.Instance;
        subscribedWaves.OnWaveEnded += HandleWaveEnded;
    }

    private void HandleFogInitialized()
    {
        for (int i = 0; i < initialZones; i++)
            if (!TrySpawnZone(minDistanceTiles, maxDistanceTiles)) break;

        Debug.Log($"[RewardZoneSystem] Spawned {activeZones.Count} initial reward zones");
    }

    private void HandleWaveEnded()
    {
        if (subscribedWaves == null) return;
        int wave = subscribedWaves.GetCurrentWave();
        if (wave <= 0 || wave % respawnEveryNWaves != 0) return;
        if (activeZones.Count >= maxActiveZones) return;

        int farthest = GetFarthestPlacedDistance();
        int min = Mathf.Max(minDistanceTiles, farthest + 1);
        if (TrySpawnZone(min, min + maxDistanceTiles))
            Log.Info("RewardZoneSystem", $"New reward zone after wave {wave}");
    }

    private void HandleZoomChanged(float zoom)
    {
        currentZoom = zoom;
        RefreshVisibility();
    }

    private void RefreshVisibility()
    {
        bool visible = currentZoom >= visibleZoomThreshold || GameModifiersManager.Stat(ModifierStat.RewardZoneAlwaysVisible) > 0f;
        foreach (var zone in activeZones)
            if (zone != null && zone.IsVisible != visible) zone.SetVisible(visible);
    }

    private int GetFarthestPlacedDistance()
    {
        if (grid == null || fog == null || !fog.IsInitialized) return 0;
        float best = 0f;
        for (int i = 0; i < grid.PlacedTileCount; i++)
        {
            var idx = fog.CenterOf(grid.GetPlacedTile(i));
            best = Mathf.Max(best, idx.magnitude);
        }
        return Mathf.CeilToInt(best);
    }

    private bool TrySpawnZone(int minDistance, int maxDistance)
    {
        if (fog == null || !fog.IsInitialized || rewardTable == null)
        {
            if (rewardTable == null) Debug.LogWarning("[RewardZoneSystem] RewardTableSO is not assigned");
            return false;
        }

        var candidates = new List<Vector2Int>();
        foreach (var index in fog.FoggedIndices)
        {
            float d = index.magnitude;
            if (d < minDistance || d > maxDistance) continue;
            if (IsZoneAt(index)) continue;
            if (OverlapsPlaced(index)) continue;
            candidates.Add(index);
        }

        if (candidates.Count == 0)
        {
            Log.Info("RewardZoneSystem", $"No fog position available between {minDistance} and {maxDistance} tiles");
            return false;
        }

        var chosen = candidates[Random.Range(0, candidates.Count)];
        var entry = rewardTable.PickRandom();
        if (entry == null) return false;

        var zone = GetZone();
        Vector3 center = fog.IndexToWorld(chosen);
        zone.Setup(chosen, center, fog.Footprint, fog.FogHeight + heightAboveFog, entry.type, entry.amount, GetIcon(entry.type));
        zone.gameObject.SetActive(true);
        activeZones.Add(zone);
        RefreshVisibility();
        zone.SetVisible(zone.IsVisible || currentZoom >= visibleZoomThreshold || GameModifiersManager.Stat(ModifierStat.RewardZoneAlwaysVisible) > 0f);
        return true;
    }

    private bool IsZoneAt(Vector2Int index)
    {
        foreach (var zone in activeZones)
            if (zone != null && (zone.GridIndex - index).sqrMagnitude <= 1) return true;
        return false;
    }

    private bool OverlapsPlaced(Vector2Int index)
    {
        if (grid == null) return false;
        fog.GetFogBounds(index, out var min, out var max);
        for (int i = 0; i < grid.PlacedTileCount; i++)
        {
            var t = grid.GetPlacedTile(i);
            if (fog.Overlaps(min, max, t.aabbMin, t.aabbMax)) return true;
        }
        return false;
    }

    private RewardZone GetZone()
    {
        RewardZone zone = pool.Count > 0 ? pool.Dequeue() : null;
        if (zone == null)
        {
            if (zonePrefab != null) zone = Instantiate(zonePrefab, zoneRoot);
            else
            {
                var go = new GameObject("RewardZone");
                go.transform.SetParent(zoneRoot, false);
                zone = go.AddComponent<RewardZone>();
            }
        }
        zone.transform.SetParent(zoneRoot, true);
        return zone;
    }

    public RewardZone FindZoneOverlapping(Vector3 min, Vector3 max)
    {
        if (fog == null || !fog.IsInitialized) return null;

        foreach (var zone in activeZones)
        {
            if (zone == null || !zone.IsVisible) continue;
            fog.GetFogBounds(zone.GridIndex, out var zMin, out var zMax);
            if (fog.Overlaps(zMin, zMax, min, max)) return zone;
        }
        return null;
    }

    public Sprite GetIcon(RewardType type)
    {
        foreach (var icon in icons)
            if (icon != null && icon.type == type) return icon.sprite;
        return null;
    }

    private void HandleTilePlaced(PlacedTile tile)
    {
        if (fog == null || !fog.IsInitialized) return;

        claimed.Clear();
        foreach (var zone in activeZones)
        {
            if (zone == null) continue;
            fog.GetFogBounds(zone.GridIndex, out var min, out var max);
            if (fog.Overlaps(min, max, tile.aabbMin, tile.aabbMax)) claimed.Add(zone);
        }

        foreach (var zone in claimed)
        {
            activeZones.Remove(zone);
            PlayClaimFeedback(zone.transform.position, zone.Reward);
            Grant(zone.Reward, zone.Amount);
            TutorialEventHub.RaiseRewardClaimed();
            zone.gameObject.SetActive(false);
            pool.Enqueue(zone);
        }
    }

    private void PlayClaimFeedback(Vector3 position, RewardType type)
    {
        Color color = RewardClaimFX.ColorFor(type);
        if (claimVfxPrefab != null) VfxPool.Spawn(claimVfxPrefab, position, Quaternion.identity, claimVfxLifetime);
        RewardClaimFX.Beam(position, color, beamHeight, beamWidth, beamDuration);
        RewardClaimFX.Burst(claimBurstPrefab, position, color, claimBurstLifetime);
        PlayClaimSound(type);
    }

    private void PlayClaimSound(RewardType type)
    {
        if (claimAudioSource == null)
        {
            claimAudioSource = gameObject.AddComponent<AudioSource>();
            claimAudioSource.playOnAwake = false;
            claimAudioSource.spatialBlend = 0f;
        }

        AudioClip clip = null;
        foreach (var rc in claimClips)
            if (rc != null && rc.type == type && rc.clip != null) { clip = rc.clip; break; }

        float pitch = 1f;
        if (clip == null)
        {
            clip = claimClip;
            if (clip == null)
            {
                if (generatedClaimTone == null) generatedClaimTone = RewardClaimFX.BuildTone(660f);
                clip = generatedClaimTone;
            }
            pitch = RewardClaimFX.PitchFor(type);
        }

        claimAudioSource.pitch = pitch;
        claimAudioSource.PlayOneShot(clip, claimVolume);
    }

    private string toastPrefix;

    public void Grant(RewardType type, int amount, string prefix)
    {
        toastPrefix = prefix;
        try
        {
            Grant(type, amount);
        }
        finally
        {
            toastPrefix = null;
        }
    }

    public void Toast(string message)
    {
        ShowToast(message);
    }

    public void Grant(RewardType type, int amount)
    {
        float bonus = GetRewardBonus();

        switch (type)
        {
            case RewardType.BlueEssence:
                GrantEssence(true, amount, bonus);
                break;
            case RewardType.RedEssence:
                GrantEssence(false, amount, bonus);
                break;
            case RewardType.FreeTurretDupe:
                if (!TryGrantTurretDupe()) GrantEssence(false, GetEssenceAmount(RewardType.RedEssence), bonus);
                break;
            case RewardType.FreeTileDupe:
                if (!TryGrantTileDupe()) GrantEssence(true, GetEssenceAmount(RewardType.BlueEssence), bonus);
                break;
            case RewardType.ExtraModifierPick:
                GrantExtraPick();
                break;
        }
    }

    private static float GetRewardBonus()
    {
        return GameModifiersManager.Multiplier(ModifierStat.RewardZoneBonus);
    }

    private int GetEssenceAmount(RewardType type)
    {
        return rewardTable != null ? rewardTable.GetAmount(type, fallbackEssenceAmount) : fallbackEssenceAmount;
    }

    private void GrantEssence(bool blue, int amount, float bonus)
    {
        if (amount <= 0) amount = fallbackEssenceAmount;
        int total = Mathf.Max(1, Mathf.RoundToInt(amount * bonus));

        if (essences == null) essences = FindFirstObjectByType<WorldSwitchPoints>();
        if (essences == null)
        {
            Debug.LogWarning("[RewardZoneSystem] WorldSwitchPoints not found. Essence reward lost");
            return;
        }

        if (blue) essences.AddBlueEssence(total);
        else essences.AddRedEssence(total);

        ShowToast(blue ? $"+{total} Normal Essences" : $"+{total} Other World Essences");
    }

    private bool TryGrantTurretDupe()
    {
        var dupeSystem = FindFirstObjectByType<TurretDupeSystem>();
        if (dupeSystem == null) return false;

        var types = new List<TurretDataSO>();
        foreach (var turret in FindObjectsByType<Turret>(FindObjectsSortMode.None))
        {
            if (turret == null || !turret.IsPlaced) continue;
            var holder = turret.GetComponent<TurretDataHolder>();
            if (holder != null && holder.turretDataSO != null && !types.Contains(holder.turretDataSO) && RunLoadout.ContainsTurret(holder.turretDataSO))
                types.Add(holder.turretDataSO);
        }

        if (types.Count == 0) return false;

        var chosen = types[Random.Range(0, types.Count)];
        dupeSystem.AddDupe(chosen);
        ShowToast($"Free copy: {(string.IsNullOrEmpty(chosen.displayName) ? chosen.name : chosen.displayName)}");
        return true;
    }

    public static bool InLoadout(TileLayout layout)
    {
        if (layout == null) return false;
        var key = layout.DupeKey;
        foreach (var t in RunLoadout.Tiles) if (t != null && t.DupeKey == key) return true;
        return false;
    }

    private bool TryGrantTileDupe()
    {
        var dupeSystem = FindFirstObjectByType<TileDupeSystem>();
        if (dupeSystem == null || grid == null) return false;

        var layouts = new List<TileLayout>();
        for (int i = 0; i < grid.PlacedTileCount; i++)
        {
            var layout = grid.GetPlacedTile(i).layout;
            if (layout == null || layout == grid.InitialLayout || layouts.Contains(layout) || !InLoadout(layout)) continue;
            layouts.Add(layout);
        }

        if (layouts.Count == 0) return false;

        var chosen = layouts[Random.Range(0, layouts.Count)];
        dupeSystem.AddDupe(chosen);
        ShowToast($"Free copy: {chosen.name}");
        return true;
    }

    private void GrantExtraPick()
    {
        var panel = ModifierPanelSelection.Instance != null
            ? ModifierPanelSelection.Instance
            : FindFirstObjectByType<ModifierPanelSelection>(FindObjectsInactive.Include);

        if (panel == null)
        {
            Debug.LogWarning("[RewardZoneSystem] ModifierPanelSelection not found. Extra pick replaced by essences");
            GrantEssence(WorldState.Current == ShiftingWorldMechanic.World.Normal, fallbackEssenceAmount, GetRewardBonus());
            return;
        }

        ShowToast("Extra modifier pick");
        ModalPanelQueue.Instance.Enqueue(() => panel.OpenExtraPick(), () => panel != null && panel.IsOpen, ModalPriority.ExtraPick, "Extra pick");
    }

    private void ShowToast(string message)
    {
        if (!string.IsNullOrEmpty(toastPrefix)) message = toastPrefix + message;
        Log.Info("RewardZoneSystem", $"Reward: {message}");
        EnsureToast();
        if (toastText == null) return;

        if (toastRoutine != null) StopCoroutine(toastRoutine);
        toastRoutine = StartCoroutine(ToastRoutine(message));
    }

    private void EnsureToast()
    {
        if (toastText != null) return;
        if (toastCanvas == null)
        {
            var ui = FindFirstObjectByType<UIController>();
            if (ui != null) toastCanvas = ui.GetComponentInParent<Canvas>();
            if (toastCanvas == null) toastCanvas = FindFirstObjectByType<Canvas>();
        }
        if (toastCanvas == null) return;

        var go = new GameObject("RewardToast", typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(toastCanvas.rootCanvas.transform, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.78f);
        rt.sizeDelta = new Vector2(700f, 60f);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = 36f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(1f, 0.9f, 0.4f, 1f);
        tmp.outlineWidth = 0.2f;
        tmp.raycastTarget = false;
        toastText = tmp;
        go.SetActive(false);
    }

    private IEnumerator ToastRoutine(string message)
    {
        toastText.text = message;
        toastText.gameObject.SetActive(true);
        var rt = toastText.rectTransform;
        Color baseColor = toastText.color;
        float t = 0f;
        while (t < toastSeconds)
        {
            t += Time.unscaledDeltaTime;
            float k = t / toastSeconds;
            rt.localScale = Vector3.one * (k < 0.1f ? Mathf.Lerp(1.3f, 1f, k / 0.1f) : 1f);
            var c = baseColor;
            c.a = k > 0.8f ? Mathf.Clamp01((1f - k) / 0.2f) : 1f;
            toastText.color = c;
            yield return null;
        }
        toastText.color = baseColor;
        toastText.gameObject.SetActive(false);
        toastRoutine = null;
    }
}
