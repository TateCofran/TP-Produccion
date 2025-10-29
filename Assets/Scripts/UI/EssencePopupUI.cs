using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class EssencePopupUI : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("Texto que muestra SIEMPRE el total acumulado.")]
    [SerializeField] private TextMeshProUGUI counterLabel;

    [Tooltip("Texto del POPUP (aparece animado y luego se oculta).")]
    [SerializeField] private TextMeshProUGUI popupLabel;

    [Header("Texto")]
    [SerializeField] private string singularName = "Essences";
    [SerializeField] private Color textColor = Color.white;

    [Header("Colores")]
    [SerializeField] private Color counterColor = Color.white;
    [SerializeField] private Color popupColor = Color.white;

    [Header("Animación popup")]
    [SerializeField] private float baseScale = 1f;
    [SerializeField] private float popScale = 1.2f;
    [SerializeField] private float popInTime = 0.10f;
    [SerializeField] private float holdTime = 0.80f;
    [SerializeField] private float outTime = 0.20f;
    [SerializeField] private float risePixels = 16f;

    [Header("Estado")]
    [SerializeField] private int total = 0;
    [Tooltip("Si true, el contador arranca visible aún con total 0.")]
    [SerializeField] private bool showCounterAtZero = true;

    private Coroutine _popupRoutine;
    private RectTransform _popupRT;
    private Vector2 _popupPos0;
    private Vector3 _popupScale0;
    private Color _popupHidden;
    private Color _popupVisible;

    private void Reset()
    {
        var tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
        if (tmps.Length >= 1 && counterLabel == null) counterLabel = tmps[0];
        if (tmps.Length >= 2 && popupLabel == null) popupLabel = tmps[1];
    }

    private void Awake()
    {
        if (counterLabel != null)
        {
            counterLabel.textWrappingMode = TextWrappingModes.NoWrap;

            counterLabel.raycastTarget = false;
            counterLabel.color = counterColor;
        }

        if (popupLabel != null)
        {

            popupLabel.textWrappingMode = TextWrappingModes.NoWrap;

            popupLabel.raycastTarget = false;
            popupLabel.color = popupColor;

            _popupRT = popupLabel.rectTransform;
            _popupPos0 = _popupRT.anchoredPosition;
        }

        _popupScale0 = Vector3.one * baseScale;
        _popupVisible = popupColor;
        _popupHidden = popupColor; _popupHidden.a = 0f;

        // Inicializar UI
        UpdateCounterText();
        if (popupLabel != null)
        {
            popupLabel.color = _popupHidden; // popup oculto
            transform.localScale = _popupScale0;
        }

        // Mostrar/ocultar contador si está en 0
        if (counterLabel != null)
            counterLabel.gameObject.SetActive(showCounterAtZero || total > 0);
    }
    public void HandleEssence(int delta, int newTotal)
    {
        total = newTotal;

        // 1) Actualizar contador (persistente)
        UpdateCounterText();
        if (counterLabel != null && !counterLabel.gameObject.activeSelf)
            counterLabel.gameObject.SetActive(true);

        // 2) Disparar popup animado
        if (popupLabel == null) return;

        string nameToUse = singularName;
        popupLabel.text = $"+{delta} {nameToUse}";

        if (_popupRoutine != null) StopCoroutine(_popupRoutine);
        _popupRoutine = StartCoroutine(PlayPopupAnim());
    }

    private void UpdateCounterText()
    {
        if (counterLabel == null) return;
        // Mostrar solo el total, claro y permanente. Ej: "Azules: 7"
        counterLabel.text = singularName + ": " + total.ToString();
    }

    private IEnumerator PlayPopupAnim()
    {
        // Reset estado popup
        transform.localScale = _popupScale0;
        _popupRT.anchoredPosition = _popupPos0;
        popupLabel.color = _popupHidden;

        // POP-IN (escala + alpha + leve subida)
        float t = 0f;
        while (t < popInTime)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / popInTime);

            float s = Mathf.Lerp(baseScale, popScale, EaseOutQuad(k));
            transform.localScale = Vector3.one * s;

            popupLabel.color = ColorLerpAlpha(_popupHidden, _popupVisible, k);
            _popupRT.anchoredPosition = _popupPos0 + Vector2.up * (risePixels * 0.5f * k);
            yield return null;
        }
        transform.localScale = Vector3.one * popScale;
        popupLabel.color = _popupVisible;
        _popupRT.anchoredPosition = _popupPos0 + Vector2.up * (risePixels * 0.5f);

        // HOLD
        float h = 0f;
        while (h < holdTime)
        {
            h += Time.unscaledDeltaTime;
            yield return null;
        }

        // OUT (escala vuelve, sube y se apaga el alpha)
        float t2 = 0f;
        while (t2 < outTime)
        {
            t2 += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t2 / outTime);

            float s = Mathf.Lerp(popScale, baseScale, EaseInQuad(k));
            transform.localScale = Vector3.one * s;

            popupLabel.color = ColorLerpAlpha(_popupVisible, _popupHidden, k);
            _popupRT.anchoredPosition = Vector2.Lerp(
                _popupPos0 + Vector2.up * (risePixels * 0.5f),
                _popupPos0 + Vector2.up * risePixels,
                k
            );
            yield return null;
        }

        // Termina oculto, pero el COUNTER sigue visible
        transform.localScale = _popupScale0;
        popupLabel.color = _popupHidden;
        _popupRT.anchoredPosition = _popupPos0;

        _popupRoutine = null;
    }

    // Easing y helpers ----------------------------
    private static float EaseOutQuad(float x) => 1f - (1f - x) * (1f - x);
    private static float EaseInQuad(float x) => x * x;

    private static Color ColorLerpAlpha(Color from, Color to, float t)
    {
        var c = Color.Lerp(from, to, t);
        c.a = Mathf.Lerp(from.a, to.a, t);
        return c;
    }
}