using UnityEngine;

[DisallowMultipleComponent]
public class ClickToPanel : MonoBehaviour
{
    [Header("Panel To Open")]
    [SerializeField] private GameObject panelToOpen;

    [Header("Behavior")]
    [Tooltip("If enabled, closes every other registered panel before opening this one.")]
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
