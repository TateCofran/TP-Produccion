using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PauseMenuAutoWire : MonoBehaviour
{
    [Header("Pause Menu Buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button menuButton;

    private void OnEnable()
    {
        WireButtons();
    }

    private void Start()
    {
        WireButtons();
    }

    private void WireButtons()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(gm.ResumeGame);
        }

        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();

            restartButton.onClick.AddListener(gm.OnPlayAgainButton);
        }

        if (menuButton != null)
        {
            menuButton.onClick.RemoveAllListeners();
            menuButton.onClick.AddListener(gm.OnMainMenuButton);
        }
    }
}
