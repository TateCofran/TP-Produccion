using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class RaycasterHoverClick : MonoBehaviour
{
    [Header("Refs")]
    public Camera cam;

    [Header("Raycast")]
    public LayerMask layerMask = ~0;
    public float maxDistance = 250f;

    [Header("Fallback")]
    [Tooltip("Si el objeto clickeado no tiene ClickToScene ni ClickToPanel, cargar esta escena (opcional).")]
    public string defaultSceneToLoad;

    private OutlineOnLook _current;

    void Update()
    {
        // Evitar raycasts cuando el puntero está sobre UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            ClearCurrent();
            return;
        }

        var cameraToUse = cam != null ? cam : Camera.main;
        if (cameraToUse == null) return;

        Ray ray = cameraToUse.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, layerMask, QueryTriggerInteraction.Ignore))
        {
            // ----- HOVER / OUTLINE -----
            if (hit.transform.TryGetComponent<OutlineOnLook>(out var outline) ||
                hit.transform.GetComponentInParent<OutlineOnLook>() is OutlineOnLook outlineParent && (outline = outlineParent))
            {
                if (_current != outline)
                {
                    _current?.Outline(false);
                    _current = outline;
                }
                _current.Outline(true);
            }
            else
            {
                ClearCurrent();
            }

            // ----- CLICK -----
            if (Input.GetMouseButtonDown(0))
            {
                // 1) Intentar abrir PANEL
                if (hit.transform.TryGetComponent<ClickToPanel>(out var clickToPanel) ||
                    hit.transform.GetComponentInParent<ClickToPanel>() is ClickToPanel clickToPanelParent && (clickToPanel = clickToPanelParent))
                {
                    clickToPanel.OpenPanel();
                    return; // listo, no seguimos a escena
                }

                // 2) Si no hay panel, intentar cargar ESCENA
                if (hit.transform.TryGetComponent<ClickToScene>(out var clickToScene) ||
                    hit.transform.GetComponentInParent<ClickToScene>() is ClickToScene clickParent && (clickToScene = clickParent))
                {
                    if (!string.IsNullOrWhiteSpace(clickToScene.sceneName))
                    {
                        SceneManager.LoadScene(clickToScene.sceneName);
                        return;
                    }
                }

                // 3) Fallback opcional
                if (!string.IsNullOrWhiteSpace(defaultSceneToLoad))
                {
                    SceneManager.LoadScene(defaultSceneToLoad);
                }
            }
        }
        else
        {
            ClearCurrent();
        }
    }

    private void ClearCurrent()
    {
        if (_current != null)
        {
            _current.Outline(false);
            _current = null;
        }
    }
}
