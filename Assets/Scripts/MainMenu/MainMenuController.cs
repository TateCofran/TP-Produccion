using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "GameScene";

    public void OnTutorialButton()
    {
        TutorialProgress.ForceTutorialThisRun = true;
        SceneManager.LoadScene(gameSceneName);
    }

    public void OnPlayButton()
    {
        TutorialProgress.ForceTutorialThisRun = false;
        SceneManager.LoadScene(gameSceneName);
    }
}
