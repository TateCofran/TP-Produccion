using UnityEngine;
using UnityEngine.EventSystems;
using static ShiftingWorldMechanic;

public class TurretPlacer : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private ShiftingWorldUI ui;
    [SerializeField] private Camera cam;

    [Header("Preview")]
    [SerializeField] private Vector3 ghostOffset = new Vector3(0f, 0.01f, 0f);

    [Header("Raycast")]
    [SerializeField] private LayerMask cellLayers = ~0;

    [SerializeField] private bool removeWithMiddleClick = true;

    private TurretDataSO selectedTurret;
    private GameObject ghost;
    private bool placing;

    public bool IsPlacing => placing;

    private void Awake()
    {
        if (ui != null) ui.OnTurretChosen += HandleTurretChosen;
    }

    private void OnEnable()
    {
        EscapeRouter.Register(HandleEscape, EscapeRouter.PriorityPlacement);
    }

    private void OnDisable()
    {
        EscapeRouter.Unregister(HandleEscape);
    }

    private void OnDestroy()
    {
        if (ui != null) ui.OnTurretChosen -= HandleTurretChosen;
    }

    private void HandleTurretChosen(TurretDataSO so)
    {
        selectedTurret = so;
        EnterPlacementMode();
    }

    private bool HandleEscape()
    {
        if (!placing) return false;
        ExitPlacementMode();
        return true;
    }

    private void EnterPlacementMode()
    {
        placing = selectedTurret != null;

        if (ghost != null)
        {
            Destroy(ghost);
            ghost = null;
        }

        if (placing && selectedTurret.prefab != null)
        {
            ghost = Instantiate(selectedTurret.prefab);

            foreach (var col in ghost.GetComponentsInChildren<Collider>())
                col.enabled = false;
        }
    }

    private void ExitPlacementMode()
    {
        placing = false;
        selectedTurret = null;

        if (ghost != null)
        {
            Destroy(ghost);
            ghost = null;
        }
    }

    private Camera ActiveCamera => cam ? cam : Camera.main;

    private static bool PointerOverUI => EventSystem.current && EventSystem.current.IsPointerOverGameObject();

    private void Update()
    {
        if (GameTime.IsPaused) return;

        if (TutorialManager.Instance != null && !TutorialManager.Instance.AllowPlaceTurrets)
            return;

        if ((removeWithMiddleClick && Input.GetMouseButtonDown(2)) ||
            (Input.GetKey(KeyCode.LeftShift) && Input.GetMouseButtonDown(0)))
        {
            TryRemoveUnderCursor();
            if (ghost) ghost.SetActive(false);
            return;
        }

        if (!placing) return;

        var cameraToUse = ActiveCamera;
        if (!cameraToUse) return;

        if (Input.GetMouseButtonDown(1))
        {
            ExitPlacementMode();
            return;
        }

        if (PointerOverUI)
        {
            if (ghost) ghost.SetActive(false);
            return;
        }

        Ray ray = cameraToUse.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out var hit, 500f, cellLayers))
        {
            if (ghost) ghost.SetActive(false);
            return;
        }

        var slot = hit.collider.GetComponentInParent<CellSlot>();
        if (slot == null || !slot.enabled || slot.IsOccupied)
        {
            if (ghost) ghost.SetActive(false);
            return;
        }

        if (ghost)
        {
            ghost.SetActive(true);
            var slotCol = slot.GetComponent<Collider>();
            var b = slotCol ? slotCol.bounds : hit.collider.bounds;
            Vector3 topCenter = new Vector3(b.center.x, b.max.y, b.center.z);
            ghost.transform.position = topCenter + ghostOffset;
        }

        if (Input.GetMouseButtonDown(0))
            TryPlaceOn(slot);
    }

    private void TryRemoveUnderCursor()
    {
        var cameraToUse = ActiveCamera;
        if (!cameraToUse || PointerOverUI) return;

        Ray ray = cameraToUse.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out var hit, 500f, cellLayers)) return;

        var slot = hit.collider.GetComponentInParent<CellSlot>();
        if (slot != null && slot.IsOccupied)
            slot.TryRemove();
    }

    private void TryPlaceOn(CellSlot slot)
    {
        var prefab = selectedTurret ? selectedTurret.prefab : null;
        if (!prefab) return;

        if (!slot.TryPlace(prefab, selectedTurret, out var instance)) return;

        TutorialEventHub.RaiseTurretPlaced();

        PlacementEvents.RaiseTurretPlaced(new PlacementEvents.TurretPlacedInfo
        {
            turretInstance = instance,
            worldPosition = slot.transform.position,
            turretId = selectedTurret ? selectedTurret.name : ""
        });

        if (ui != null)
            ui.NotifyTurretPlaced(World.Other);

        ExitPlacementMode();
    }
}
