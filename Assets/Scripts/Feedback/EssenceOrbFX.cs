using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class EssenceOrbFX : MonoBehaviour
{
    public static EssenceOrbFX Instance { get; private set; }

    private class Orb
    {
        public RectTransform rect;
        public Image image;
        public Vector2 start;
        public Vector2 control;
        public float elapsed;
        public float duration;
        public int amount;
        public bool blue;
        public bool active;
    }

    [Header("Refs")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform target;
    [SerializeField] private EssencePopupUI essenceUI;
    [SerializeField] private Camera worldCamera;

    [Header("Look")]
    [SerializeField] private Sprite blueIcon;
    [SerializeField] private Sprite redIcon;
    [SerializeField] private Color blueColor = new Color(0.35f, 0.65f, 1f, 1f);
    [SerializeField] private Color redColor = new Color(1f, 0.35f, 0.3f, 1f);
    [SerializeField, Min(4f)] private float orbSize = 30f;
    [SerializeField, Min(0f)] private float extraSizePerAmount = 2f;
    [SerializeField, Min(4f)] private float maxOrbSize = 52f;

    private readonly List<Orb> orbs = new();
    private readonly Stack<Orb> free = new();
    private RectTransform canvasRect;
    private Orb lastSpawned;

    public int ActiveOrbs { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        if (canvas != null) canvas = canvas.rootCanvas;
        canvasRect = canvas != null ? canvas.transform as RectTransform : null;
        if (blueIcon == null) blueIcon = FeedbackSprites.Drop;
        if (redIcon == null) redIcon = FeedbackSprites.Flame;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public bool CanSpawn => isActiveAndEnabled && canvasRect != null && target != null;

    public void Spawn(Vector3 worldPosition, int amount, bool blue)
    {
        if (amount <= 0) return;
        if (essenceUI != null) essenceUI.RegisterInFlight(amount, blue);
        if (!CanSpawn)
        {
            Arrive(amount, blue);
            return;
        }

        var settings = FeedbackSettings.Current;
        if (ActiveOrbs >= settings.maxOrbs && lastSpawned != null && lastSpawned.active && lastSpawned.blue == blue)
        {
            lastSpawned.amount += amount;
            ResizeOrb(lastSpawned);
            return;
        }
        if (ActiveOrbs >= settings.maxOrbs)
        {
            var merge = FindActive(blue);
            if (merge != null)
            {
                merge.amount += amount;
                ResizeOrb(merge);
                return;
            }
        }

        var cam = worldCamera != null ? worldCamera : Camera.main;
        Vector2 startLocal;
        if (cam != null)
        {
            Vector3 screen = cam.WorldToScreenPoint(worldPosition);
            if (screen.z < 0f) screen = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, CanvasCamera(), out startLocal);
        }
        else
        {
            startLocal = Vector2.zero;
        }

        Vector2 end = TargetLocal();
        Vector2 mid = (startLocal + end) * 0.5f;
        var orb = GetOrb();
        orb.start = startLocal;
        orb.control = mid + Vector2.up * settings.orbArcHeight;
        orb.elapsed = 0f;
        orb.duration = UnityEngine.Random.Range(settings.orbMinDuration, settings.orbMaxDuration);
        orb.amount = amount;
        orb.blue = blue;
        orb.active = true;
        orb.image.sprite = blue ? blueIcon : redIcon;
        orb.image.color = blue ? blueColor : redColor;
        orb.rect.anchoredPosition = startLocal;
        ResizeOrb(orb);
        orb.rect.gameObject.SetActive(true);
        orb.rect.SetAsLastSibling();

        lastSpawned = orb;
        ActiveOrbs++;
    }

    private void Update()
    {
        if (ActiveOrbs == 0) return;

        Vector2 end = TargetLocal();
        float dt = Time.unscaledDeltaTime;

        for (int i = 0; i < orbs.Count; i++)
        {
            var orb = orbs[i];
            if (!orb.active) continue;

            orb.elapsed += dt;
            float k = Mathf.Clamp01(orb.elapsed / orb.duration);
            float eased = k * k;
            Vector2 a = Vector2.Lerp(orb.start, orb.control, eased);
            Vector2 b = Vector2.Lerp(orb.control, end, eased);
            orb.rect.anchoredPosition = Vector2.Lerp(a, b, eased);
            orb.rect.localScale = Vector3.one * Mathf.Lerp(1f, 0.75f, eased);

            if (k >= 1f) Finish(orb);
        }
    }

    private void Finish(Orb orb)
    {
        orb.active = false;
        orb.rect.gameObject.SetActive(false);
        ActiveOrbs = Mathf.Max(0, ActiveOrbs - 1);
        if (lastSpawned == orb) lastSpawned = null;
        free.Push(orb);
        Arrive(orb.amount, orb.blue);
    }

    private void Arrive(int amount, bool blue)
    {
        if (essenceUI != null) essenceUI.AddArrived(amount, blue);
    }

    private Orb FindActive(bool blue)
    {
        for (int i = orbs.Count - 1; i >= 0; i--)
            if (orbs[i].active && orbs[i].blue == blue) return orbs[i];
        return null;
    }

    private void ResizeOrb(Orb orb)
    {
        float size = Mathf.Min(maxOrbSize, orbSize + extraSizePerAmount * Mathf.Max(0, orb.amount - 1));
        orb.rect.sizeDelta = new Vector2(size, size);
    }

    private Orb GetOrb()
    {
        if (free.Count > 0) return free.Pop();

        var go = new GameObject("EssenceOrb", typeof(RectTransform), typeof(Image));
        var rect = (RectTransform)go.transform;
        rect.SetParent(canvasRect, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        var img = go.GetComponent<Image>();
        img.raycastTarget = false;
        img.preserveAspect = true;
        go.SetActive(false);

        var orb = new Orb { rect = rect, image = img };
        orbs.Add(orb);
        return orb;
    }

    private Vector2 TargetLocal()
    {
        if (target == null || canvasRect == null) return Vector2.zero;
        Vector3 world = target.TransformPoint(target.rect.center);
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(CanvasCamera(), world);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, CanvasCamera(), out var local);
        return local;
    }

    private Camera CanvasCamera()
    {
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay) return null;
        return canvas.worldCamera;
    }
}
