using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "GameScene";

    // BOTÓN: TUTORIAL
    public void OnTutorialButton()
    {
        TutorialProgress.ForceTutorialThisRun = true;
        SceneManager.LoadScene(gameSceneName);
    }

    // BOTÓN: JUGAR NORMAL
    public void OnPlayButton()
    {
        TutorialProgress.ForceTutorialThisRun = false;
        SceneManager.LoadScene(gameSceneName);
    }
}
