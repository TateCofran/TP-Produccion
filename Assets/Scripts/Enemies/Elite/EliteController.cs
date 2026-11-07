using System.Collections.Generic;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class EliteController : MonoBehaviour
{
    [Header("Hasted")]
    [SerializeField] private float hastedSpeedMultiplier = 1.35f;
    [Header("Armored")]
    [SerializeField] private float armoredDefenseBonus = 3f;
    [Header("Regenerating")]
    [Tooltip("Fraction of max health healed per second.")]
    [SerializeField] private float regenPerSecond = 0.02f;
    [Header("Shielded")]
    [SerializeField] private float shieldMultiplier = 1.5f;
    [Header("Splitter")]
    [SerializeField, Min(1)] private int splitCount = 2;
    [SerializeField, Min(0f)] private float splitSpread = 0.6f;

    [Header("Visual")]
    [SerializeField, Min(0.1f)] private float auraRadius = 0.7f;
    [SerializeField] private float labelHeight = 2.3f;

    private Enemy enemy;
    private EnemyHealth health;
    private EnemyEffects effects;
    private LineRenderer aura;
    private TextMeshPro label;
    private float regenAccumulator;
    private bool subscribedDeath;

    public bool IsElite { get; private set; }
    public EliteAffix Affix { get; private set; }

    public static int EssenceMultiplier => 3;

    public string DisplayName
    {
        get
        {
            string baseName = enemy != null && enemy.Data != null && !string.IsNullOrEmpty(enemy.Data.enemyName) ? enemy.Data.enemyName : name;
            return IsElite ? $"{Affix} {baseName}" : baseName;
        }
    }

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
        health = GetComponent<EnemyHealth>();
        effects = GetComponent<EnemyEffects>();
    }

    private void OnDestroy()
    {
        if (subscribedDeath && health != null) health.OnDied -= HandleDied;
    }

    public static Color ColorFor(EliteAffix affix)
    {
        switch (affix)
        {
            case EliteAffix.Hasted: return new Color(1f, 0.9f, 0.2f, 0.9f);
            case EliteAffix.Armored: return new Color(0.7f, 0.75f, 0.85f, 0.9f);
            case EliteAffix.Regenerating: return new Color(0.3f, 1f, 0.4f, 0.9f);
            case EliteAffix.Shielded: return new Color(0.35f, 0.75f, 1f, 0.9f);
            case EliteAffix.Splitter: return new Color(0.85f, 0.35f, 1f, 0.9f);
            default: return Color.white;
        }
    }

    public void Apply(EliteAffix affix)
    {
        Clear();
        if (enemy == null) Awake();
        if (enemy == null || health == null) return;

        IsElite = true;
        Affix = affix;

        switch (affix)
        {
            case EliteAffix.Hasted:
                if (effects != null) effects.AddSpeedMultiplier(this, hastedSpeedMultiplier);
                break;
            case EliteAffix.Armored:
                health.SetDefense(health.GetDefense() + armoredDefenseBonus);
                break;
            case EliteAffix.Shielded:
                health.TryApplyShieldAuraBoost(shieldMultiplier);
                break;
            case EliteAffix.Splitter:
                if (!subscribedDeath)
                {
                    health.OnDied += HandleDied;
                    subscribedDeath = true;
                }
                break;
        }

        ShowVisual(ColorFor(affix));
        Log.Info(nameof(EliteController), $"{DisplayName} spawned");
    }

    public void Clear()
    {
        if (effects != null) effects.RemoveSpeedMultiplier(this);
        IsElite = false;
        regenAccumulator = 0f;
        if (aura != null) aura.enabled = false;
        if (label != null) label.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!IsElite) return;

        if (Affix == EliteAffix.Regenerating && health != null && !health.IsDead())
        {
            regenAccumulator += health.GetMaxHealth() * regenPerSecond * Time.deltaTime;
            if (regenAccumulator >= 1f)
            {
                float amount = Mathf.Floor(regenAccumulator);
                regenAccumulator -= amount;
                if (health.GetCurrentHealth() < health.GetMaxHealth()) health.Heal(amount);
            }
        }

        if (label != null && label.gameObject.activeSelf)
        {
            var cam = Camera.main;
            if (cam != null) label.transform.rotation = cam.transform.rotation;
        }
    }

    private void HandleDied()
    {
        if (!IsElite || Affix != EliteAffix.Splitter || enemy == null || enemy.ReachedCore) return;
        if (EnemyPool.Instance == null || enemy.Data == null) return;

        EnemyType minionType = MinionTypeForZone(enemy.Data.type);
        Vector3 deathPos = transform.position;
        float step = Mathf.PI * 2f / splitCount;
        for (int i = 0; i < splitCount; i++)
        {
            Vector3 pos = deathPos + new Vector3(Mathf.Cos(step * i), 0f, Mathf.Sin(step * i)) * splitSpread;
            var go = EnemyPool.Instance.GetEnemy(minionType);
            if (go == null) continue;
            var child = go.GetComponent<Enemy>();
            if (child == null) continue;
            var route = new List<Vector3> { pos };
            var path = enemy.RemainingRoute();
            route.AddRange(path);
            child.Init(route);
            child.MarkAsSummoned();
            WaveManager.Instance?.RegisterExtraEnemies(1);
        }
    }

    public static EnemyType MinionTypeForZone(EnemyType type)
    {
        string n = type.ToString();
        if (n.EndsWith("3")) return EnemyType.Minion3;
        if (n.EndsWith("2")) return EnemyType.Minion2;
        return EnemyType.Minion;
    }

    private void ShowVisual(Color color)
    {
        if (aura == null)
        {
            var go = new GameObject("EliteAura");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            aura = go.AddComponent<LineRenderer>();
            aura.useWorldSpace = false;
            aura.loop = true;
            aura.positionCount = 32;
            aura.widthMultiplier = 0.08f;
            aura.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            aura.sharedMaterial = new Material(Shader.Find("Sprites/Default")) { hideFlags = HideFlags.DontSave };
            for (int i = 0; i < 32; i++)
            {
                float a = i / 32f * Mathf.PI * 2f;
                aura.SetPosition(i, new Vector3(Mathf.Cos(a) * auraRadius, 0f, Mathf.Sin(a) * auraRadius));
            }
        }
        aura.startColor = color;
        aura.endColor = color;
        aura.enabled = true;

        if (label == null)
        {
            var go = new GameObject("EliteLabel");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, labelHeight, 0f);
            label = go.AddComponent<TextMeshPro>();
            label.fontSize = 3f;
            label.alignment = TextAlignmentOptions.Center;
            label.fontStyle = FontStyles.Bold;
            label.outlineWidth = 0.2f;
            label.rectTransform.sizeDelta = new Vector2(6f, 1f);
        }
        label.text = DisplayName;
        label.color = color;
        label.gameObject.SetActive(true);
    }
}
