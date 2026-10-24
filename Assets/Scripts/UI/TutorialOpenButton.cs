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
            Debug.LogWarning("[TutorialOpenButton] 'tutorial' is not assigned in the inspector");
    }
}
