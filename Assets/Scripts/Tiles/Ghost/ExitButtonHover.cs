using System;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class ExitButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Action onEnter;
    private Action onExit;

    public void Bind(Action enter, Action exit)
    {
        onEnter = enter;
        onExit = exit;
    }

    public void OnPointerEnter(PointerEventData eventData) => onEnter?.Invoke();

    public void OnPointerExit(PointerEventData eventData) => onExit?.Invoke();

    private void OnDisable()
    {
        onExit?.Invoke();
    }
}
