using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnemyPopupUI : MonoBehaviour
{
    [Header("Popup")]
    [SerializeField] private GameObject popupObject;
    [SerializeField] private TextMeshProUGUI popupText;
    [SerializeField] private Button popupButton;

    [SerializeField] private Image popupIcon;
    [SerializeField] private EnemySpriteData[] enemySprites;

    [Header("Info Panel")]
    [SerializeField] private GameObject infoPanelObject;
    [SerializeField] private EnemyInfoPanel infoPanel;

    [SerializeField] private float popupDuration = 10f;

    private EnemyData currentData;
    private SpawnManager spawnManager;
    private Coroutine hideRoutine;
    private bool infoPanelOpen;

    private void Start()
    {
        popupObject.SetActive(false);
        infoPanelObject.SetActive(false);

        spawnManager = FindFirstObjectByType<SpawnManager>();
        if (spawnManager != null)
            spawnManager.OnFirstTimeEnemySpawned += HandleFirstSpawn;
        else
            Debug.LogWarning("[EnemyPopupUI] SpawnManager not found in the scene");

        popupButton.onClick.AddListener(OpenInfoPanel);
    }

    private void OnDestroy()
    {
        if (spawnManager != null)
            spawnManager.OnFirstTimeEnemySpawned -= HandleFirstSpawn;

        EscapeRouter.Unregister(HandleEscape);
        if (infoPanelOpen) GameTime.ReleasePause();
    }

    private void HandleFirstSpawn(EnemyData data)
    {
        currentData = data;
        popupIcon.sprite = GetSprite(data.enemyName);
        popupObject.SetActive(true);

        if (hideRoutine != null) StopCoroutine(hideRoutine);
        hideRoutine = StartCoroutine(HidePopupAfterDelay());
    }

    private IEnumerator HidePopupAfterDelay()
    {
        yield return new WaitForSeconds(popupDuration);
        popupObject.SetActive(false);
        hideRoutine = null;
    }

    private void OpenInfoPanel()
    {
        popupObject.SetActive(false);
        infoPanelObject.SetActive(true);
        infoPanel.Display(currentData);

        if (infoPanelOpen) return;
        infoPanelOpen = true;
        GameTime.RequestPause();
        EscapeRouter.Register(HandleEscape, EscapeRouter.PriorityPopup);
    }

    public void CloseInfoPanel()
    {
        infoPanelObject.SetActive(false);

        if (!infoPanelOpen) return;
        infoPanelOpen = false;
        EscapeRouter.Unregister(HandleEscape);
        GameTime.ReleasePause();
    }

    private bool HandleEscape()
    {
        if (!infoPanelOpen) return false;
        CloseInfoPanel();
        return true;
    }

    private Sprite GetSprite(string enemyName)
    {
        foreach (var e in enemySprites)
        {
            if (e.enemyName == enemyName)
                return e.sprite;
        }
        Debug.LogWarning($"[EnemyPopupUI] No sprite defined for {enemyName}");
        return null;
    }
}

[System.Serializable]
public class EnemySpriteData
{
    public string enemyName;
    public Sprite sprite;
}
