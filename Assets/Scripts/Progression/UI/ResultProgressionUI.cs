using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ResultProgressionUI : MonoBehaviour
{
    [Header("Layout")]
    [SerializeField] private Vector2 anchorMin = new Vector2(0.2f, 0.57f);
    [SerializeField] private Vector2 anchorMax = new Vector2(0.8f, 0.9f);

    [SerializeField] private int sortingOrder = 50;

    [Header("Animation (unscaled)")]
    [SerializeField, Min(0.1f)] private float fillDuration = 1.5f;
    [SerializeField, Min(0f)] private float flashDuration = 0.35f;

    [Header("Audio")]
    [SerializeField] private AudioClip levelUpClip;
    [SerializeField, Range(0f, 1f)] private float volume = 0.6f;

    private RectTransform root;
    private AudioSource source;

    public int LevelUpsPlayed { get; private set; }
    public int CardsShown { get; private set; }
    public bool Finished { get; private set; }

    private void Start()
    {
        var result = ProgressionStore.LastRun;
        if (result == null) return;

        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        if (levelUpClip == null) levelUpClip = RewardClaimFX.BuildTone(988f);

        var canvasGo = new GameObject("ProgressionResultCanvas", typeof(RectTransform));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        root = UiKit.Rect(canvasGo.transform, "ProgressionResult", anchorMin, anchorMax, Vector2.zero, Vector2.zero);
        UiKit.Image(root, new Color(0f, 0f, 0f, 0.8f));

        var normal = BuildBar(new Vector2(0.02f, 0.62f), new Vector2(0.49f, 0.98f), "Normal", UiKit.NormalColor, out var normalFill, out var normalText);
        var other = BuildBar(new Vector2(0.51f, 0.62f), new Vector2(0.98f, 0.98f), "Other World", UiKit.OtherColor, out var otherFill, out var otherText);

        StartCoroutine(Animate(result, normal, normalFill, normalText, other, otherFill, otherText));
    }

    private RectTransform BuildBar(Vector2 min, Vector2 max, string label, Color color, out RectTransform fill, out TextMeshProUGUI text)
    {
        var holder = UiKit.Rect(root, label + "Bar", min, max, Vector2.zero, Vector2.zero);
        text = UiKit.Text(holder, "Label", label, 20f, TextAlignmentOptions.Top, color);
        var barArea = UiKit.Rect(holder, "BarArea", new Vector2(0f, 0f), new Vector2(1f, 0.45f), Vector2.zero, Vector2.zero);
        UiKit.Bar(barArea, "Bar", color, out fill);
        return holder;
    }

    private IEnumerator Animate(RunProgressResult result, RectTransform normal, RectTransform normalFill, TextMeshProUGUI normalText,
        RectTransform other, RectTransform otherFill, TextMeshProUGUI otherText)
    {
        int lastNormalLevel = ProgressionStore.LevelForXp(result.normalXpBefore, out _, out _);
        int lastOtherLevel = ProgressionStore.LevelForXp(result.otherXpBefore, out _, out _);

        float t = 0f;
        while (true)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / fillDuration);
            float eased = 1f - (1f - k) * (1f - k);

            lastNormalLevel = Step(result.normalXpBefore, result.normalXpAfter, eased, result.blueGained, "Normal", normal, normalFill, normalText, lastNormalLevel);
            lastOtherLevel = Step(result.otherXpBefore, result.otherXpAfter, eased, result.redGained, "Other World", other, otherFill, otherText, lastOtherLevel);

            if (k >= 1f) break;
            yield return null;
        }

        BuildUnlockCards(result);
        Finished = true;
    }

    private int Step(int before, int after, float eased, int gained, string label, RectTransform holder, RectTransform fill, TextMeshProUGUI text, int lastLevel)
    {
        int xp = Mathf.RoundToInt(Mathf.Lerp(before, after, eased));
        int level = ProgressionStore.LevelForXp(xp, out int into, out int needed);
        UiKit.SetBar(fill, needed > 0 ? (float)into / needed : 0f);
        text.text = $"{label}  +{gained} XP   Lv {level} — {into} / {needed}";
        if (level > lastLevel)
        {
            LevelUpsPlayed += level - lastLevel;
            StartCoroutine(Flash(holder));
            if (source != null && levelUpClip != null) source.PlayOneShot(levelUpClip, volume);
        }
        return level;
    }

    private IEnumerator Flash(RectTransform holder)
    {
        var flash = UiKit.Fill(holder, "Flash");
        var img = UiKit.Image(flash, new Color(1f, 1f, 1f, 0.8f));
        float t = 0f;
        while (t < flashDuration)
        {
            t += Time.unscaledDeltaTime;
            img.color = new Color(1f, 1f, 1f, 0.8f * (1f - t / flashDuration));
            yield return null;
        }
        Destroy(flash.gameObject);
    }

    private void BuildUnlockCards(RunProgressResult result)
    {
        if (result.unlocked.Count == 0) return;
        var area = UiKit.Rect(root, "Unlocks", new Vector2(0f, 0f), new Vector2(1f, 0.58f), new Vector2(10f, 6f), new Vector2(-10f, -6f));
        var layout = area.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        foreach (var item in result.unlocked)
        {
            var card = UiKit.Sized(area, "Unlock", new Vector2(120f, 130f));
            UiKit.Image(card, UiKit.CardColor);
            if (item.IsLegendary)
            {
                var frame = card.gameObject.AddComponent<Outline>();
                frame.effectColor = UiKit.Gold;
                frame.effectDistance = new Vector2(4f, 4f);
            }
            var iconArea = UiKit.Rect(card, "IconArea", new Vector2(0.15f, 0.42f), new Vector2(0.85f, 0.95f), Vector2.zero, Vector2.zero);
            if (item.kind == UnlockedItem.Kind.Modifier)
            {
                UiKit.Image(iconArea, item.IsLegendary ? UiKit.Gold : new Color(0.3f, 0.3f, 0.45f, 1f));
                UiKit.Text(iconArea, "M", item.IsLegendary ? "L" : "M", 30f, TextAlignmentOptions.Center, Color.black);
            }
            else
            {
                UiKit.Icon(iconArea, "Icon", item.tile, item.turret, false);
            }
            var name = UiKit.Text(card, "Name", item.DisplayName, 14f, TextAlignmentOptions.Center, item.IsLegendary ? UiKit.Gold : Color.white);
            name.rectTransform.anchorMin = new Vector2(0f, 0.2f);
            name.rectTransform.anchorMax = new Vector2(1f, 0.42f);
            var caption = UiKit.Text(card, "Caption", item.Caption, 12f, TextAlignmentOptions.Center, new Color(1f, 1f, 1f, 0.7f));
            caption.rectTransform.anchorMax = new Vector2(1f, 0.2f);
            CardsShown++;
        }
    }
}
