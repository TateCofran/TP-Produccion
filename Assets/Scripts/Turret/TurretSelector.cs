using UnityEngine;
using UnityEngine.EventSystems;

public class TurretSelector : MonoBehaviour
{
    [Header("Raycast")]
    [SerializeField] private Camera cam;                 // cámara a usar (si no se asigna usa Camera.main)
    [SerializeField] private LayerMask turretLayer = ~0; // capa de torretas (opcional)
    [SerializeField] private float maxDistance = 1000f;

    private Turret selectedTurret;

    private Camera Cam => cam ? cam : Camera.main;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
            HandleSelection();

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
            DeselectTurret();
    }

    private void HandleSelection()
    {
        // Evitar clicks a través de UI
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

        // click vacío = deseleccionar actual
        DeselectTurret();
    }

    private void SelectTurret(Turret turret)
    {
        if (selectedTurret == turret)
        {
            // ya está seleccionada → refrescar panel igual
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
