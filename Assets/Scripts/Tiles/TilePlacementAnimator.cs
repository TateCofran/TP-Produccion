using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class TilePlacementAnimator : MonoBehaviour
{
    [Header("Playback")]
    [SerializeField] private bool autoPlayOnEnable = false;
    [SerializeField, Min(0.05f)] private float duration = 0.35f;
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Drop And Bounce")]
    [SerializeField, Min(0f)] private float dropHeight = 1.2f;
    [Tooltip("Fraction of the duration spent falling. The rest is the bounce.")]
    [SerializeField, Range(0.1f, 0.95f)] private float fallPortion = 0.65f;
    [SerializeField, Min(0f)] private float bounceHeight = 0.12f;

    private Vector3 _initialScale;
    private Vector3 _restPosition;
    private Coroutine _running;
    private GameObject _dustPrefab;
    private float _dustLifetime;

    public bool IsPlaying => _running != null;

    private void Awake()
    {
        _initialScale = transform.localScale;
    }

    private void OnEnable()
    {
        if (autoPlayOnEnable)
            Play(0f);
    }

    private void OnDisable()
    {
        if (_running != null)
        {
            StopCoroutine(_running);
            _running = null;
            transform.localPosition = _restPosition;
            transform.localScale = _initialScale == Vector3.zero ? Vector3.one : _initialScale;
        }
    }

    public void Play(float delaySeconds = 0f)
    {
        Play(delaySeconds, null, 0f);
    }

    public void Play(float delaySeconds, GameObject dustPrefab, float dustLifetime)
    {
        if (!isActiveAndEnabled) return;
        if (_running != null)
        {
            StopCoroutine(_running);
            transform.localPosition = _restPosition;
        }
        _restPosition = transform.localPosition;
        _dustPrefab = dustPrefab;
        _dustLifetime = dustLifetime;
        _running = StartCoroutine(Co_Play(delaySeconds));
    }

    private IEnumerator Co_Play(float delay)
    {
        var targetScale = _initialScale == Vector3.zero ? Vector3.one : _initialScale;
        transform.localScale = Vector3.zero;
        transform.localPosition = _restPosition + Vector3.up * dropHeight;

        if (delay > 0f) yield return new WaitForSeconds(delay);

        float fallTime = duration * fallPortion;
        float bounceTime = Mathf.Max(0.01f, duration - fallTime);
        bool landed = false;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / duration);
            transform.localScale = targetScale * scaleCurve.Evaluate(Mathf.Clamp01(t / fallTime));

            float y;
            if (t < fallTime)
            {
                float f = t / fallTime;
                y = dropHeight * (1f - f * f);
            }
            else
            {
                if (!landed)
                {
                    landed = true;
                    SpawnDust();
                }
                float b = Mathf.Clamp01((t - fallTime) / bounceTime);
                y = Mathf.Sin(b * Mathf.PI) * bounceHeight * (1f - b * 0.5f);
            }

            transform.localPosition = _restPosition + Vector3.up * y;
            if (n >= 1f) break;
            yield return null;
        }

        if (!landed) SpawnDust();
        transform.localScale = targetScale;
        transform.localPosition = _restPosition;
        _running = null;
    }

    private void SpawnDust()
    {
        if (_dustPrefab == null) return;
        VfxPool.Spawn(_dustPrefab, transform.position, Quaternion.identity, _dustLifetime > 0f ? _dustLifetime : 1f);
    }
}
