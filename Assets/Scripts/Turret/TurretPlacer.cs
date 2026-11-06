using UnityEngine;
using UnityEngine.EventSystems;
using static ShiftingWorldMechanic;

public class TurretPlacer : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private ShiftingWorldUI ui;
    [SerializeField] private Camera cam;

    [Header("Preview")]
    [SerializeField] private TurretGhostPreview ghostPreview;

    [Header("Raycast")]
    [SerializeField] private LayerMask cellLayers = ~0;

    [SerializeField] private bool removeWithMiddleClick = true;

    private TurretDataSO selectedTurret;
    private bool placing;
    private ITurretDupeSystem dupeSystem;

    public bool IsPlacing => placing;

    private void Awake()
    {
        if (ui != null) ui.OnTurretChosen += HandleTurretChosen;
        if (ghostPreview == null) ghostPreview = GetComponent<TurretGhostPreview>();
        if (ghostPreview == null) ghostPreview = gameObject.AddComponent<TurretGhostPreview>();
        ghostPreview.Configure(cam, cellLayers);
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
        CancelPlacement();
        return true;
    }

    private void EnterPlacementMode()
    {
        placing = selectedTurret != null;

        if (dupeSystem == null) dupeSystem = FindFirstObjectByType<TurretDupeSystem>();
        if (placing && ghostPreview != null)
            ghostPreview.Begin(selectedTurret, dupeSystem != null ? dupeSystem.GetTurretLevelData(selectedTurret) : null);
    }

    private void ExitPlacementMode()
    {
        placing = false;
        selectedTurret = null;
        if (ghostPreview != null) ghostPreview.End();
    }

    private void CancelPlacement()
    {
        ExitPlacementMode();
        if (ui != null) ui.NotifyTurretPlacementCancelled();
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
            return;
        }

        if (!placing) return;

        var cameraToUse = ActiveCamera;
        if (!cameraToUse) return;

        if (Input.GetMouseButtonDown(1))
        {
            CancelPlacement();
            return;
        }

        if (PointerOverUI) return;

        Ray ray = cameraToUse.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out var hit, 500f, cellLayers)) return;

        var slot = hit.collider.GetComponentInParent<CellSlot>();
        if (slot == null || !slot.enabled || slot.IsOccupied) return;

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

        if (ghostPreview != null) ghostPreview.End();
        if (!slot.TryPlace(prefab, selectedTurret, out var instance))
        {
            if (ghostPreview != null) ghostPreview.Begin(selectedTurret, dupeSystem != null ? dupeSystem.GetTurretLevelData(selectedTurret) : null);
            return;
        }

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
