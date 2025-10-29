using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

[DisallowMultipleComponent]
public class UIPanelController : MonoBehaviour, IPanelController
{
    [SerializeField] private List<GameObject> panels = new();

    public void HideAll()
    {
        foreach (var p in panels)
            if (p) p.SetActive(false);
    }
    public void HideAllExcept(GameObject except)
    {
        foreach (var p in panels)
        {
            if (!p) continue;
            if (except != null && p == except) continue;
            p.SetActive(false);
        }
    }
    public void Show(GameObject panel)
    {
        if (panel) panel.SetActive(true);
    }
}
