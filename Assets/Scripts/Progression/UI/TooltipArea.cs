using UnityEngine;
using UnityEngine.EventSystems;

public class TooltipArea : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public string text;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!string.IsNullOrEmpty(text)) OptionTooltip.Show(text, transform as RectTransform);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        OptionTooltip.HideTooltip();
    }

    private void OnDisable()
    {
        OptionTooltip.HideTooltip();
    }
}
