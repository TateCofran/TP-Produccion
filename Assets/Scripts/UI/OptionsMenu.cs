using UnityEngine;

public class OptionsMenu : MonoBehaviour
{
    [Header("Panels")]
    public GameObject optionsPanel;
    public GameObject soundPanel;
    public GameObject screenPanel;

    public void OpenSoundPanel()
    {
        optionsPanel.SetActive(false);
        soundPanel.SetActive(true);
        screenPanel.SetActive(false);
    }

    public void OpenScreenPanel()
    {
        optionsPanel.SetActive(false);
        soundPanel.SetActive(false);
        screenPanel.SetActive(true);
    }

    public void BackToOptions()
    {
        optionsPanel.SetActive(true);
        soundPanel.SetActive(false);
        screenPanel.SetActive(false);
    }

    public void ClosePanel(GameObject panel)
    {
        panel.SetActive(false);
    }
}

