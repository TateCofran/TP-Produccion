using UnityEngine;
using UnityEngine.EventSystems;

public class ModifierIconHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private ActiveModifiersHUD hud;
    private ModifierSO modifier;

    public void Bind(ActiveModifiersHUD owner, ModifierSO mod)
    {
        hud = owner;
        modifier = mod;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hud != null) hud.ShowTooltip(modifier);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (hud != null) hud.HideTooltip();
    }

    private void OnDisable()
    {
        if (hud != null) hud.HideTooltip();
    }
}
