using UnityEngine;

[DisallowMultipleComponent]
public sealed class CellSlot : MonoBehaviour
{
    [Header("Placement")]
    [SerializeField] private Transform parentForTurrets;
    [SerializeField] private float extraYOffset = 0.01f;

    [Header("State (runtime)")]
    [SerializeField] private bool occupied = false;
    [SerializeField] private GameObject currentTurret;

    [Header("Optional")]
    [Tooltip("If enabled, the range is shown automatically when the turret is placed.")]
    [SerializeField] private bool showRangeOnPlace = false;

    [SerializeReference] private ITurretDupeSystem dupeSystem;

    private Collider _cellCollider;
    private Transform _topAnchor;

    private void Awake()
    {
        _cellCollider = GetComponent<Collider>();
        if (!_cellCollider) _cellCollider = GetComponentInChildren<Collider>();

        var ta = transform.Find("TopAnchor");
        if (ta) _topAnchor = ta;
    }

    private Vector3 GetTopCenter()
    {
        if (_topAnchor) return _topAnchor.position;

        if (_cellCollider)
        {
            var b = _cellCollider.bounds;
            return new Vector3(b.center.x, b.max.y, b.center.z);
        }

        return transform.position;
    }

    private static float GetVisualHalfHeight(GameObject go)
    {
        var rends = go.GetComponentsInChildren<Renderer>();
        if (rends != null && rends.Length > 0)
        {
            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            return b.size.y * 0.5f;
        }

        var cols = go.GetComponentsInChildren<Collider>();
        if (cols != null && cols.Length > 0)
        {
            Bounds b = cols[0].bounds;
            for (int i = 1; i < cols.Length; i++) b.Encapsulate(cols[i].bounds);
            return b.size.y * 0.5f;
        }

        return 0.5f;
    }

    public bool TryPlace(GameObject turretPrefab, TurretDataSO turretData = null)
    {
        return TryPlace(turretPrefab, turretData, out _);
    }

    public bool TryPlace(GameObject turretPrefab, TurretDataSO turretData, out GameObject instance)
    {
        instance = null;

        if (IsOccupied || !turretPrefab) return false;

        Vector3 top = GetTopCenter();

        Transform parent = parentForTurrets ? parentForTurrets : null;
        GameObject turret = Instantiate(turretPrefab, top, Quaternion.identity, parent);

        Transform baseAnchor = turret.transform.Find("BaseAnchor");
        if (baseAnchor != null)
        {
            float deltaY = (top.y + extraYOffset) - baseAnchor.position.y;
            Vector3 p = turret.transform.position;
            turret.transform.position = new Vector3(top.x, p.y + deltaY, top.z);
        }
        else
        {
            float halfH = GetVisualHalfHeight(turret);
            float targetY = top.y + halfH + extraYOffset;
            turret.transform.position = new Vector3(top.x, targetY, top.z);
        }

        if (turretData != null && turret.TryGetComponent<TurretDataHolder>(out var dataHolder))
        {
            dataHolder.ApplyDataSO(turretData);
        }

        if (turret.TryGetComponent<Turret>(out var turretComp))
        {
            turretComp.SetPlaced(true);
            if (showRangeOnPlace) turretComp.ShowRange();
        }
        else
        {
            Debug.LogWarning($"[CellSlot] Instance '{turret.name}' has no Turret component");
        }

        currentTurret = turret;
        occupied = true;
        instance = turret;

        return true;
    }

    public bool TryRemove()
    {
        if (!occupied || !currentTurret) { occupied = false; currentTurret = null; return false; }

        Destroy(currentTurret);
        currentTurret = null;
        occupied = false;
        return true;
    }

    public bool IsOccupied => occupied && currentTurret != null;
    public GameObject GetCurrentTurret() => currentTurret;
}
