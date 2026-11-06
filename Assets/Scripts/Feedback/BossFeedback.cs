using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class BossFeedback : MonoBehaviour
{
    public static BossFeedback Instance { get; private set; }

    [Header("Refs")]
    [Tooltip("HUD canvas. The banner and the health bar are built inside it when not assigned.")]
    [SerializeField] private Canvas hudCanvas;
    [SerializeField] private Camera cam;
    [SerializeField] private TMP_FontAsset font;

    [Header("Banner")]
    [SerializeField] private TextMeshProUGUI bannerText;
    [SerializeField, Min(0.1f)] private float bannerDuration = 2f;
    [SerializeField] private Color bannerColor = new Color(1f, 0.3f, 0.25f, 1f);

    [Header("Camera Pan")]
    [SerializeField] private bool panToBoss = true;
    [SerializeField, Min(0.05f)] private float panInDuration = 0.5f;
    [SerializeField, Min(0f)] private float panHoldDuration = 0.7f;
    [SerializeField, Min(0.05f)] private float panOutDuration = 0.5f;

    [Header("Health Bar")]
    [SerializeField] private RectTransform healthBarRoot;
    [SerializeField] private Image healthBarFill;
    [SerializeField] private TextMeshProUGUI healthBarName;
    [SerializeField] private Color healthBarColor = new Color(0.85f, 0.15f, 0.15f, 1f);

    [Header("Death")]
    [SerializeField, Range(0f, 1f)] private float deathTrauma = 0.9f;

    private Enemy currentBoss;
    private Coroutine bannerRoutine;
    private Coroutine panRoutine;

    public Enemy CurrentBoss => currentBoss;
    public bool BannerVisible => bannerText != null && bannerText.gameObject.activeSelf;
    public bool HealthBarVisible => healthBarRoot != null && healthBarRoot.gameObject.activeSelf;
    public float HealthBarFill => healthBarFill != null ? healthBarFill.fillAmount : 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        BuildUI();
        if (bannerText != null) bannerText.gameObject.SetActive(false);
        if (healthBarRoot != null) healthBarRoot.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        Enemy.OnAnyEnemySpawned += HandleSpawned;
        Enemy.OnAnyEnemyKilled += HandleKilled;
    }

    private void OnDisable()
    {
        Enemy.OnAnyEnemySpawned -= HandleSpawned;
        Enemy.OnAnyEnemyKilled -= HandleKilled;
        Unbind();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void HandleSpawned(Enemy enemy)
    {
        if (enemy == null || !enemy.IsBoss) return;

        Bind(enemy);

        string bossName = enemy.Data != null && !string.IsNullOrEmpty(enemy.Data.enemyName) ? enemy.Data.enemyName : enemy.name;
        if (bannerRoutine != null) StopCoroutine(bannerRoutine);
        bannerRoutine = StartCoroutine(BannerRoutine($"BOSS: {bossName}"));

        if (panToBoss)
        {
            if (panRoutine != null) StopCoroutine(panRoutine);
            panRoutine = StartCoroutine(PanRoutine(enemy.transform));
        }

        Log.Info(nameof(BossFeedback), $"Boss spawned: {bossName}");
    }

    private void HandleKilled(Enemy enemy)
    {
        if (enemy == null || enemy != currentBoss) return;

        Unbind();
        if (!enemy.ReachedCore)
        {
            var settings = FeedbackSettings.Current;
            SlowMotion.Play(settings.bossDeathSlowScale, settings.bossDeathSlowDuration);
            CameraShake.AddTrauma(deathTrauma);
        }
    }

    private void Bind(Enemy enemy)
    {
        Unbind();
        currentBoss = enemy;
        if (enemy.Health != null) enemy.Health.OnDamaged += HandleBossDamaged;

        if (healthBarRoot != null) healthBarRoot.gameObject.SetActive(true);
        if (healthBarName != null)
            healthBarName.text = enemy.Data != null && !string.IsNullOrEmpty(enemy.Data.enemyName) ? enemy.Data.enemyName : enemy.name;
        if (healthBarFill != null) healthBarFill.fillAmount = 1f;
    }

    private void Unbind()
    {
        if (currentBoss != null && currentBoss.Health != null) currentBoss.Health.OnDamaged -= HandleBossDamaged;
        currentBoss = null;
        if (healthBarRoot != null) healthBarRoot.gameObject.SetActive(false);
    }

    private void HandleBossDamaged(float current, float max)
    {
        if (healthBarFill != null) healthBarFill.fillAmount = max > 0f ? Mathf.Clamp01(current / max) : 0f;
    }

    private IEnumerator BannerRoutine(string text)
    {
        if (bannerText == null) yield break;

        bannerText.text = text;
        bannerText.gameObject.SetActive(true);
        var rt = bannerText.rectTransform;

        float t = 0f;
        while (t < bannerDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = t / bannerDuration;
            float alpha = k < 0.15f ? k / 0.15f : (k > 0.8f ? (1f - k) / 0.2f : 1f);
            float scale = k < 0.15f ? Mathf.Lerp(1.4f, 1f, k / 0.15f) : 1f;
            var c = bannerColor;
            c.a = Mathf.Clamp01(alpha);
            bannerText.color = c;
            rt.localScale = Vector3.one * scale;
            yield return null;
        }

        bannerText.gameObject.SetActive(false);
        bannerRoutine = null;
    }

    private IEnumerator PanRoutine(Transform boss)
    {
        var c = cam != null ? cam : Camera.main;
        if (c == null || boss == null) yield break;

        var camT = c.transform;
        Vector3 start = camT.position;
        Vector3 target = start + GroundOffsetTo(c, boss.position);

        yield return Move(camT, start, target, panInDuration);

        float hold = 0f;
        while (hold < panHoldDuration)
        {
            hold += Time.unscaledDeltaTime;
            yield return null;
        }

        yield return Move(camT, camT.position, start, panOutDuration);
        panRoutine = null;
    }

    private static Vector3 GroundOffsetTo(Camera c, Vector3 worldTarget)
    {
        var ray = c.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        var plane = new Plane(Vector3.up, new Vector3(0f, worldTarget.y, 0f));
        if (!plane.Raycast(ray, out float enter)) return Vector3.zero;
        Vector3 center = ray.GetPoint(enter);
        Vector3 offset = worldTarget - center;
        offset.y = 0f;
        return offset;
    }

    private static IEnumerator Move(Transform t, Vector3 from, Vector3 to, float duration)
    {
        float e = 0f;
        while (e < duration)
        {
            e += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(e / duration));
            t.position = Vector3.Lerp(from, to, k);
            yield return null;
        }
        t.position = to;
    }

    private void BuildUI()
    {
        if (hudCanvas == null)
        {
            var ui = FindFirstObjectByType<UIController>();
            if (ui != null) hudCanvas = ui.GetComponentInParent<Canvas>();
            if (hudCanvas == null) hudCanvas = FindFirstObjectByType<Canvas>();
        }
        if (hudCanvas == null) return;
        var root = hudCanvas.rootCanvas.transform;

        if (bannerText == null)
        {
            var go = new GameObject("BossBanner", typeof(RectTransform));
            go.transform.SetParent(root, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.68f);
            rt.sizeDelta = new Vector2(900f, 90f);
            bannerText = go.AddComponent<TextMeshProUGUI>();
            if (font != null) bannerText.font = font;
            bannerText.fontSize = 56f;
            bannerText.fontStyle = FontStyles.Bold;
            bannerText.alignment = TextAlignmentOptions.Center;
            bannerText.raycastTarget = false;
            bannerText.outlineWidth = 0.25f;
        }

        if (healthBarRoot == null)
        {
            var bg = new GameObject("BossHealthBar", typeof(RectTransform), typeof(Image));
            healthBarRoot = (RectTransform)bg.transform;
            healthBarRoot.SetParent(root, false);
            healthBarRoot.anchorMin = healthBarRoot.anchorMax = new Vector2(0.5f, 1f);
            healthBarRoot.pivot = new Vector2(0.5f, 1f);
            healthBarRoot.sizeDelta = new Vector2(620f, 26f);
            healthBarRoot.anchoredPosition = new Vector2(0f, -112f);
            var bgImg = bg.GetComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.7f);
            bgImg.raycastTarget = false;

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            var fillRt = (RectTransform)fillGo.transform;
            fillRt.SetParent(healthBarRoot, false);
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = new Vector2(3f, 3f);
            fillRt.offsetMax = new Vector2(-3f, -3f);
            healthBarFill = fillGo.GetComponent<Image>();
            healthBarFill.sprite = FeedbackSprites.White;
            healthBarFill.type = Image.Type.Filled;
            healthBarFill.fillMethod = Image.FillMethod.Horizontal;
            healthBarFill.color = healthBarColor;
            healthBarFill.raycastTarget = false;

            var nameGo = new GameObject("Name", typeof(RectTransform));
            var nameRt = (RectTransform)nameGo.transform;
            nameRt.SetParent(healthBarRoot, false);
            nameRt.anchorMin = Vector2.zero;
            nameRt.anchorMax = Vector2.one;
            nameRt.offsetMin = Vector2.zero;
            nameRt.offsetMax = Vector2.zero;
            healthBarName = nameGo.AddComponent<TextMeshProUGUI>();
            if (font != null) healthBarName.font = font;
            healthBarName.fontSize = 18f;
            healthBarName.alignment = TextAlignmentOptions.Center;
            healthBarName.raycastTarget = false;
        }
    }
}
