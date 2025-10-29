using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PanelSwitcher : MonoBehaviour
{
    public static PanelSwitcher Instance { get; private set; }

    [Header("Paneles registrados (arrastrá acá tus paneles)")]
    [SerializeField] private List<GameObject> panels = new();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    public void Register(GameObject panel)
    {
        if (panel != null && !panels.Contains(panel))
            panels.Add(panel);
    }

    public void Unregister(GameObject panel)
    {
        if (panel != null) panels.Remove(panel);
    }

    public void ShowOnly(GameObject target)
    {
        if (!target) return;

        foreach (var p in panels)
        {
            if (p != null) p.SetActive(false);
        }
        target.SetActive(true);
    }

    public void HideAll()
    {
        foreach (var p in panels)
        {
            if (p != null) p.SetActive(false);
        }
    }
}
