using UnityEngine;

[DisallowMultipleComponent]
public class PausePanelBinder : MonoBehaviour
{
    private void Awake()
    {
        // Si el GameManager ya existe, se registra automáticamente.
        if (GameManager.Instance != null)
            GameManager.Instance.RegisterPausePanel(gameObject);
    }

    private void OnEnable()
    {
        // Reafirma el registro por si el panel se habilita luego.
        if (GameManager.Instance != null)
            GameManager.Instance.RegisterPausePanel(gameObject);
    }
}
