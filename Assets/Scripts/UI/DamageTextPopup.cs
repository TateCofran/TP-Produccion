using System;
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

    public event Action<DamageTextPopup> Finished;

    private float _t;
    private TMP_Text _tmp;
    private Color _base;
    private Vector3 _start;
    private bool _baseCaptured;

    public void Play()
    {
        _t = 0f;
        _start = transform.localPosition;
        if (_tmp != null) _tmp.color = _base;
    }

    public void ConfigureWorldSpace(float worldRiseDistance)
    {
        riseDistance = worldRiseDistance;
    }

    private void Awake()
    {
        CacheText();
    }

    private void CacheText()
    {
        if (_tmp != null) return;
        _tmp = GetComponentInChildren<TMP_Text>();
        if (_tmp != null && !_baseCaptured)
        {
            _base = _tmp.color;
            _baseCaptured = true;
        }
    }

    private void OnEnable()
    {
        CacheText();
        _t = 0f;
        _start = transform.localPosition;
        if (_tmp != null) _tmp.color = _base;
    }

    private void Update()
    {
        _t += Time.deltaTime;
        float k = Mathf.Clamp01(_t / lifetime);

        float yOff = riseCurve.Evaluate(k) * riseDistance;
        transform.localPosition = _start + new Vector3(0f, yOff, 0f);

        if (_tmp != null)
        {
            float fadeT = (k <= fadeStart) ? 0f : Mathf.InverseLerp(fadeStart, 1f, k);
            float a = Mathf.Clamp01(fadeCurve.Evaluate(fadeT));
            var c = _tmp.color; c.a = a * _base.a; _tmp.color = c;
        }

        if (_t >= lifetime) Finish();
    }

    public void Finish()
    {
        if (!gameObject.activeSelf) return;
        transform.localPosition = _start;
        gameObject.SetActive(false);
        Finished?.Invoke(this);
    }

    public void SetText(string value)
    {
        CacheText();
        if (_tmp) _tmp.text = value;
    }

    public void SetStyle(Color color, float scale = 1f)
    {
        CacheText();
        _base = color;
        _baseCaptured = true;
        if (_tmp) _tmp.color = color;
        transform.localScale = Vector3.one * scale;
    }
}
