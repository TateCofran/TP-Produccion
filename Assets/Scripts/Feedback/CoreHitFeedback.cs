using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class CoreHitFeedback : MonoBehaviour
{
    public static CoreHitFeedback Instance { get; private set; }

    [Header("Refs")]
    [Tooltip("HUD canvas. The vignette overlay is built inside it when not assigned.")]
    [SerializeField] private Canvas hudCanvas;
    [SerializeField] private Image vignette;

    [Header("Hit")]
    [SerializeField] private Color vignetteColor = new Color(0.85f, 0f, 0f, 1f);
    [SerializeField, Range(0f, 1f)] private float hitAlpha = 0.75f;
    [SerializeField, Min(0.01f)] private float hitDuration = 0.3f;
    [SerializeField, Range(0f, 1f)] private float hitTrauma = 0.35f;

    [Header("Low Health")]
    [SerializeField, Min(0)] private int lowHealthThreshold = 3;
    [SerializeField, Range(0f, 1f)] private float lowHealthAlpha = 0.25f;
    [SerializeField, Min(0f)] private float lowHealthPulseSpeed = 2f;
    [SerializeField, Range(0f, 1f)] private float lowHealthPulseAmount = 0.08f;

    private float hitStart = -999f;
    private bool lowHealth;

    public float CurrentAlpha => vignette != null ? vignette.color.a : 0f;
    public bool LowHealth => lowHealth;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        BuildOverlay();
        SetAlpha(0f);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public static void NotifyHit(int currentHealth, int maxHealth)
    {
        if (Instance != null) Instance.OnHit(currentHealth, maxHealth);
    }

    public static void NotifyHealth(int currentHealth, int maxHealth)
    {
        if (Instance != null) Instance.lowHealth = currentHealth > 0 && currentHealth <= Instance.lowHealthThreshold;
    }

    private void OnHit(int currentHealth, int maxHealth)
    {
        hitStart = Time.unscaledTime;
        lowHealth = currentHealth > 0 && currentHealth <= lowHealthThreshold;
        CameraShake.AddTrauma(hitTrauma);
    }

    private void Update()
    {
        if (vignette == null) return;

        float baseAlpha = 0f;
        if (lowHealth)
        {
            float pulse = Mathf.Sin(Time.unscaledTime * lowHealthPulseSpeed) * lowHealthPulseAmount;
            baseAlpha = Mathf.Clamp01(lowHealthAlpha + pulse);
        }

        float hitAlphaNow = 0f;
        float k = (Time.unscaledTime - hitStart) / hitDuration;
        if (k >= 0f && k < 1f) hitAlphaNow = hitAlpha * (1f - k * k);

        float flash = FeedbackSettings.Current.FlashMultiplier;
        if (flash <= 0f) flash = 0.35f;
        SetAlpha(Mathf.Max(baseAlpha, hitAlphaNow * flash));
    }

    private void SetAlpha(float alpha)
    {
        if (vignette == null) return;
        var c = vignetteColor;
        c.a = alpha;
        vignette.color = c;
        vignette.enabled = alpha > 0.001f;
    }

    private void BuildOverlay()
    {
        if (vignette != null) return;
        if (hudCanvas == null)
        {
            var ui = FindFirstObjectByType<UIController>();
            if (ui != null) hudCanvas = ui.GetComponentInParent<Canvas>();
            if (hudCanvas == null) hudCanvas = FindFirstObjectByType<Canvas>();
        }
        if (hudCanvas == null) return;

        var go = new GameObject("CoreHitVignette", typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(hudCanvas.rootCanvas.transform, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.SetAsFirstSibling();
        vignette = go.GetComponent<Image>();
        vignette.sprite = FeedbackSprites.Vignette;
        vignette.raycastTarget = false;
    }
}
