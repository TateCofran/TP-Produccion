using UnityEngine;
using UnityEngine.EventSystems;
using static ShiftingWorldMechanic;

public class TurretPlacer : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private ShiftingWorldUI ui;     
    [SerializeField] private Camera cam;             

    [Header("Preview (opcional)")]
    [SerializeField] private Vector3 ghostOffset = new Vector3(0f, 0.01f, 0f);

    [Header("Raycast")]
    [SerializeField] private LayerMask cellLayers = ~0;  

    private TurretDataSO _selectedTurret;
    private GameObject _ghost;
    private bool _placing;

    [SerializeField] private bool removeWithMiddleClick = true;

    private bool _removing;

    private void Awake()
    {
        if (ui != null) ui.OnTurretChosen += HandleTurretChosen;
    }

    private void OnDestroy()
    {
        if (ui != null) ui.OnTurretChosen -= HandleTurretChosen;
    }

    private void HandleTurretChosen(TurretDataSO so)
    {
        _selectedTurret = so;
        EnterPlacementMode();
    }

    private void EnterPlacementMode()
    {
        _placing = (_selectedTurret != null);

        if (_ghost != null)
        {
            Destroy(_ghost);
            _ghost = null;
        }

        if (_placing && _selectedTurret != null && _selectedTurret.prefab != null)
        {
            _ghost = Instantiate(_selectedTurret.prefab);


            var colliders = _ghost.GetComponentsInChildren<Collider>();
            foreach (var col in colliders)
            {
                col.enabled = false;
            }


        }
    }

    private void ExitPlacementMode()
    {
        _placing = false;
        _selectedTurret = null;

        if (_ghost != null)
        {
            Destroy(_ghost);
            _ghost = null;
        }
    }

    private void Update()
    {
        if (TutorialManager.Instance != null && !TutorialManager.Instance.AllowPlaceTurrets)
        {
            return;
        }

        if ((removeWithMiddleClick && Input.GetMouseButtonDown(2)) ||
            (Input.GetKey(KeyCode.LeftShift) && Input.GetMouseButtonDown(0)))
        {
            var camRemove = cam ? cam : Camera.main;
            if (!camRemove) return;

            if (EventSystem.current && EventSystem.current.IsPointerOverGameObject())
                return;

            Ray rayRemove = camRemove.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(rayRemove, out var hitRemove, 500f, cellLayers))
            {
                var slot = hitRemove.collider.GetComponentInParent<CellSlot>();
                if (slot != null && slot.IsOccupied)
                {
                    if (slot.TryRemove())
                        Debug.Log("[TurretPlacer] Torreta eliminada.");
                }
            }

            if (_ghost) _ghost.SetActive(false);
            return;
        }

        if (!_placing) return;

        var cameraToUse = cam ? cam : Camera.main;
        if (!cameraToUse) return;


        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
        {
            ExitPlacementMode();
            return;
        }
        if (EventSystem.current && EventSystem.current.IsPointerOverGameObject())
        {
            if (_ghost) _ghost.SetActive(false);
            return;
        }

        Ray ray = cameraToUse.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out var hit, 500f, cellLayers))
        {
            var slot = hit.collider.GetComponentInParent<CellSlot>();

            if (slot != null && slot.enabled && !slot.IsOccupied)
            {
                // Mostrar y posicionar ghost
                if (_ghost)
                {
                    _ghost.SetActive(true);
                    var slotCol = slot.GetComponent<Collider>();
                    var b = (slotCol ? slotCol.bounds : hit.collider.bounds);
                    Vector3 topCenter = new Vector3(b.center.x, b.max.y, b.center.z);
                    _ghost.transform.position = topCenter + ghostOffset;
                }

                // Click izquierdo = colocar
                if (Input.GetMouseButtonDown(0))
                {
                    var prefab = _selectedTurret ? _selectedTurret.prefab : null;
                    if (!prefab) return;

                    bool placed = false;

                    // Preferí la sobrecarga con data si existe
                    try
                    {
                        placed = slot.TryPlace(prefab, _selectedTurret);
                    }
                    catch
                    {
                        // Fallback a la versión sin data
                        placed = slot.TryPlace(prefab);
                    }

                    if (placed)
                    {
                        Debug.Log($"[TurretPlacer] Torreta colocada en {slot.name}");

                        // Tutorial EventHub
                        TutorialEventHub.RaiseTurretPlaced();   // << NUEVO

                        // Avisar al sistema de oleadas
                        PlacementEvents.RaiseTurretPlaced(new PlacementEvents.TurretPlacedInfo
                        {
                            turretInstance = prefab,
                            worldPosition = slot.transform.position,
                            turretId = _selectedTurret ? _selectedTurret.name : ""
                        });

                        // Avisar a la UI
                        if (ui != null)
                            ui.NotifyTurretPlaced(World.Otro);

                        ExitPlacementMode();
                    }
                }
            }
            else
            {
                if (_ghost) _ghost.SetActive(false);
            }
        }
        else
        {
            if (_ghost) _ghost.SetActive(false);
        }
    }

    // API opcional para un modo remover explícito por UI
    public void EnterRemoveMode()
    {
        _placing = false;
        _removing = true;
        if (_ghost) _ghost.SetActive(false);
    }

    public void ExitRemoveMode()
    {
        _removing = false;
    }
}
