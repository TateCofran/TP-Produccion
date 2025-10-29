using UnityEngine;
using System.Collections;

public class TurretRangeVisualizer : MonoBehaviour, IRangeDisplay
{
    [Header("Refs")]
    [SerializeField] private LineRenderer lineRenderer;

    [Header("Calidad del círculo")]
    [SerializeField] private float targetChordLength = 0.15f;
    [SerializeField] private int minSegments = 24;
    [SerializeField] private int maxSegments = 256;

    [Header("Altura del círculo")]
    [SerializeField] private float yOffset = 0.02f;

    [Header("Animación al mostrar")]
    [SerializeField] private float showDuration = 0.35f;
    [SerializeField] private float widthMultiplierTarget = 0.1f;

    private float _currentRadius = -1f;
    private int _currentSegments = -1;

    private Vector3 _lastCenter;
    private bool _visible;

    private bool _animating;
    private Coroutine _animRoutine;

    // para que la animación ocurra una sola vez
    private bool _playedOnce;

    private void Awake()
    {
        if (!lineRenderer)
        {
            Debug.LogError($"[{nameof(TurretRangeVisualizer)}] LineRenderer no asignado en {name}");
            enabled = false;
            return;
        }

        lineRenderer.useWorldSpace = true;
        lineRenderer.loop = true;
        lineRenderer.enabled = false;
    }

    private void LateUpdate()
    {
        if (!_visible || !lineRenderer) return;
        if (_animating) return;

        Vector3 center = transform.position;
        if ((center - _lastCenter).sqrMagnitude > 0.0000001f)
        {
            RedrawCircleWorldSpace();
        }
    }

    // ========== IRangeDisplay ==========
    public void Show(float radius)
    {
        if (!lineRenderer) return;
        _visible = true;

        SetRadius(radius, true);
        lineRenderer.enabled = true;

        // 👇 Animar solo la primera vez que se coloca
        if (!_playedOnce)
        {
            _playedOnce = true;
            if (_animRoutine != null) StopCoroutine(_animRoutine);
            _animRoutine = StartCoroutine(PlayShowAnimation());
        }
    }

    public void Hide()
    {
        _visible = false;
        if (lineRenderer) lineRenderer.enabled = false;
    }

    public bool IsVisible()
    {
        return lineRenderer && lineRenderer.enabled;
    }

    public void SetRadius(float radius, bool forceRedraw = false)
    {
        radius = Mathf.Max(0f, radius);

        int desiredSegments = ComputeSegments(radius);
        bool radiusChanged = !Mathf.Approximately(_currentRadius, radius);
        bool segsChanged = desiredSegments != _currentSegments;

        if (!forceRedraw && !radiusChanged && !segsChanged) return;

        _currentRadius = radius;
        _currentSegments = desiredSegments;

        RedrawCircleWorldSpace();
    }

    private int ComputeSegments(float radius)
    {
        float circumference = Mathf.PI * 2f * Mathf.Max(0.01f, radius);
        int segments = Mathf.CeilToInt(circumference / Mathf.Max(0.01f, targetChordLength));
        return Mathf.Clamp(segments, minSegments, maxSegments);
    }

    private void RedrawCircleWorldSpace()
    {
        if (!lineRenderer || _currentSegments < 3) return;

        lineRenderer.positionCount = _currentSegments + 1;

        Vector3 center = transform.position;
        _lastCenter = center;

        float angleStep = 360f / _currentSegments;
        for (int i = 0; i <= _currentSegments; i++)
        {
            float angleRad = Mathf.Deg2Rad * angleStep * i;
            float x = Mathf.Cos(angleRad) * _currentRadius;
            float z = Mathf.Sin(angleRad) * _currentRadius;
            lineRenderer.SetPosition(i, new Vector3(center.x + x, center.y + yOffset, center.z + z));
        }
    }

    // ========= Anim solo 1 vez =========
    private IEnumerator PlayShowAnimation()
    {
        if (!lineRenderer || _currentSegments < 3)
            yield break;

        _animating = true;

        float dur = Mathf.Max(0.01f, showDuration);
        float t = 0f;

        // ancho inicial en 0
        lineRenderer.widthMultiplier = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            float eased = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);

            // crecer grosor
            lineRenderer.widthMultiplier = Mathf.Lerp(0f, widthMultiplierTarget, eased);

            yield return null;
        }

        lineRenderer.widthMultiplier = widthMultiplierTarget;

        _animating = false;
        _animRoutine = null;
    }
}
