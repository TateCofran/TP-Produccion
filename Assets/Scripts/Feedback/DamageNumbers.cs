using System.Collections.Generic;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class DamageNumbers : MonoBehaviour
{
    public static DamageNumbers Instance { get; private set; }

    [Header("Pool")]
    [SerializeField, Min(1)] private int maxActive = 40;
    [SerializeField] private TMP_FontAsset font;

    [Header("Look")]
    [SerializeField, Min(0.1f)] private float fontSize = 4f;
    [SerializeField] private Color normalColor = new Color(1f, 0.95f, 0.85f, 1f);
    [SerializeField] private Color bigHitColor = new Color(1f, 0.55f, 0.1f, 1f);
    [SerializeField, Min(1f)] private float bigHitScale = 1.6f;
    [Tooltip("A hit counts as big when it removes more than this fraction of the max health.")]
    [SerializeField, Range(0f, 1f)] private float bigHitThreshold = 0.3f;
    [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 1.8f, 0f);
    [SerializeField, Min(0f)] private float randomSpread = 0.35f;

    private readonly List<DamageTextPopup> all = new();
    private readonly Queue<DamageTextPopup> free = new();
    private readonly List<DamageTextPopup> active = new();
    private Camera cam;

    public int ActiveCount => active.Count;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        HitFlash.OnEnemyHit += HandleHit;
    }

    private void OnDisable()
    {
        HitFlash.OnEnemyHit -= HandleHit;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void HandleHit(EnemyHealth health, float damage, Vector3 position)
    {
        if (health == null) return;
        bool big = damage > health.GetMaxHealth() * bigHitThreshold;
        Show(position, damage, big);
    }

    public void Show(Vector3 worldPosition, float damage, bool big)
    {
        int value = Mathf.Max(1, Mathf.RoundToInt(damage));
        if (active.Count >= maxActive)
        {
            var oldest = active[0];
            oldest.Finish();
        }

        var popup = Get();
        if (cam == null) cam = Camera.main;

        Vector2 spread = Random.insideUnitCircle * randomSpread;
        Vector3 pos = worldPosition + spawnOffset + new Vector3(spread.x, 0f, spread.y);
        popup.transform.position = pos;
        if (cam != null) popup.transform.rotation = cam.transform.rotation;

        popup.SetText(value.ToString());
        popup.SetStyle(big ? bigHitColor : normalColor, big ? bigHitScale : 1f);
        popup.gameObject.SetActive(true);
        popup.Play();
        active.Add(popup);
    }

    private DamageTextPopup Get()
    {
        while (free.Count > 0)
        {
            var p = free.Dequeue();
            if (p != null) return p;
        }

        var go = new GameObject("DamageNumber");
        go.transform.SetParent(transform, false);
        go.SetActive(false);
        var tmp = go.AddComponent<TextMeshPro>();
        if (font != null) tmp.font = font;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        tmp.outlineWidth = 0.2f;
        tmp.rectTransform.sizeDelta = new Vector2(4f, 1.5f);
        var popup = go.AddComponent<DamageTextPopup>();
        popup.ConfigureWorldSpace(1.2f);
        popup.Finished += HandleFinished;
        all.Add(popup);
        return popup;
    }

    private void HandleFinished(DamageTextPopup popup)
    {
        active.Remove(popup);
        free.Enqueue(popup);
    }
}
