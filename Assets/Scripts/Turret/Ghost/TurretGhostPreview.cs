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
        if (!visible) lastSlot = null;
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
