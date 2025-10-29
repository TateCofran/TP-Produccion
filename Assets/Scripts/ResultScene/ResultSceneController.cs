using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class ResultSceneController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text titleText;      // “¡Ganaste!” / “Perdiste...”
    [SerializeField] private TMP_Text timeText;       // “Tiempo: mm:ss”
    [SerializeField] private TMP_Text waveText;     //oleada q alcanzó el jugador
    [SerializeField] private TMP_Text totalEnemiesKilledText; //cantidad de enemigos que mato el jugador
    [SerializeField] private TMP_Text blueEssencesText; //normal
    [SerializeField] private TMP_Text redEssencesText;  //other

    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button playAgainButton;

    private void OnEnable()
    {
        // Por si el orden de inicialización viene raro
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
        // Reintenta cuando la escena terminó de cargar
        UpdateUI();
        WireButtons();
    }

    private IEnumerator Start()
    {
        // Espera un frame por si GameManager llega en el siguiente ciclo
        yield return null;
        UpdateUI();
    }

    private void UpdateUI()
    {
        var gm = GameManager.Instance != null ? GameManager.Instance : FindObjectOfType<GameManager>();

        if (gm == null)
        {
            // Fallback visible para detectar si no existe GameManager
            if (titleText) titleText.text = "Results";
            if (timeText) timeText.text = "Tiempo: --:--";
            if (waveText) waveText.text = "Waves completed: -";
            if (totalEnemiesKilledText) totalEnemiesKilledText.text = "Total Enemies killed: -";
            if (blueEssencesText) blueEssencesText.text = $"Normal Essences: -";
            if (redEssencesText) redEssencesText.text = $"Other World Essences: -";

            Debug.LogWarning("[ResultSceneController] GameManager no encontrado. ¿Está marcado como DontDestroyOnLoad?");
            return;
        }

        if (titleText) titleText.text = gm.PlayerHasWon ? "¡Ganaste!" : "Perdiste...";
        if (timeText) timeText.text = "Tiempo: " + FormatTime(gm.timePlayed);
        if (waveText) waveText.text = "Waves Completed: " + gm.wavesCompleted;
        if (totalEnemiesKilledText) totalEnemiesKilledText.text = "Total Enemies Killed: " + gm.totalEnemiesKilled;
        if (blueEssencesText)
            blueEssencesText.text = $"Normal Essences (run): {gm.totalBlueEssences}\nBank total: {EssenceBank.TotalBlue}";
        if (redEssencesText)
            redEssencesText.text = $"Other World Essences (run): {gm.totalRedEssences}\nBank total: {EssenceBank.TotalRed}";

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
