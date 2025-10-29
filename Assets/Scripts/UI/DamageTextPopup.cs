using UnityEngine;
using TMPro;

[DisallowMultipleComponent]
public class DamageTextPopup : MonoBehaviour
{
    [Header("Anim")]
    [SerializeField] private float lifetime = 1f;
    [SerializeField] private float riseDistance = 30f; 
    [SerializeField] private AnimationCurve riseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float fadeStart = 0.5f;
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.Linear(0, 1, 1, 0);

    private float _t;
    private TextMeshProUGUI _tmp;
    private Color _base;
    private Vector3 _start;

    public void Play() { _t = 0f; } // trigger externo

    private void Awake()
    {
        _tmp = GetComponentInChildren<TextMeshProUGUI>();
        if (_tmp == null) _tmp = GetComponent<TextMeshProUGUI>();
        if (_tmp != null) _base = _tmp.color;
    }

    private void OnEnable()
    {
        _t = 0f;
        _start = transform.localPosition; // posición relativa al padre (barra)
        if (_tmp != null) _tmp.color = _base;
    }

    private void Update()
    {
        _t += Time.deltaTime;
        float k = Mathf.Clamp01(_t / lifetime);

        // Movimiento vertical relativo a la barra
        float yOff = riseCurve.Evaluate(k) * riseDistance;
        transform.localPosition = _start + new Vector3(0f, yOff, 0f);

        // Fade
        if (_tmp != null)
        {
            float fadeT = (k <= fadeStart) ? 0f : Mathf.InverseLerp(fadeStart, 1f, k);
            float a = Mathf.Clamp01(fadeCurve.Evaluate(fadeT));
            var c = _tmp.color; c.a = a; _tmp.color = c;
        }

        if (_t >= lifetime)
        {
            gameObject.SetActive(false);
            transform.localPosition = _start; // reinicia posición
        }
    }

    public void SetText(string value)
    {
        if (_tmp) _tmp.text = value;
    }

    public void SetStyle(Color color, float scale = 1f)
    {
        if (_tmp) _tmp.color = color;
        transform.localScale = Vector3.one * scale;
    }
}
