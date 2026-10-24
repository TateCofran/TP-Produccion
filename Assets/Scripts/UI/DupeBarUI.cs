using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DupeBarUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform dupeContainer;
    [SerializeField] private GameObject activeDupeImage;
    [SerializeField] private GameObject inactiveDupeImage;
    [SerializeField] private TextMeshProUGUI turretType;
    [SerializeField] private TextMeshProUGUI turretLevelText;

    private List<GameObject> activeDupes = new List<GameObject>();

    private void OnEnable()
    {
        if (turretType == null)
            turretType = GetComponentInChildren<TextMeshProUGUI>();

        if (TurretDupeUI.Instance == null)
        {
            Debug.LogWarning("[DupeBarUI] No active TurretDupeUI instance");
            return;
        }

        UpdateDupeBar();
    }

    public string GetTurretTypeName()
    {
        return turretType != null ? turretType.text : "";
    }

    public void UpdateDupeBar()
    {
        foreach (var dupe in activeDupes)
            Destroy(dupe);
        activeDupes.Clear();

        string typeName = turretType.text;
        var data = TurretDupeUI.Instance.GetTurretData(typeName);
        Debug.Log("[DupeBarUI] Current data type: " + data);

        float width = (80f - ((data.maxDupes - 1) * 4f)) / data.maxDupes;

        for (int i = 1; i <= data.maxDupes; i++)
        {
            GameObject prefab = (i <= data.currentDupes) ? activeDupeImage : inactiveDupeImage;
            GameObject dupeImg = Instantiate(prefab, dupeContainer);

            RectTransform rt = dupeImg.GetComponent<RectTransform>();

            rt.sizeDelta = new Vector2(width, 9f);

            activeDupes.Add(dupeImg);
        }

        if (turretLevelText != null)
        {
            turretLevelText.text = data.level.ToString();
        }

        Debug.Log($"[DupeBarUI] Dupes UI built for {typeName}: {data.currentDupes}/{data.maxDupes} (width {width})");
    }
}
