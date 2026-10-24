using UnityEngine;
using UnityEngine.EventSystems;

public class TurretSelector : MonoBehaviour
{
    [Header("Raycast")]
    [SerializeField] private Camera cam;
    [SerializeField] private LayerMask turretLayer = ~0;
    [SerializeField] private float maxDistance = 1000f;

    private Turret selectedTurret;

    private Camera Cam => cam ? cam : Camera.main;

    private void OnEnable()
    {
        EscapeRouter.Register(HandleEscape, EscapeRouter.PrioritySelection);
    }

    private void OnDisable()
    {
        EscapeRouter.Unregister(HandleEscape);
    }

    private void Update()
    {
        if (GameTime.IsPaused) return;

        if (Input.GetMouseButtonDown(0))
            HandleSelection();

        if (Input.GetMouseButtonDown(1))
            DeselectTurret();
    }

    private bool HandleEscape()
    {
        if (selectedTurret == null) return false;
        DeselectTurret();
        return true;
    }

    private void HandleSelection()
    {
        if (EventSystem.current && EventSystem.current.IsPointerOverGameObject())
            return;

        var cameraToUse = Cam;
        if (!cameraToUse) return;

        Ray ray = cameraToUse.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, turretLayer))
        {
            Turret turret = hit.collider.GetComponentInParent<Turret>();
            if (turret != null)
            {
                SelectTurret(turret);
                return;
            }
        }

        DeselectTurret();
    }

    private void SelectTurret(Turret turret)
    {
        if (selectedTurret == turret)
        {
            TurretInfoPanel.Instance?.Show(turret);
            return;
        }

        if (selectedTurret != null)
            selectedTurret.HideRange();

        selectedTurret = turret;
        selectedTurret.ShowRange();

        TurretInfoPanel.Instance?.Show(selectedTurret);
    }

    private void DeselectTurret()
    {
        if (selectedTurret != null)
        {
            selectedTurret.HideRange();
            selectedTurret = null;
        }
        TurretInfoPanel.Instance?.Hide();
    }
}
