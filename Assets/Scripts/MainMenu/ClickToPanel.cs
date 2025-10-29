using UnityEngine;

[DisallowMultipleComponent]
public class ClickToPanel : MonoBehaviour
{
    [Header("Panel a abrir")]
    [SerializeField] private GameObject panelToOpen;

    [Header("Comportamiento")]
    [Tooltip("Si está activo, cerrará todos los demás paneles registrados antes de abrir este.")]
    [SerializeField] private bool exclusiveShow = true;

    public void OpenPanel()
    {
        if (!panelToOpen) return;

        if (exclusiveShow && PanelSwitcher.Instance != null)
        {
            PanelSwitcher.Instance.ShowOnly(panelToOpen);
        }
        else
        {
            panelToOpen.SetActive(true);
        }
    }

    public void ClosePanel()
    {
        if (panelToOpen) panelToOpen.SetActive(false);
    }

    public GameObject GetPanel() => panelToOpen;
}
