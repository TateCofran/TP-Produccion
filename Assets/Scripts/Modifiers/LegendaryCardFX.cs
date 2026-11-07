using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class LegendaryCardFX : MonoBehaviour
{
    public static readonly Color Gold = new Color(1f, 0.78f, 0.2f, 1f);

    [Header("Frame")]
    [SerializeField] private Vector2 frameDistance = new Vector2(5f, 5f);
    [SerializeField, Min(0f)] private float pulseSpeed = 3f;

    [Header("Sparkles")]
    [SerializeField, Min(0)] private int sparkleCount = 10;
    [SerializeField, Min(1f)] private float sparkleSize = 7f;
    [SerializeField, Min(0.1f)] private float sparkleLifetime = 1.6f;
    [SerializeField, Min(0f)] private float sparkleRise = 70f;

    private Outline frame;
    private RectTransform[] sparkles;
    private Image[] sparkleImages;
    private float[] phases;
    private Vector2[] origins;
    private bool built;

    public bool IsBuilt => built;

    private void OnEnable()
    {
        Build();
    }

    public void Build()
    {
        if (built) return;
        built = true;

        var graphic = GetComponent<Graphic>();
        if (graphic != null)
        {
            frame = gameObject.AddComponent<Outline>();
            frame.effectColor = Gold;
            frame.effectDistance = frameDistance;
        }

        var rect = (RectTransform)transform;
        sparkles = new RectTransform[sparkleCount];
        sparkleImages = new Image[sparkleCount];
        phases = new float[sparkleCount];
        origins = new Vector2[sparkleCount];
        for (int i = 0; i < sparkleCount; i++)
        {
            var go = new GameObject("LegendarySparkle", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(rect, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(sparkleSize, sparkleSize);
            rt.localRotation = Quaternion.Euler(0f, 0f, 45f);
            var img = go.GetComponent<Image>();
            img.color = Gold;
            img.raycastTarget = false;
            sparkles[i] = rt;
            sparkleImages[i] = img;
            phases[i] = Random.value;
            float w = rect.rect.width > 1f ? rect.rect.width : 200f;
            float h = rect.rect.height > 1f ? rect.rect.height : 280f;
            origins[i] = new Vector2(Random.Range(-0.45f, 0.45f) * w, Random.Range(-0.5f, 0.1f) * h);
        }
    }

    private void Update()
    {
        if (!built) return;
        float t = Time.unscaledTime;

        if (frame != null)
        {
            float pulse = 0.65f + 0.35f * Mathf.Sin(t * pulseSpeed);
            var c = Gold;
            c.a = pulse;
            frame.effectColor = c;
        }

        for (int i = 0; i < sparkles.Length; i++)
        {
            float k = Mathf.Repeat(t / sparkleLifetime + phases[i], 1f);
            sparkles[i].anchoredPosition = origins[i] + new Vector2(0f, k * sparkleRise);
            var c = Gold;
            c.a = Mathf.Sin(k * Mathf.PI);
            sparkleImages[i].color = c;
            float s = 0.6f + 0.4f * Mathf.Sin(k * Mathf.PI);
            sparkles[i].localScale = new Vector3(s, s, 1f);
        }
    }
}
