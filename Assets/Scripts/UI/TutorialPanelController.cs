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
        if (panel == null) panel = gameObject;
        if (closeButton == null)
            closeButton = GetComponentInChildren<Button>(true);
    }

    private void Awake()
    {
        if (panel == null) panel = gameObject;
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(CloseTutorial);
        }

        // Aseguramos que arranque oculto
        if (panel.activeSelf)
        {
            panel.SetActive(false);
            _isOpen = false;
        }
    }

    public void OpenTutorial()
    {
        if (_isOpen) return;
        _isOpen = true;

        if (panel != null) panel.SetActive(true);

        // Pausar el juego
        if (GameManager.Instance != null)
            GameManager.Instance.PauseGame();
        else
            Time.timeScale = 0f; // fallback
    }

    public void CloseTutorial()
    {
        if (!_isOpen) return;
        _isOpen = false;

        if (panel != null) panel.SetActive(false);

        // Reanudar el juego
        if (GameManager.Instance != null)
            GameManager.Instance.ResumeGame();
        else
            Time.timeScale = 1f; // fallback
    }
}
