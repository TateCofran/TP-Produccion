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
    }

    public void SetWorld(bool isNormalWorld)
    {
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
