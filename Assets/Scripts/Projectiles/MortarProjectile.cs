using System.Collections.Generic;
using UnityEngine;

public class MortarProjectile : MonoBehaviour
{
    private static readonly Stack<MortarProjectile> Pool = new();
    private static Transform root;
    private static Material shellMaterial;

    [SerializeField, Min(0.01f)] private float shellSize = 0.35f;
    [SerializeField] private Color markerColor = new Color(1f, 0.45f, 0.1f, 0.85f);
    [SerializeField] private Color explosionColor = new Color(1f, 0.6f, 0.2f, 1f);
    [SerializeField, Min(8)] private int markerSegments = 32;
    [SerializeField, Range(0f, 1f)] private float impactTrauma = 0.08f;

    private Vector3 start;
    private Vector3 targetGround;
    private float arcHeight;
    private float flightTime;
    private float elapsed;
    private float damage;
    private float radius;
    private bool flying;
    private LineRenderer marker;

    public static int ActiveCount { get; private set; }

    public static MortarProjectile Launch(Vector3 from, Vector3 groundTarget, float flight, float arc, float damageValue, float explosionRadius)
    {
        var shell = Get();
        shell.start = from;
        shell.targetGround = groundTarget;
        shell.flightTime = Mathf.Max(0.1f, flight);
        shell.arcHeight = arc;
        shell.damage = damageValue;
        shell.radius = Mathf.Max(0.1f, explosionRadius);
        shell.elapsed = 0f;
        shell.flying = true;
        shell.transform.position = from;
        shell.transform.localScale = Vector3.one * shell.shellSize;
        shell.gameObject.SetActive(true);
        shell.DrawMarker();
        ActiveCount++;
        return shell;
    }

    private static MortarProjectile Get()
    {
        while (Pool.Count > 0)
        {
            var p = Pool.Pop();
            if (p != null) return p;
        }

        if (root == null) root = new GameObject("~MortarShells").transform;
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "MortarShell";
        Object.Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(root, false);
        var r = go.GetComponent<Renderer>();
        if (shellMaterial == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            shellMaterial = new Material(shader != null ? shader : Shader.Find("Sprites/Default")) { hideFlags = HideFlags.DontSave };
            shellMaterial.color = new Color(0.15f, 0.12f, 0.1f, 1f);
            if (shellMaterial.HasProperty("_BaseColor")) shellMaterial.SetColor("_BaseColor", new Color(0.15f, 0.12f, 0.1f, 1f));
        }
        r.sharedMaterial = shellMaterial;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var shell = go.AddComponent<MortarProjectile>();
        go.SetActive(false);
        return shell;
    }

    private void DrawMarker()
    {
        if (marker == null)
        {
            var go = new GameObject("ImpactMarker");
            go.transform.SetParent(root, false);
            marker = go.AddComponent<LineRenderer>();
            marker.useWorldSpace = true;
            marker.loop = true;
            marker.widthMultiplier = 0.06f;
            marker.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            marker.sharedMaterial = new Material(Shader.Find("Sprites/Default")) { hideFlags = HideFlags.DontSave };
        }

        marker.positionCount = markerSegments;
        for (int i = 0; i < markerSegments; i++)
        {
            float a = i / (float)markerSegments * Mathf.PI * 2f;
            marker.SetPosition(i, targetGround + new Vector3(Mathf.Cos(a) * radius, 0.08f, Mathf.Sin(a) * radius));
        }
        marker.startColor = markerColor;
        marker.endColor = markerColor;
        marker.enabled = true;
    }

    private void Update()
    {
        if (!flying) return;

        elapsed += Time.deltaTime;
        float k = Mathf.Clamp01(elapsed / flightTime);
        Vector3 p = Vector3.Lerp(start, targetGround, k);
        p.y += 4f * arcHeight * k * (1f - k);
        transform.position = p;

        if (marker != null)
        {
            var c = markerColor;
            c.a *= 0.4f + 0.6f * k;
            marker.startColor = c;
            marker.endColor = c;
        }

        if (k >= 1f) Explode();
    }

    private void Explode()
    {
        flying = false;
        float r2 = radius * radius;
        var list = EnemyTracker.Enemies;
        for (int i = list.Count - 1; i >= 0; i--)
        {
            var e = list[i];
            if (e == null || e.Health == null || e.Health.IsDead() || !e.IsTargetable()) continue;
            Vector3 d = e.transform.position - targetGround;
            d.y = 0f;
            if (d.sqrMagnitude > r2) continue;
            e.Health.TakeDamage(TurretDamageCalculator.Compute(damage, e));
        }

        FeedbackFxPool.MuzzleFlash(targetGround + Vector3.up * 0.3f, explosionColor, radius * 1.6f, 0.25f);
        if (impactTrauma > 0f) CameraShake.AddTrauma(impactTrauma);
        Release();
    }

    private void Release()
    {
        flying = false;
        if (marker != null) marker.enabled = false;
        gameObject.SetActive(false);
        ActiveCount = Mathf.Max(0, ActiveCount - 1);
        Pool.Push(this);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Pool.Clear();
        root = null;
        ActiveCount = 0;
    }
}
