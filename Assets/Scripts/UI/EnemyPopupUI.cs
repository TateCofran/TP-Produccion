using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class EnemyPopupUI : MonoBehaviour
{
    [Header("Popup")]
    [SerializeField] private GameObject popupObject;
    [SerializeField] private TextMeshProUGUI popupText;
    [SerializeField] private Button popupButton;

    [Header("Panel de info")]
    [SerializeField] private GameObject infoPanelObject;
    [SerializeField] private EnemyInfoPanel infoPanel;

    private EnemyData currentData;


    private void Start()
    {
        popupObject.SetActive(false);
        infoPanelObject.SetActive(false);

        SpawnManager sm = FindFirstObjectByType<SpawnManager>();
        sm.OnFirstTimeEnemySpawned += HandleFirstSpawn;

        popupButton.onClick.AddListener(OpenInfoPanel);
    }

    private void HandleFirstSpawn(EnemyData data)
    {
        currentData = data;

        popupText.text = $"New enemy type: {data.enemyName}";
        popupObject.SetActive(true);


        StartCoroutine(HidePopupAfter10Sec());
    }

    private IEnumerator HidePopupAfter10Sec()
    {
        yield return new WaitForSeconds(10f);
        popupObject.SetActive(false);

    }

    private void OpenInfoPanel()
    {
        popupObject.SetActive(false);

        Time.timeScale = 0f;
        infoPanelObject.SetActive(true);

        infoPanel.Display(currentData);
    }

    public void CloseInfoPanel()
    {
        infoPanelObject.SetActive(false);
        Time.timeScale = 1f;
    }
}
