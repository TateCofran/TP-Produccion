using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class OffscreenIndicator : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private Camera worldCamera;

    [Header("Look")]
    [SerializeField] private Color color = new Color(1f, 0.82f, 0.2f, 1f);
    [SerializeField, Min(8f)] private float size = 48f;
    [SerializeField, Min(0f)] private float edgePadding = 40f;

    private RectTransform arrow;
    private Transform target;

    public bool IsShowing => arrow != null && arrow.gameObject.activeSelf;

    private void Awake()
    {
        if (canvas == null)
        {
            var ui = FindFirstObjectByType<UIController>();
            if (ui != null) canvas = ui.GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
        }
        if (canvas != null) canvas = canvas.rootCanvas;
        Build();
    }

    private void OnEnable()
    {
        BountyMarker.OnBountyStarted += HandleStarted;
        BountyMarker.OnBountyEnded += HandleEnded;
    }

    private void OnDisable()
    {
        BountyMarker.OnBountyStarted -= HandleStarted;
        BountyMarker.OnBountyEnded -= HandleEnded;
    }

    private void HandleStarted(BountyMarker marker) => target = marker != null ? marker.transform : null;

    private void HandleEnded(BountyMarker marker)
    {
        if (marker != null && target == marker.transform) target = null;
    }

    public void Track(Transform t) => target = t;

    private void Build()
    {
        if (canvas == null || arrow != null) return;
        var go = new GameObject("OffscreenArrow", typeof(RectTransform), typeof(Image));
        arrow = (RectTransform)go.transform;
        arrow.SetParent(canvas.transform, false);
        arrow.sizeDelta = new Vector2(size, size);
        var img = go.GetComponent<Image>();
        img.sprite = ExitArrowSprite.Get();
        img.color = color;
        img.raycastTarget = false;
        go.SetActive(false);
    }

    private void LateUpdate()
    {
        if (arrow == null) return;
        if (target == null || !target.gameObject.activeInHierarchy)
        {
            if (arrow.gameObject.activeSelf) arrow.gameObject.SetActive(false);
            return;
        }

        var cam = worldCamera != null ? worldCamera : Camera.main;
        if (cam == null) return;

        Vector3 sp = cam.WorldToScreenPoint(target.position);
        bool behind = sp.z < 0f;
        if (behind) sp = -sp;
        bool onScreen = !behind && sp.x > 0 && sp.x < Screen.width && sp.y > 0 && sp.y < Screen.height;
        if (onScreen)
        {
            if (arrow.gameObject.activeSelf) arrow.gameObject.SetActive(false);
            return;
        }

        Vector2 center = new Vector2(Screen.width, Screen.height) * 0.5f;
        Vector2 dir = ((Vector2)sp - center).normalized;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.up;
        float halfW = center.x - edgePadding;
        float halfH = center.y - edgePadding;
        float scale = Mathf.Min(halfW / Mathf.Max(0.0001f, Mathf.Abs(dir.x)), halfH / Mathf.Max(0.0001f, Mathf.Abs(dir.y)));
        Vector2 screenPos = center + dir * scale;

        var canvasRect = (RectTransform)canvas.transform;
        Camera uiCam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, uiCam, out var local);
        arrow.anchoredPosition = local;
        arrow.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);
        if (!arrow.gameObject.activeSelf) arrow.gameObject.SetActive(true);
    }
}
