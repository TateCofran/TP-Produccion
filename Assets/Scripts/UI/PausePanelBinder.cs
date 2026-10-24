using UnityEngine;

[DisallowMultipleComponent]
public class PausePanelBinder : MonoBehaviour
{
    private void Awake()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.RegisterPausePanel(gameObject);
    }

    private void OnEnable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.RegisterPausePanel(gameObject);
    }
}
