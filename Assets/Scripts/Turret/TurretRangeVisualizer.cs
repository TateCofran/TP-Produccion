using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[DisallowMultipleComponent]
public sealed class TurretRangeVisualizer : MonoBehaviour, IRangeDisplay
{
    private static readonly HashSet<TurretRangeVisualizer> s_instances = new();
    private static readonly HashSet<TurretRangeVisualizer> s_visible = new();
    private static bool s_bulkHideInProgress = false;

    [Header("Refs")]
    [SerializeField] private LineRenderer lineRenderer;

    [Header("Circle Quality")]
    [Tooltip("Approximate length of each chord. Larger radii use more segments automatically.")]
    [SerializeField] private float targetChordLength = 0.15f;
    [SerializeField] private int minSegments = 24;
    [SerializeField] private int maxSegments = 256;

    [Header("Circle Height")]
    [SerializeField] private float yOffset = 0.02f;

    [Header("Options")]
    [Tooltip("If enabled, redraws even when the radius did not change (useful when re-enabled).")]
    [SerializeField] private bool alwaysRedrawOnShow = true;

    private float _currentRadius = -1f;
    private int _currentSegments = -1;
    private bool _visible;
    private bool _animating;
    private bool _playedOnce;
    private Coroutine _animRoutine;

    private Transform _tr;
    private Vector3 _lastCenter;

    [Header("Show Animation")]
    [SerializeField] private float showDuration = 0.25f;
    [SerializeField] private float widthMultiplierTarget = 0.045f;

    private void OnEnable()
    {
        s_instances.Add(this);
    }

    private void OnDisable()
    {
        s_instances.Remove(this);
        s_visible.Remove(this);

        if (_animRoutine != null)
        {
            StopCoroutine(_animRoutine);
            _animRoutine = null;
        }
        _animating = false;
    }

    private bool _initialized;

    private void Awake()
    {
        EnsureInitialized();
    }

    private void EnsureInitialized()
    {
        if (_initialized) return;
        _initialized = true;

        _tr = transform;

        if (!lineRenderer)
            lineRenderer = GetComponent<LineRenderer>();

        if (lineRenderer)
        {
            lineRenderer.loop = true;
            lineRenderer.useWorldSpace = true;
            lineRenderer.enabled = false;
            lineRenderer.widthMultiplier = 0f;

            lineRenderer.numCornerVertices = 2;
            lineRenderer.numCapVertices = 2;
        }

        _lastCenter = Center();
    }

    private void LateUpdate()
    {
        if (!_visible || !lineRenderer) return;

        Vector3 center = Center();
        Vector3 delta = center - _lastCenter;
        if (delta.sqrMagnitude > 0f)
        {
            int count = lineRenderer.positionCount;
            for (int i = 0; i < count; i++)
            {
                Vector3 pos = lineRenderer.GetPosition(i);

                lineRenderer.SetPosition(i, new Vector3(pos.x + delta.x, center.y, pos.z + delta.z));
            }
            _lastCenter = center;
        }
    }

    public void Show(float radius)
    {
        EnsureInitialized();
        if (!lineRenderer) return;

        _visible = true;
        s_visible.Add(this);

        SetRadius(radius, true);
        lineRenderer.enabled = true;

        if (!_playedOnce)
        {
            _playedOnce = true;
            if (_animRoutine != null) StopCoroutine(_animRoutine);
            _animRoutine = StartCoroutine(PlayShowAnimation());
        }
    }

    public void Hide()
    {
        HideInternal(alsoHideOthers: true);
    }

    public bool IsVisible() => _visible;

    public void ResetAnimationForPlacement()
    {
        _playedOnce = false;

        if (_animRoutine != null)
        {
            StopCoroutine(_animRoutine);
            _animRoutine = null;
        }

        _animating = false;
        if (lineRenderer) lineRenderer.widthMultiplier = 0f;
    }

    public void SetRadius(float radius, bool forceRedraw = false)
    {
        EnsureInitialized();
        if (!lineRenderer) return;

        radius = Mathf.Max(0.0001f, radius);
        int desiredSegments = ComputeSegments(radius);

        bool segmentsChanged = desiredSegments != _currentSegments;
        bool radiusChanged = !Mathf.Approximately(radius, _currentRadius);

        if (segmentsChanged || radiusChanged || (forceRedraw && alwaysRedrawOnShow))
        {
            _currentRadius = radius;
            _currentSegments = desiredSegments;

            RebuildCircle(_currentRadius, _currentSegments);
            _lastCenter = Center();
        }
    }

    private void HideInternal(bool alsoHideOthers)
    {
        EnsureInitialized();
        if (!lineRenderer) return;

        _visible = false;
        s_visible.Remove(this);
        lineRenderer.enabled = false;

        if (!alsoHideOthers) return;

        if (s_bulkHideInProgress) return;

        s_bulkHideInProgress = true;
        try
        {
            var others = ListPool<TurretRangeVisualizer>.Get();
            foreach (var tv in s_visible) others.Add(tv);

            for (int i = 0; i < others.Count; i++)
            {
                var other = others[i];
                if (other != null && other._visible)
                    other.HideInternal(alsoHideOthers: false);
            }

            ListPool<TurretRangeVisualizer>.Release(others);
        }
        finally
        {
            s_bulkHideInProgress = false;
        }
    }

    private Vector3 Center()
    {
        if (_tr == null) _tr = transform;
        var p = _tr.position;
        p.y += yOffset;
        return p;
    }

    private int ComputeSegments(float radius)
    {
        float circumference = 2f * Mathf.PI * radius;
        int seg = Mathf.CeilToInt(circumference / Mathf.Max(0.0001f, targetChordLength));
        return Mathf.Clamp(seg, minSegments, maxSegments);
    }

    private void RebuildCircle(float radius, int segments)
    {
        if (!lineRenderer) return;
        if (segments < 3) segments = 3;

        lineRenderer.positionCount = segments;

        float angleStep = (Mathf.PI * 2f) / segments;
        Vector3 c = Center();

        for (int i = 0; i < segments; i++)
        {
            float angle = i * angleStep;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            lineRenderer.SetPosition(i, new Vector3(c.x + x, c.y, c.z + z));
        }
    }

    private IEnumerator PlayShowAnimation()
    {
        if (!lineRenderer) yield break;

        _animating = true;
        lineRenderer.widthMultiplier = 0f;

        float t = 0f;
        while (t < showDuration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / showDuration);
            float eased = 1f - Mathf.Pow(1f - u, 3f);
            lineRenderer.widthMultiplier = Mathf.Lerp(0f, widthMultiplierTarget, eased);
            yield return null;
        }

        lineRenderer.widthMultiplier = widthMultiplierTarget;
        _animating = false;
        _animRoutine = null;
    }

    private static class ListPool<T>
    {
        private static readonly Stack<List<T>> _pool = new();
        public static List<T> Get() => _pool.Count > 0 ? _pool.Pop() : new List<T>(8);
        public static void Release(List<T> list)
        {
            list.Clear();
            _pool.Push(list);
        }
    }
}
