using System.Collections;
using TMPro;
using UnityEngine;

public class EssencePopupUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TextMeshProUGUI counterLabel;
    [SerializeField] private TextMeshProUGUI popupLabel;

    [Header("Text")]
    [SerializeField] private string normalName = "Normal Essences";
    [SerializeField] private string otherName = "Other Essences";

    [Header("Colors")]
    [SerializeField] private Color normalCounterColor = new Color(255,188,73);
    [SerializeField] private Color normalPopupColor = Color.blue;
    [SerializeField] private Color otherCounterColor = new Color(255, 188, 73);
    [SerializeField] private Color otherPopupColor = Color.red;

    [Header("Popup Animation")]
    [SerializeField] private float baseScale = 1f;
    [SerializeField] private float popScale = 1.2f;
    [SerializeField] private float popInTime = 0.10f;
    [SerializeField] private float holdTime = 0.80f;
    [SerializeField] private float outTime = 0.20f;
    [SerializeField] private float risePixels = 16f;

    [Header("Batching")]
    [Tooltip("Unscaled seconds during which gained essences are accumulated into a single popup.")]
    [Min(0f)][SerializeField] private float batchWindow = 0.5f;

    [Header("State")]
    [SerializeField] private int totalBlue = 0;
    [SerializeField] private int totalRed = 0;
    [SerializeField] private bool showCounterAtZero = true;

    private Coroutine _popupRoutine;
    private RectTransform _popupRT;
    private Vector2 _popupPos0;
    private Vector3 _popupScale0;
    private Color _popupHidden;
    private Color _popupVisible;
    private bool showingNormalWorld = true;

    private int inFlightBlue;
    private int inFlightRed;
    private Coroutine punchRoutine;
    private Vector3 counterScale0 = Vector3.one;

    private int _pendingDelta;
    private int _pendingTotal;
    private float _pendingFlushAt;
    private bool _hasPending;

    private void Awake()
    {
        if (popupLabel != null)
        {
            _popupRT = popupLabel.rectTransform;
            _popupPos0 = _popupRT.anchoredPosition;
        }

        _popupScale0 = Vector3.one * baseScale;
        SetWorld(true);

        UpdateCounterText();
        popupLabel.color = _popupHidden;
        transform.localScale = _popupScale0;
        if (counterLabel != null) counterScale0 = counterLabel.rectTransform.localScale;
    }

    private void Update()
    {
        if (_hasPending && Time.unscaledTime >= _pendingFlushAt)
            FlushPending();
    }

    private void OnDisable()
    {
        _hasPending = false;
        _pendingDelta = 0;
    }

    public void QueueEssence(int delta, int newTotal)
    {
        if (delta <= 0) return;

        if (batchWindow <= 0f)
        {
            HandleEssence(delta, newTotal);
            return;
        }

        if (!_hasPending)
        {
            _hasPending = true;
            _pendingDelta = 0;
            _pendingFlushAt = Time.unscaledTime + batchWindow;
        }

        _pendingDelta += delta;
        _pendingTotal = newTotal;
    }

    public void SetTotals(int blue, int red)
    {
        totalBlue = Mathf.Max(0, blue - inFlightBlue);
        totalRed = Mathf.Max(0, red - inFlightRed);
        UpdateCounterText();
    }

    public void ResetTotals()
    {
        _hasPending = false;
        _pendingDelta = 0;
        totalBlue = 0;
        totalRed = 0;
        inFlightBlue = 0;
        inFlightRed = 0;
        UpdateCounterText();
    }

    public void RegisterInFlight(int amount, bool blue)
    {
        if (amount <= 0) return;
        if (blue) inFlightBlue += amount;
        else inFlightRed += amount;
    }

    public void AddArrived(int amount, bool blue)
    {
        if (amount <= 0) return;

        if (blue)
        {
            inFlightBlue = Mathf.Max(0, inFlightBlue - amount);
            totalBlue += amount;
        }
        else
        {
            inFlightRed = Mathf.Max(0, inFlightRed - amount);
            totalRed += amount;
        }

        UpdateCounterText();
        if (blue == showingNormalWorld) PunchCounter();
    }

    private void PunchCounter()
    {
        if (counterLabel == null || !isActiveAndEnabled) return;
        if (punchRoutine != null) StopCoroutine(punchRoutine);
        punchRoutine = StartCoroutine(PunchRoutine());
    }

    private IEnumerator PunchRoutine()
    {
        var settings = FeedbackSettings.Current;
        var rt = counterLabel.rectTransform;
        float duration = settings.counterPunchDuration;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            float s = k < 0.5f ? Mathf.Lerp(1f, settings.counterPunchScale, k * 2f) : Mathf.Lerp(settings.counterPunchScale, 1f, (k - 0.5f) * 2f);
            rt.localScale = counterScale0 * s;
            yield return null;
        }
        rt.localScale = counterScale0;
        punchRoutine = null;
    }

    private void FlushPending()
    {
        if (!_hasPending) return;
        _hasPending = false;

        int delta = _pendingDelta;
        _pendingDelta = 0;
        if (delta > 0) HandleEssence(delta, _pendingTotal);
    }

    public void SetWorld(bool isNormalWorld)
    {
        if (isNormalWorld != showingNormalWorld)
            FlushPending();

        showingNormalWorld = isNormalWorld;

        if (counterLabel != null)
            counterLabel.color = isNormalWorld ? normalCounterColor : otherCounterColor;

        if (popupLabel != null)
        {
            _popupVisible = isNormalWorld ? normalPopupColor : otherPopupColor;
            _popupHidden = _popupVisible;
            _popupHidden.a = 0f;
        }

        UpdateCounterText();
    }

    public void HandleEssence(int delta, int newTotal)
    {
        if (showingNormalWorld)
            totalBlue = newTotal;
        else
            totalRed = newTotal;

        UpdateCounterText();

        if (popupLabel == null) return;

        string labelName = showingNormalWorld ? normalName : otherName;
        popupLabel.text = $"+{delta} {labelName}";

        if (_popupRoutine != null) StopCoroutine(_popupRoutine);
        _popupRoutine = StartCoroutine(PlayPopupAnim());
    }

    private void UpdateCounterText()
    {
        if (counterLabel == null) return;

        if (showingNormalWorld)
            counterLabel.text = $"{normalName}: {totalBlue}";
        else
            counterLabel.text = $"{otherName}: {totalRed}";

        counterLabel.gameObject.SetActive(showCounterAtZero || totalBlue > 0 || totalRed > 0);
    }

    private IEnumerator PlayPopupAnim()
    {
        transform.localScale = _popupScale0;
        _popupRT.anchoredPosition = _popupPos0;
        popupLabel.color = _popupHidden;

        float t = 0f;
        while (t < popInTime)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / popInTime);
            transform.localScale = Vector3.one * Mathf.Lerp(baseScale, popScale, k);
            popupLabel.color = Color.Lerp(_popupHidden, _popupVisible, k);
            yield return null;
        }

        yield return new WaitForSecondsRealtime(holdTime);

        float t2 = 0f;
        while (t2 < outTime)
        {
            t2 += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t2 / outTime);
            transform.localScale = Vector3.one * Mathf.Lerp(popScale, baseScale, k);
            popupLabel.color = Color.Lerp(_popupVisible, _popupHidden, k);
            yield return null;
        }

        transform.localScale = _popupScale0;
        popupLabel.color = _popupHidden;
        _popupRoutine = null;
    }
}
