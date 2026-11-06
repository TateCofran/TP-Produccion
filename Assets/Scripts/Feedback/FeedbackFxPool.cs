using System.Collections.Generic;
using UnityEngine;

public class FeedbackFxPool : MonoBehaviour
{
    private class Flash
    {
        public SpriteRenderer renderer;
        public float start;
        public float duration;
        public float size;
        public Color color;
        public bool active;
    }

    private class Line
    {
        public LineRenderer renderer;
        public float start;
        public float duration;
        public Vector3 from;
        public Vector3 to;
        public float jitter;
        public int segments;
        public float nextJitterAt;
        public Color color;
        public bool active;
    }

    private static FeedbackFxPool instance;

    private readonly List<Flash> flashes = new();
    private readonly List<Line> lines = new();
    private Material lineMaterial;
    private Camera cam;

    public static FeedbackFxPool Instance
    {
        get
        {
            if (instance != null) return instance;
            if (!Application.isPlaying) return null;
            instance = new GameObject("~FeedbackFxPool").AddComponent<FeedbackFxPool>();
            return instance;
        }
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    public static void MuzzleFlash(Vector3 position, Color color, float size, float duration)
    {
        var pool = Instance;
        if (pool == null) return;
        float mult = FeedbackSettings.Current.FlashMultiplier;
        if (mult <= 0f) return;
        color.a *= mult;
        pool.SpawnFlash(position, color, size, duration);
    }

    public static void Tracer(Vector3 from, Vector3 to, Color color, float width, float duration)
    {
        var pool = Instance;
        if (pool == null) return;
        pool.SpawnLine(from, to, color, width, duration, 0f, 2);
    }

    public static void Lightning(Vector3 from, Vector3 to, Color color, float width, float duration, float jitter, int segments)
    {
        var pool = Instance;
        if (pool == null) return;
        pool.SpawnLine(from, to, color, width, duration, jitter, Mathf.Max(2, segments));
    }

    private void SpawnFlash(Vector3 position, Color color, float size, float duration)
    {
        Flash flash = null;
        foreach (var f in flashes) if (!f.active) { flash = f; break; }
        if (flash == null)
        {
            var go = new GameObject("MuzzleFlash");
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = FeedbackSprites.SoftCircle;
            sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            sr.receiveShadows = false;
            flash = new Flash { renderer = sr };
            flashes.Add(flash);
        }

        flash.renderer.transform.position = position;
        flash.start = Time.time;
        flash.duration = Mathf.Max(0.01f, duration);
        flash.size = size;
        flash.color = color;
        flash.active = true;
        flash.renderer.color = color;
        flash.renderer.gameObject.SetActive(true);
    }

    private void SpawnLine(Vector3 from, Vector3 to, Color color, float width, float duration, float jitter, int segments)
    {
        Line line = null;
        foreach (var l in lines) if (!l.active) { line = l; break; }
        if (line == null)
        {
            var go = new GameObject("FeedbackLine");
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.numCapVertices = 2;
            if (lineMaterial == null)
                lineMaterial = new Material(Shader.Find("Sprites/Default")) { hideFlags = HideFlags.DontSave };
            lr.sharedMaterial = lineMaterial;
            line = new Line { renderer = lr };
            lines.Add(line);
        }

        line.from = from;
        line.to = to;
        line.jitter = jitter;
        line.segments = segments;
        line.start = Time.time;
        line.duration = Mathf.Max(0.01f, duration);
        line.color = color;
        line.nextJitterAt = 0f;
        line.active = true;
        line.renderer.widthMultiplier = width;
        line.renderer.positionCount = segments;
        line.renderer.startColor = color;
        line.renderer.endColor = color;
        BuildLine(line);
        line.renderer.gameObject.SetActive(true);
    }

    private void BuildLine(Line line)
    {
        Vector3 dir = line.to - line.from;
        Vector3 side = Vector3.Cross(dir.normalized, Vector3.up);
        if (side.sqrMagnitude < 0.001f) side = Vector3.right;
        side.Normalize();

        for (int i = 0; i < line.segments; i++)
        {
            float t = i / (float)(line.segments - 1);
            Vector3 p = Vector3.Lerp(line.from, line.to, t);
            if (line.jitter > 0f && i > 0 && i < line.segments - 1)
            {
                float envelope = Mathf.Sin(t * Mathf.PI);
                p += side * Random.Range(-line.jitter, line.jitter) * envelope;
                p += Vector3.up * Random.Range(-line.jitter, line.jitter) * 0.5f * envelope;
            }
            line.renderer.SetPosition(i, p);
        }
    }

    private void Update()
    {
        if (cam == null) cam = Camera.main;
        float now = Time.time;

        foreach (var f in flashes)
        {
            if (!f.active) continue;
            float k = (now - f.start) / f.duration;
            if (k >= 1f)
            {
                f.active = false;
                f.renderer.gameObject.SetActive(false);
                continue;
            }
            float s = f.size * (k < 0.3f ? Mathf.Lerp(0.6f, 1.2f, k / 0.3f) : Mathf.Lerp(1.2f, 0.4f, (k - 0.3f) / 0.7f));
            f.renderer.transform.localScale = Vector3.one * s;
            if (cam != null) f.renderer.transform.rotation = cam.transform.rotation;
            var c = f.color;
            c.a *= 1f - k;
            f.renderer.color = c;
        }

        foreach (var l in lines)
        {
            if (!l.active) continue;
            float k = (now - l.start) / l.duration;
            if (k >= 1f)
            {
                l.active = false;
                l.renderer.gameObject.SetActive(false);
                continue;
            }
            if (l.jitter > 0f && now >= l.nextJitterAt)
            {
                BuildLine(l);
                l.nextJitterAt = now + 0.03f;
            }
            var c = l.color;
            c.a *= 1f - k * k;
            l.renderer.startColor = c;
            l.renderer.endColor = c;
        }
    }
}
