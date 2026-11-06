using UnityEngine;

[DisallowMultipleComponent]
public class RewardZone : MonoBehaviour
{
    [Header("Border")]
    [SerializeField] private LineRenderer border;
    [SerializeField] private Material borderMaterial;
    [SerializeField] private Color borderColor = new Color(1f, 0.85f, 0.1f, 1f);
    [SerializeField, Min(0.01f)] private float borderWidth = 0.15f;
    [SerializeField, Min(0f)] private float borderInset = 0.1f;

    [Header("Pulse")]
    [SerializeField] private bool pulse = true;
    [SerializeField, Min(0f)] private float pulseSpeed = 2f;
    [SerializeField, Range(0f, 1f)] private float pulseMinAlpha = 0.45f;

    [Header("Icon")]
    [SerializeField] private SpriteRenderer icon;
    [SerializeField] private float iconHeight = 0.6f;

    private static Material fallbackMaterial;

    private static Material GetFallbackMaterial()
    {
        if (fallbackMaterial != null) return fallbackMaterial;
        var shader = Shader.Find("Sprites/Default");
        if (shader == null) return null;
        fallbackMaterial = new Material(shader) { name = "RewardZoneBorderFallback" };
        return fallbackMaterial;
    }

    public Bounds Bounds { get; private set; }
    public RewardType Reward { get; private set; }
    public int Amount { get; private set; }
    public Vector2Int GridIndex { get; private set; }
    public bool IsVisible { get; private set; }

    private void Awake()
    {
        EnsureBorder();
    }

    private void Update()
    {
        if (!IsVisible || !pulse || border == null) return;

        float t = (Mathf.Sin(Time.unscaledTime * pulseSpeed) + 1f) * 0.5f;
        var c = borderColor;
        c.a = Mathf.Lerp(pulseMinAlpha, 1f, t);
        border.startColor = c;
        border.endColor = c;
    }

    public void Setup(Vector2Int gridIndex, Vector3 center, float size, float height, RewardType reward, int amount, Sprite iconSprite)
    {
        EnsureBorder();

        GridIndex = gridIndex;
        Reward = reward;
        Amount = amount;
        Bounds = new Bounds(new Vector3(center.x, 0f, center.z), new Vector3(size, 1000f, size));

        transform.position = new Vector3(center.x, height, center.z);

        float half = Mathf.Max(0.01f, size * 0.5f - borderInset);
        border.useWorldSpace = false;
        border.loop = true;
        border.positionCount = 4;
        border.SetPosition(0, new Vector3(-half, 0f, -half));
        border.SetPosition(1, new Vector3(-half, 0f, half));
        border.SetPosition(2, new Vector3(half, 0f, half));
        border.SetPosition(3, new Vector3(half, 0f, -half));
        border.startWidth = borderWidth;
        border.endWidth = borderWidth;
        border.startColor = borderColor;
        border.endColor = borderColor;

        if (icon != null)
        {
            icon.sprite = iconSprite;
            icon.transform.localPosition = new Vector3(0f, iconHeight, 0f);
        }
    }

    public void SetVisible(bool visible)
    {
        IsVisible = visible;
        if (border != null) border.enabled = visible;
        if (icon != null) icon.enabled = visible && icon.sprite != null;
    }

    private void EnsureBorder()
    {
        if (border == null) border = GetComponent<LineRenderer>();
        if (border == null) border = gameObject.AddComponent<LineRenderer>();

        border.alignment = LineAlignment.View;
        border.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        border.receiveShadows = false;
        if (borderMaterial != null) border.sharedMaterial = borderMaterial;
        else if (border.sharedMaterial == null) border.sharedMaterial = GetFallbackMaterial();
        transform.rotation = Quaternion.identity;
    }
}
