using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ModifierCardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Refs")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Button button;

    [Header("Entry (unscaled)")]
    [SerializeField, Min(0f)] private float entryDelayPerCard = 0.08f;
    [SerializeField, Min(0.01f)] private float entryDuration = 0.2f;
    [SerializeField, Range(0.1f, 1f)] private float entryStartScale = 0.8f;

    [Header("Hover")]
    [SerializeField, Min(1f)] private float hoverScale = 1.05f;
    [SerializeField, Min(0.01f)] private float hoverSpeed = 14f;
    [SerializeField] private Color normalGlow = new Color(0.35f, 0.65f, 1f, 0.9f);
    [SerializeField] private Color otherGlow = new Color(1f, 0.35f, 0.3f, 0.9f);
    [SerializeField] private Vector2 glowDistance = new Vector2(6f, 6f);

    [Header("Confirm")]
    [SerializeField, Min(1f)] private float chosenScale = 1.15f;

    private CanvasGroup group;
    private Outline glow;
    private Vector3 baseScale = Vector3.one;
    private float targetScale = 1f;
    private float currentScale = 1f;
    private bool interactive;
    private bool baseCaptured;
    private bool entryPending;
    private float pendingDelay;

    public bool Hovered { get; private set; }
    public float CurrentScale => currentScale;
    public float Alpha => group != null ? group.alpha : 1f;

    private void EnsureComponents()
    {
        if (!baseCaptured)
        {
            baseScale = transform.localScale;
            baseCaptured = true;
        }
        if (group == null) group = GetComponent<CanvasGroup>();
        if (group == null) group = gameObject.AddComponent<CanvasGroup>();
        if (glow == null)
        {
            var graphic = GetComponent<Graphic>();
            if (graphic != null)
            {
                glow = GetComponent<Outline>();
                if (glow == null) glow = gameObject.AddComponent<Outline>();
                glow.effectDistance = glowDistance;
                glow.enabled = false;
            }
        }
    }

    public void Bind(ModifierSO mod, Action onClick)
    {
        Bind(mod, onClick, 0);
    }

    public void Bind(ModifierSO mod, Action onClick, int index)
    {
        if (button == null) button = GetComponentInChildren<Button>(true);
        EnsureComponents();

        if (nameText) nameText.text = mod != null ? mod.displayName : string.Empty;
        if (descriptionText) descriptionText.text = mod != null ? mod.description : string.Empty;

        if (iconImage)
        {
            iconImage.sprite = mod != null ? mod.icon : null;
            iconImage.enabled = iconImage.sprite != null;
        }

        if (glow != null)
        {
            bool other = mod != null && (mod.Category == ModifierCategory.OtherWorld || mod.Category == ModifierCategory.LegendaryOther);
            glow.effectColor = other ? otherGlow : normalGlow;
        }

        if (button)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => { if (interactive) onClick?.Invoke(); });
            button.interactable = true;
        }
        else
        {
            Debug.LogWarning("[ModifierCardUI] No Button found on the card");
        }

        StopAllCoroutines();
        pendingDelay = index * entryDelayPerCard;
        if (isActiveAndEnabled)
        {
            entryPending = false;
            StartCoroutine(EntryRoutine(pendingDelay));
        }
        else
        {
            entryPending = true;
            SetScale(entryStartScale);
            group.alpha = 0f;
        }
    }

    private void OnEnable()
    {
        if (!entryPending) return;
        entryPending = false;
        StartCoroutine(EntryRoutine(pendingDelay));
    }

    private void OnDisable()
    {
        if (!interactive && group != null && group.alpha < 1f) entryPending = true;
    }

    private IEnumerator EntryRoutine(float delay)
    {
        interactive = false;
        targetScale = 1f;
        SetScale(entryStartScale);
        group.alpha = 0f;

        float wait = 0f;
        while (wait < delay)
        {
            wait += Time.unscaledDeltaTime;
            yield return null;
        }

        float t = 0f;
        while (t < entryDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / entryDuration);
            float eased = 1f - (1f - k) * (1f - k);
            SetScale(Mathf.Lerp(entryStartScale, 1f, eased));
            group.alpha = eased;
            yield return null;
        }

        SetScale(1f);
        group.alpha = 1f;
        interactive = true;
    }

    private void Update()
    {
        if (!interactive) return;
        float next = Mathf.MoveTowards(currentScale, targetScale, hoverSpeed * Time.unscaledDeltaTime * Mathf.Max(0.01f, Mathf.Abs(targetScale - currentScale)) + 0.0005f);
        if (!Mathf.Approximately(next, currentScale)) SetScale(next);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Hovered = true;
        if (!interactive) return;
        targetScale = hoverScale;
        if (glow != null) glow.enabled = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Hovered = false;
        targetScale = 1f;
        if (glow != null) glow.enabled = false;
    }

    public IEnumerator PlayConfirm(bool chosen, float duration)
    {
        EnsureComponents();
        interactive = false;
        if (button) button.interactable = false;
        float startScale = currentScale;
        float startAlpha = group.alpha;
        if (chosen && glow != null) glow.enabled = true;

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            if (chosen) SetScale(Mathf.Lerp(startScale, chosenScale, 1f - (1f - k) * (1f - k)));
            else group.alpha = Mathf.Lerp(startAlpha, 0f, k);
            yield return null;
        }
    }

    private void SetScale(float s)
    {
        currentScale = s;
        transform.localScale = baseScale * s;
    }
}
