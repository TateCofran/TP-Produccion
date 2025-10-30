using UnityEngine;

[DisallowMultipleComponent]
public sealed class TutorialOpenButton : MonoBehaviour
{
    [SerializeField] private TutorialPanelController tutorial;

    public void Open()
    {
        if (tutorial != null)
            tutorial.OpenTutorial();
        else
            Debug.LogWarning("[TutorialOpenButton] Falta asignar 'tutorial' en el inspector.");
    }
}
