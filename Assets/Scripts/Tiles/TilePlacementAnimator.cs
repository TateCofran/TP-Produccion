// TilePlacementAnimator.cs
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class TilePlacementAnimator : MonoBehaviour
{
    [Header("Playback")]
    [SerializeField] private bool autoPlayOnEnable = false; // ahora por defecto NO auto-play
    [SerializeField, Min(0.05f)] private float duration = 0.35f;
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Vector3 _initialScale;
    private Coroutine _running;

    private void Awake()
    {
        _initialScale = transform.localScale;
    }

    private void OnEnable()
    {
        if (autoPlayOnEnable)
            Play(0f);
    }

    /// <summary>Dispara la animación con un retraso opcional.</summary>
    public void Play(float delaySeconds = 0f)
    {
        if (!isActiveAndEnabled) return;
        if (_running != null) StopCoroutine(_running);
        _running = StartCoroutine(Co_Play(delaySeconds));
    }

    private IEnumerator Co_Play(float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        // estado inicial
        var targetScale = _initialScale == Vector3.zero ? Vector3.one : _initialScale;
        transform.localScale = Vector3.zero;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / duration);
            float s = scaleCurve.Evaluate(n);
            transform.localScale = targetScale * s;
            yield return null;
        }
        transform.localScale = targetScale;
        _running = null;
    }
}
