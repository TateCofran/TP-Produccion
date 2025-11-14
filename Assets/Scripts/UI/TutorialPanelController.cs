using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class TutorialPanelController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Button closeButton;

    private bool _isOpen;

    private void Reset()
    {
        // Auto-guess en caso de agregar el script desde el inspector
        if (panel == null)
            panel = gameObject;

        if (closeButton == null)
            closeButton = GetComponentInChildren<Button>();
    }

    private void Awake()
    {
        // Asegurarnos de que el panel arranca cerrado
        if (panel != null)
            panel.SetActive(false);

        _isOpen = false;

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseTutorial);
            closeButton.onClick.AddListener(CloseTutorial);
        }
        else
        {
            Debug.LogWarning("[TutorialPanelController] Falta asignar 'closeButton' en el inspector.");
        }
    }

    public void OpenTutorial()
    {
        if (_isOpen) return;
        _isOpen = true;

        if (panel != null)
            panel.SetActive(true);

        // Pausar el juego SIN abrir el panel de pausa
        if (GameManager.Instance != null)
            GameManager.Instance.PauseOnly();
        else
            Time.timeScale = 0f; // fallback si no hay GameManager
    }

    public void CloseTutorial()
    {
        if (!_isOpen) return;
        _isOpen = false;

        if (panel != null)
            panel.SetActive(false);

        // Reanudar el juego usando la lógica normal del GameManager
        if (GameManager.Instance != null)
            GameManager.Instance.ResumeGame();
        else
            Time.timeScale = 1f; // fallback
    }
}
