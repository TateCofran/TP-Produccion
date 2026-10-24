using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class TutorialPanelController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Button closeButton;

    private bool isOpen;

    private void Reset()
    {
        if (panel == null)
            panel = gameObject;

        if (closeButton == null)
            closeButton = GetComponentInChildren<Button>();
    }

    private void Awake()
    {
        if (panel != null)
            panel.SetActive(false);

        isOpen = false;

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseTutorial);
            closeButton.onClick.AddListener(CloseTutorial);
        }
        else
        {
            Debug.LogWarning("[TutorialPanelController] 'closeButton' is not assigned in the inspector");
        }
    }

    private void OnDestroy()
    {
        EscapeRouter.Unregister(HandleEscape);
        if (isOpen) GameTime.ReleasePause();
    }

    public void OpenTutorial()
    {
        if (isOpen) return;
        isOpen = true;

        if (panel != null)
            panel.SetActive(true);

        GameTime.RequestPause();
        EscapeRouter.Register(HandleEscape, EscapeRouter.PriorityPopup);
    }

    public void CloseTutorial()
    {
        if (!isOpen) return;
        isOpen = false;

        if (panel != null)
            panel.SetActive(false);

        EscapeRouter.Unregister(HandleEscape);
        GameTime.ReleasePause();
    }

    private bool HandleEscape()
    {
        if (!isOpen) return false;
        CloseTutorial();
        return true;
    }
}
