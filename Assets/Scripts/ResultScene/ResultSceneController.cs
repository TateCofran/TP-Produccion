using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class ResultSceneController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text titleText;      
    [SerializeField] private TMP_Text timeText;      
    [SerializeField] private TMP_Text waveText;   
    [SerializeField] private TMP_Text totalEnemiesKilledText;
    [SerializeField] private TMP_Text blueEssencesText;
    [SerializeField] private TMP_Text redEssencesText;  

    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button playAgainButton;

    [SerializeField] private GameObject winImage;
    [SerializeField] private GameObject loseImage;


    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        UpdateUI();
        WireButtons();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        UpdateUI();
        WireButtons();
    }

    private IEnumerator Start()
    {
        yield return null;
        UpdateUI();
    }

    private void UpdateUI()
    {
        var gm = GameManager.Instance != null ? GameManager.Instance : FindFirstObjectByType<GameManager>();

        if (gm == null)
        {
            Debug.LogWarning("[ResultSceneController] GameManager no encontrado.");
            return;
        }

        if (titleText) titleText.text = gm.PlayerHasWon ? "¡Won!" : "Lose...";

        if (winImage) winImage.SetActive(gm.PlayerHasWon);
        if (loseImage) loseImage.SetActive(!gm.PlayerHasWon);

        if (timeText) timeText.text = "Time: " + FormatTime(gm.timePlayed);
        if (waveText) waveText.text = "Waves Completed: " + gm.wavesCompleted;
        if (totalEnemiesKilledText) totalEnemiesKilledText.text = "Enemies Killed: " + gm.totalEnemiesKilled;

        if (blueEssencesText)
            blueEssencesText.text = $"Normal Essences: {gm.totalBlueEssences} Total: {EssenceBank.TotalBlue}";

        if (redEssencesText)
            redEssencesText.text = $"OW Essences: {gm.totalRedEssences} Total: {EssenceBank.TotalRed}";
    }

    private void WireButtons()
    {
        if (GameManager.Instance == null) return;

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(() => GameManager.Instance.OnMainMenuButton());
        }

        if (playAgainButton != null)
        {
            playAgainButton.onClick.RemoveAllListeners();
            playAgainButton.onClick.AddListener(() => GameManager.Instance.OnPlayAgainButton());
        }
    }

    private string FormatTime(float seconds)
    {
        int total = Mathf.FloorToInt(seconds);
        int mins = total / 60;
        int secs = total % 60;
        return $"{mins:00}:{secs:00}";
    }
}
