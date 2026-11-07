using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class AuraVisual : MonoBehaviour
{
    [SerializeField] private Color color = new Color(1f, 0.8f, 0.3f, 0.6f);
    [SerializeField, Min(8)] private int segments = 48;
    [SerializeField, Min(0.005f)] private float width = 0.05f;
    [SerializeField] private float yOffset = 0.06f;
    [SerializeField, Min(0.005f)] private float linkWidth = 0.025f;

    private LineRenderer ring;
    private readonly List<LineRenderer> links = new();
    private Material material;
    private float radius;
    private Turret turret;

    public void SetColor(Color c)
    {
        color = c;
        if (ring != null) { ring.startColor = color; ring.endColor = color; }
    }

    private void Awake()
    {
        turret = GetComponent<Turret>();
    }

    public void SetRadius(float newRadius)
    {
        if (Mathf.Approximately(radius, newRadius) && ring != null) return;
        radius = newRadius;
        EnsureRing();
        Vector3 c = transform.position;
        float y = GroundY() + yOffset;
        ring.positionCount = segments;
        for (int i = 0; i < segments; i++)
        {
            float a = i / (float)segments * Mathf.PI * 2f;
            ring.SetPosition(i, new Vector3(c.x + Mathf.Cos(a) * radius, y, c.z + Mathf.Sin(a) * radius));
        }
    }

    public void SetLinks(IReadOnlyList<Transform> targets)
    {
        bool selected = turret != null && TurretInfoPanel.Instance != null && TurretInfoPanel.Instance.Current == turret;
        int needed = selected && targets != null ? targets.Count : 0;

        while (links.Count < needed) links.Add(CreateLine("AuraLink", linkWidth));
        for (int i = 0; i < links.Count; i++)
        {
            bool on = i < needed && targets[i] != null;
            links[i].enabled = on;
            if (!on) continue;
            float y = GroundY() + 0.5f;
            links[i].SetPosition(0, new Vector3(transform.position.x, y, transform.position.z));
            links[i].SetPosition(1, new Vector3(targets[i].position.x, y, targets[i].position.z));
        }
    }

    private float GroundY()
    {
        var slot = GetComponentInParent<CellSlot>();
        return slot != null ? slot.transform.position.y : transform.position.y - 0.5f;
    }

    private void EnsureRing()
    {
        if (ring != null) return;
        ring = CreateLine("AuraRing", width);
        ring.loop = true;
    }

    private LineRenderer CreateLine(string name, float lineWidth)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.widthMultiplier = lineWidth;
        lr.positionCount = 2;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        if (material == null) material = new Material(Shader.Find("Sprites/Default")) { hideFlags = HideFlags.DontSave };
        lr.sharedMaterial = material;
        lr.startColor = color;
        lr.endColor = color;
        return lr;
    }

    private void OnDestroy()
    {
        if (material != null) Destroy(material);
    }
}
