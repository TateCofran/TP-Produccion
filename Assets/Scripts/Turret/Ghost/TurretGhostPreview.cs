using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class TurretGhostPreview : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Camera cam;
    [SerializeField] private LayerMask cellLayers = ~0;

    [Header("Look")]
    [SerializeField] private Material ghostMaterial;
    [SerializeField] private string ghostLayerName = GhostVisuals.DefaultLayerName;
    [SerializeField] private Color freeColor = new Color(0.3f, 1f, 0.4f, 1f);
    [SerializeField] private Color occupiedColor = new Color(1f, 0.3f, 0.3f, 1f);
    [SerializeField, Range(0f, 1f)] private float alpha = 0.55f;

    [Header("Range Circle")]
    [SerializeField] private Material rangeMaterial;
    [SerializeField, Min(8)] private int rangeSegments = 64;
    [SerializeField, Min(0.005f)] private float rangeWidth = 0.06f;
    [SerializeField] private float rangeYOffset = 0.05f;

    private readonly Dictionary<GameObject, GameObject> ghostsByPrefab = new();
    private GameObject currentGhost;
    private TurretDataSO currentTurret;
    private LineRenderer rangeLine;
    private float currentRange;
    private bool active;
    private int ghostLayer;
    private CellSlot lastSlot;
    private bool lastFree;

    [Header("Synergy Preview")]
    [SerializeField] private Color synergyColor = new Color(1f, 0.85f, 0.25f, 1f);
    [SerializeField, Min(0.1f)] private float synergyLabelSize = 3.5f;

    private TMPro.TextMeshPro synergyLabel;
    private readonly System.Collections.Generic.List<LineRenderer> cellMarkers = new();

    public string LastSynergyText { get; private set; } = string.Empty;
    public int LastSynergyCells { get; private set; }

    public bool IsActive => active;

    private void Awake()
    {
        ghostLayer = GhostVisuals.ResolveLayer(ghostLayerName);
    }

    public void Configure(Camera camera, LayerMask layers)
    {
        if (camera != null) cam = camera;
        cellLayers = layers;
    }

    public void Begin(TurretDataSO turret, TurretLevelData level)
    {
        End();
        if (turret == null || turret.prefab == null) return;

        currentTurret = turret;
        currentGhost = GetOrBuild(turret.prefab);
        TurretOptionButton.ComputeStats(turret, level, out _, out currentRange, out _);
        active = currentGhost != null;
        lastSlot = null;
    }

    public void End()
    {
        active = false;
        currentTurret = null;
        lastSlot = null;
        if (currentGhost != null) currentGhost.SetActive(false);
        currentGhost = null;
        if (rangeLine != null) rangeLine.enabled = false;
        HideSynergy();
    }

    private void Update()
    {
        if (!active) return;

        var slot = RaycastSlot();
        if (slot == null)
        {
            SetVisible(false);
            return;
        }

        bool free = !slot.IsOccupied && slot.enabled;
        if (slot != lastSlot || free != lastFree || !currentGhost.activeSelf)
        {
            currentGhost.SetActive(true);
            var pose = slot.GetPlacementPose(currentGhost);
            currentGhost.transform.SetPositionAndRotation(pose.position, pose.rotation);
            GhostVisuals.SetTint(currentGhost, free ? freeColor : occupiedColor, alpha);
            DrawRange(slot.transform.position, free ? freeColor : occupiedColor);
            ShowSynergy(slot, pose.position, free);
            lastSlot = slot;
            lastFree = free;
        }
    }

    private CellSlot RaycastSlot()
    {
        var c = cam ? cam : Camera.main;
        if (c == null) return null;
        if (EventSystem.current && EventSystem.current.IsPointerOverGameObject()) return null;

        Ray ray = c.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out var hit, 500f, cellLayers)) return null;
        return hit.collider.GetComponentInParent<CellSlot>();
    }

    private void SetVisible(bool visible)
    {
        if (currentGhost != null && currentGhost.activeSelf != visible) currentGhost.SetActive(visible);
        if (rangeLine != null) rangeLine.enabled = visible;
        if (!visible)
        {
            lastSlot = null;
            HideSynergy();
        }
    }

    private void ShowSynergy(CellSlot slot, Vector3 ghostPosition, bool free)
    {
        HideSynergy();
        var service = SynergyService.Instance;
        if (!free || service == null || currentTurret == null) return;

        var rules = service.GetActiveSynergies(slot, currentTurret, out var cells);
        if (rules.Count == 0) return;

        var sb = new System.Text.StringBuilder();
        foreach (var r in rules)
        {
            if (sb.Length > 0) sb.Append('\n');
            sb.Append(r.BonusLabel);
        }
        LastSynergyText = sb.ToString();
        LastSynergyCells = cells.Count;

        if (synergyLabel == null)
        {
            var go = new GameObject("SynergyLabel");
            go.transform.SetParent(transform, false);
            synergyLabel = go.AddComponent<TMPro.TextMeshPro>();
            synergyLabel.alignment = TMPro.TextAlignmentOptions.Center;
            synergyLabel.fontStyle = TMPro.FontStyles.Bold;
            synergyLabel.outlineWidth = 0.2f;
            synergyLabel.rectTransform.sizeDelta = new Vector2(6f, 2f);
            go.layer = ghostLayer;
        }
        synergyLabel.fontSize = synergyLabelSize;
        synergyLabel.color = synergyColor;
        synergyLabel.text = "Synergy\n" + LastSynergyText;
        synergyLabel.transform.position = ghostPosition + Vector3.up * 2.2f;
        var c = cam ? cam : Camera.main;
        if (c != null) synergyLabel.transform.rotation = c.transform.rotation;
        synergyLabel.gameObject.SetActive(true);

        for (int i = 0; i < cells.Count; i++)
        {
            if (i >= cellMarkers.Count) cellMarkers.Add(CreateMarker());
            var m = cellMarkers[i];
            Vector3 p = cells[i] + Vector3.up * 0.12f;
            float h = 0.45f;
            m.SetPosition(0, p + new Vector3(-h, 0f, -h));
            m.SetPosition(1, p + new Vector3(-h, 0f, h));
            m.SetPosition(2, p + new Vector3(h, 0f, h));
            m.SetPosition(3, p + new Vector3(h, 0f, -h));
            m.enabled = true;
        }
    }

    private LineRenderer CreateMarker()
    {
        var go = new GameObject("SynergyCellMarker");
        go.transform.SetParent(transform, false);
        go.layer = ghostLayer;
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop = true;
        lr.positionCount = 4;
        lr.widthMultiplier = 0.06f;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.sharedMaterial = rangeMaterial != null ? rangeMaterial : new Material(Shader.Find("Sprites/Default")) { hideFlags = HideFlags.DontSave };
        lr.startColor = synergyColor;
        lr.endColor = synergyColor;
        return lr;
    }

    private void HideSynergy()
    {
        LastSynergyText = string.Empty;
        LastSynergyCells = 0;
        if (synergyLabel != null) synergyLabel.gameObject.SetActive(false);
        foreach (var m in cellMarkers) if (m != null) m.enabled = false;
    }

    public void PreviewAt(CellSlot slot)
    {
        if (!active || slot == null || currentGhost == null) return;
        bool free = !slot.IsOccupied && slot.enabled;
        currentGhost.SetActive(true);
        var pose = slot.GetPlacementPose(currentGhost);
        currentGhost.transform.SetPositionAndRotation(pose.position, pose.rotation);
        ShowSynergy(slot, pose.position, free);
        lastSlot = slot;
        lastFree = free;
    }

    private GameObject GetOrBuild(GameObject prefab)
    {
        if (ghostsByPrefab.TryGetValue(prefab, out var existing) && existing != null) return existing;

        var ghost = GhostVisuals.InstantiateInert(prefab, Vector3.zero, Quaternion.identity, transform);
        if (ghost == null) return null;
        ghost.name = $"TurretGhost_{prefab.name}";
        GhostVisuals.ApplyGhost(ghost, GhostVisuals.ResolveMaterial(ghostMaterial), ghostLayer);
        ghost.SetActive(false);
        ghostsByPrefab[prefab] = ghost;
        return ghost;
    }

    private void DrawRange(Vector3 center, Color color)
    {
        if (currentRange <= 0f) return;
        EnsureRangeLine();

        rangeLine.positionCount = rangeSegments;
        float y = center.y + rangeYOffset;
        for (int i = 0; i < rangeSegments; i++)
        {
            float a = i / (float)rangeSegments * Mathf.PI * 2f;
            rangeLine.SetPosition(i, new Vector3(center.x + Mathf.Cos(a) * currentRange, y, center.z + Mathf.Sin(a) * currentRange));
        }

        rangeLine.startColor = color;
        rangeLine.endColor = color;
        rangeLine.enabled = true;
    }

    private void EnsureRangeLine()
    {
        if (rangeLine != null) return;

        var go = new GameObject("TurretGhostRange");
        go.transform.SetParent(transform, false);
        go.layer = ghostLayer;
        rangeLine = go.AddComponent<LineRenderer>();
        rangeLine.loop = true;
        rangeLine.useWorldSpace = true;
        rangeLine.widthMultiplier = rangeWidth;
        rangeLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rangeLine.receiveShadows = false;
        rangeLine.sharedMaterial = rangeMaterial != null ? rangeMaterial : new Material(Shader.Find("Sprites/Default")) { hideFlags = HideFlags.DontSave };
        rangeLine.enabled = false;
    }
}
