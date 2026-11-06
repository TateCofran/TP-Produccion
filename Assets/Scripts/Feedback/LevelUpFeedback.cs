using System.Collections.Generic;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class LevelUpFeedback : MonoBehaviour
{
    public static LevelUpFeedback Instance { get; private set; }

    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    private class FlashTarget
    {
        public Renderer renderer;
        public float start;
        public Color color;
    }

    private class Label
    {
        public TextMeshPro text;
        public Vector3 start;
        public float startTime;
        public bool active;
    }

    [Header("Refs")]
    [SerializeField] private TurretDupeSystem turretDupes;
    [SerializeField] private TileDupeSystem tileDupes;
    [SerializeField] private TMP_FontAsset font;

    [Header("Flash")]
    [SerializeField] private Color turretFlashColor = new Color(1f, 0.85f, 0.3f, 1f);
    [SerializeField] private Color tileFlashColor = new Color(0.6f, 1f, 0.6f, 1f);
    [SerializeField, Min(0f)] private float flashIntensity = 2f;
    [SerializeField, Min(0.01f)] private float flashDuration = 0.45f;

    [Header("Floating Label")]
    [SerializeField, Min(0.1f)] private float labelDuration = 1.2f;
    [SerializeField, Min(0f)] private float labelRise = 1.5f;
    [SerializeField] private Vector3 labelOffset = new Vector3(0f, 2f, 0f);
    [SerializeField, Min(0.1f)] private float labelFontSize = 5f;

    [Header("Audio")]
    [Tooltip("Optional. A rising tone is generated when empty.")]
    [SerializeField] private AudioClip levelUpClip;
    [SerializeField, Range(0f, 1f)] private float volume = 0.5f;
    [SerializeField, Min(0f)] private float pitchPerLevel = 0.08f;
    [SerializeField, Min(1f)] private float maxPitch = 2f;

    private readonly List<FlashTarget> flashes = new();
    private readonly List<Label> labels = new();
    private readonly HashSet<Material> prepared = new();
    private MaterialPropertyBlock block;
    private AudioSource source;
    private Camera cam;

    public int ActiveFlashes => flashes.Count;
    public int ActiveLabels { get { int n = 0; foreach (var l in labels) if (l.active) n++; return n; } }
    public float LastPitch { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        block = new MaterialPropertyBlock();
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        if (levelUpClip == null) levelUpClip = RewardClaimFX.BuildTone(523f);
        if (turretDupes == null) turretDupes = FindFirstObjectByType<TurretDupeSystem>();
        if (tileDupes == null) tileDupes = FindFirstObjectByType<TileDupeSystem>();
    }

    private void OnEnable()
    {
        if (turretDupes != null) turretDupes.OnTurretLevelUp += HandleTurretLevelUp;
        if (tileDupes != null) tileDupes.OnTileLevelUp += HandleTileLevelUp;
    }

    private void OnDisable()
    {
        if (turretDupes != null) turretDupes.OnTurretLevelUp -= HandleTurretLevelUp;
        if (tileDupes != null) tileDupes.OnTileLevelUp -= HandleTileLevelUp;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void HandleTurretLevelUp(TurretDataSO turret, TurretLevelData data)
    {
        if (turret == null) return;
        int level = data != null ? data.currentLevel : 2;
        foreach (var holder in FindObjectsByType<TurretDataHolder>(FindObjectsSortMode.None))
        {
            if (holder == null || holder.turretDataSO == null) continue;
            if (holder.turretDataSO != turret && holder.turretDataSO.id != turret.id) continue;
            Flash(holder.gameObject, turretFlashColor);
            ShowLabel(holder.transform.position, level);
        }
        PlaySound(level);
    }

    private void HandleTileLevelUp(TileLayout layout, TileLevelData data)
    {
        if (layout == null) return;
        int level = data != null ? data.currentLevel : 2;
        foreach (var applier in FindObjectsByType<TileLevelApplier>(FindObjectsSortMode.None))
        {
            if (applier == null || applier.Layout != layout) continue;
            Flash(applier.gameObject, tileFlashColor);
            ShowLabel(CenterOf(applier.transform), level);
        }
        PlaySound(level);
    }

    private static Vector3 CenterOf(Transform root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return root.position;
        var b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
        return new Vector3(b.center.x, b.max.y, b.center.z);
    }

    public void Flash(GameObject root, Color color)
    {
        float mult = FeedbackSettings.Current.FlashMultiplier;
        if (root == null || mult <= 0f) return;

        float now = Time.unscaledTime;
        foreach (var r in root.GetComponentsInChildren<Renderer>())
        {
            if (r == null || r is ParticleSystemRenderer || r is LineRenderer || r is TrailRenderer || r is SpriteRenderer) continue;
            PrepareEmission(r);
            Color emission = color * flashIntensity * mult;
            r.GetPropertyBlock(block);
            block.SetColor(EmissionColorId, emission);
            r.SetPropertyBlock(block);
            flashes.Add(new FlashTarget { renderer = r, start = now, color = emission });
        }
    }

    private void PrepareEmission(Renderer r)
    {
        foreach (var m in r.sharedMaterials)
        {
            if (m == null || prepared.Contains(m) || !m.HasProperty(EmissionColorId)) continue;
            if (!m.IsKeywordEnabled("_EMISSION"))
            {
                var mats = r.materials;
                foreach (var inst in mats)
                {
                    if (inst == null || !inst.HasProperty(EmissionColorId)) continue;
                    inst.EnableKeyword("_EMISSION");
                    inst.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                    inst.SetColor(EmissionColorId, Color.black);
                    prepared.Add(inst);
                }
                return;
            }
            prepared.Add(m);
        }
    }

    private void ShowLabel(Vector3 position, int level)
    {
        Label label = null;
        foreach (var l in labels) if (!l.active) { label = l; break; }
        if (label == null)
        {
            var go = new GameObject("LevelUpLabel");
            go.transform.SetParent(transform, false);
            var tmp = go.AddComponent<TextMeshPro>();
            if (font != null) tmp.font = font;
            tmp.fontSize = labelFontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = new Color(1f, 0.9f, 0.35f, 1f);
            tmp.outlineWidth = 0.2f;
            tmp.rectTransform.sizeDelta = new Vector2(5f, 1.5f);
            label = new Label { text = tmp };
            labels.Add(label);
        }

        label.text.text = $"Lv {level}";
        label.start = position + labelOffset;
        label.startTime = Time.unscaledTime;
        label.active = true;
        label.text.transform.position = label.start;
        label.text.gameObject.SetActive(true);
    }

    private void PlaySound(int level)
    {
        if (source == null || levelUpClip == null) return;
        LastPitch = Mathf.Min(maxPitch, 1f + pitchPerLevel * Mathf.Max(0, level - 1));
        source.pitch = LastPitch;
        source.PlayOneShot(levelUpClip, volume);
    }

    private void Update()
    {
        float now = Time.unscaledTime;

        for (int i = flashes.Count - 1; i >= 0; i--)
        {
            var f = flashes[i];
            float k = (now - f.start) / flashDuration;
            if (f.renderer == null)
            {
                flashes.RemoveAt(i);
                continue;
            }
            f.renderer.GetPropertyBlock(block);
            if (k >= 1f)
            {
                block.SetColor(EmissionColorId, Color.black);
                f.renderer.SetPropertyBlock(block);
                flashes.RemoveAt(i);
                continue;
            }
            block.SetColor(EmissionColorId, f.color * (1f - k));
            f.renderer.SetPropertyBlock(block);
        }

        if (cam == null) cam = Camera.main;
        foreach (var l in labels)
        {
            if (!l.active) continue;
            float k = (now - l.startTime) / labelDuration;
            if (k >= 1f)
            {
                l.active = false;
                l.text.gameObject.SetActive(false);
                continue;
            }
            l.text.transform.position = l.start + Vector3.up * labelRise * k;
            if (cam != null) l.text.transform.rotation = cam.transform.rotation;
            float scale = k < 0.15f ? Mathf.Lerp(0.5f, 1.2f, k / 0.15f) : Mathf.Lerp(1.2f, 1f, Mathf.Clamp01((k - 0.15f) / 0.2f));
            l.text.transform.localScale = Vector3.one * scale;
            var c = l.text.color;
            c.a = k > 0.7f ? (1f - k) / 0.3f : 1f;
            l.text.color = c;
        }
    }
}
