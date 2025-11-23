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

    [SerializeField] private Image popupIcon;                    
    [SerializeField] private EnemySpriteData[] enemySprites;     

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

        //popupText.text = $"New enemy type: {data.enemyName}";
        popupIcon.sprite = GetSprite(data.enemyName);

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

    private Sprite GetSprite(string enemyName)
    {
        foreach (var e in enemySprites)
        {
            if (e.enemyName == enemyName)
                return e.sprite;
        }
        Debug.LogWarning($"No hay sprite definido para {enemyName}");
        return null;
    }
}

[System.Serializable]
public class EnemySpriteData
{
    public string enemyName;
    public Sprite sprite;
}