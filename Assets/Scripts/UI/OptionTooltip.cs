using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class OptionTooltip : MonoBehaviour
{
    public static OptionTooltip Instance { get; private set; }

    [Header("Refs")]
    [SerializeField] private RectTransform panel;
    [SerializeField] private TMP_Text label;

    [Header("Layout")]
    [SerializeField] private Vector2 offset = new Vector2(0f, 12f);
    [SerializeField] private Vector2 padding = new Vector2(12f, 8f);
    [SerializeField, Min(50f)] private float maxWidth = 260f;
    [SerializeField] private Color background = new Color(0f, 0f, 0f, 0.85f);
    [SerializeField, Min(8f)] private float fontSize = 18f;

    private Canvas canvas;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        EnsureBuilt();
        Hide();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public static void Show(string text, RectTransform anchor)
    {
        if (string.IsNullOrEmpty(text) || anchor == null) return;
        var tooltip = GetOrCreate(anchor);
        if (tooltip != null) tooltip.ShowInternal(text, anchor);
    }

    public static void HideTooltip()
    {
        if (Instance != null) Instance.Hide();
    }

    private static OptionTooltip GetOrCreate(RectTransform anchor)
    {
        if (Instance != null) return Instance;

        var rootCanvas = anchor.GetComponentInParent<Canvas>();
        if (rootCanvas == null) return null;
        rootCanvas = rootCanvas.rootCanvas;

        var go = new GameObject("OptionTooltip", typeof(RectTransform));
        go.transform.SetParent(rootCanvas.transform, false);
        return go.AddComponent<OptionTooltip>();
    }

    private void EnsureBuilt()
    {
        canvas = GetComponentInParent<Canvas>();
        if (canvas != null) canvas = canvas.rootCanvas;

        if (panel == null)
        {
            var panelGo = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(transform, false);
            panel = (RectTransform)panelGo.transform;
            panel.pivot = new Vector2(0.5f, 0f);
            var img = panelGo.GetComponent<Image>();
            img.color = background;
            img.raycastTarget = false;
        }

        if (label == null)
        {
            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(panel, false);
            label = textGo.AddComponent<TextMeshProUGUI>();
            label.fontSize = fontSize;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            var rt = (RectTransform)textGo.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = padding;
            rt.offsetMax = -padding;
        }

        var group = panel.GetComponent<CanvasGroup>();
        if (group == null) group = panel.gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;

        transform.SetAsLastSibling();
    }

    private void ShowInternal(string text, RectTransform anchor)
    {
        EnsureBuilt();
        label.text = text;

        Vector2 preferred = label.GetPreferredValues(text, maxWidth, 0f);
        float width = Mathf.Min(maxWidth, preferred.x) + padding.x * 2f;
        float height = label.GetPreferredValues(text, width - padding.x * 2f, 0f).y + padding.y * 2f;
        panel.sizeDelta = new Vector2(width, height);

        var corners = new Vector3[4];
        anchor.GetWorldCorners(corners);
        Vector3 topCenter = (corners[1] + corners[2]) * 0.5f;
        panel.position = topCenter;
        panel.anchoredPosition += offset;

        transform.SetAsLastSibling();
        panel.gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (panel != null) panel.gameObject.SetActive(false);
    }
}
