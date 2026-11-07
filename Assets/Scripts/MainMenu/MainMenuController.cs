using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "GameScene";
    [SerializeField] private LoadoutPanelUI loadoutOverlay;

    public void OnTutorialButton()
    {
        TutorialProgress.ForceTutorialThisRun = true;
        SceneManager.LoadScene(gameSceneName);
    }

    public void OnPlayButton()
    {
        if (ProgressionStore.Table != null && TryOpenLoadout()) return;
        StartGame();
    }

    public void StartGame()
    {
        TutorialProgress.ForceTutorialThisRun = false;
        RunLoadout.EnsureInitialized();
        SceneManager.LoadScene(gameSceneName);
    }

    private bool TryOpenLoadout()
    {
        if (loadoutOverlay == null)
        {
            var canvas = FindMenuCanvas();
            if (canvas == null) return false;
            var rt = UiKit.Fill(canvas.transform, "LoadoutOverlay");
            loadoutOverlay = rt.gameObject.AddComponent<LoadoutPanelUI>();
            loadoutOverlay.DimBackground = true;
        }
        loadoutOverlay.transform.SetAsLastSibling();
        loadoutOverlay.Open(StartGame, null);
        return true;
    }

    private Canvas FindMenuCanvas()
    {
        Canvas best = null;
        foreach (var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            if (c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay && (best == null || c.sortingOrder >= best.sortingOrder) && c.gameObject.name == "UI") best = c;
        if (best != null) return best;
        foreach (var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            if (c.isRootCanvas && (best == null || c.sortingOrder > best.sortingOrder)) best = c;
        return best;
    }
}
