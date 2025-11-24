using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TileDupeBarUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform dupeContainer;
    [SerializeField] private GameObject activeDupeImage;
    [SerializeField] private GameObject inactiveDupeImage;
    [SerializeField] private TextMeshProUGUI tileTypeText;
    [SerializeField] private TextMeshProUGUI tileLevelText;

    private readonly List<GameObject> activeDupes = new();

    private void OnEnable()
    {
        if (tileTypeText == null)
            tileTypeText = GetComponentInChildren<TextMeshProUGUI>();

        if (TileDupeUI.Instance == null)
        {
            Debug.LogWarning("[TileDupeBarUI] No TileDupeUI instance active.");
            return;
        }

        UpdateDupeBar();
    }

    public string GetTileTypeName()
    {
        return tileTypeText != null ? tileTypeText.text : string.Empty;
    }

    public void UpdateDupeBar()
    {
        if (TileDupeUI.Instance == null) return;

        foreach (var dupe in activeDupes)
        {
            if (dupe != null)
                Destroy(dupe);
        }
        activeDupes.Clear();

        string typeName = GetTileTypeName();
        var data = TileDupeUI.Instance.GetTileData(typeName);

        if (data.maxDupes <= 0) return;

        float width = (80f - ((data.maxDupes - 1) * 4f)) / data.maxDupes;

        for (int i = 1; i <= data.maxDupes; i++)
        {
            GameObject prefab = (i <= data.currentDupes) ? activeDupeImage : inactiveDupeImage;
            GameObject dupeImg = Instantiate(prefab, dupeContainer);

            var rt = dupeImg.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.sizeDelta = new Vector2(width, 9f);
            }

            activeDupes.Add(dupeImg);
        }

        if (tileLevelText != null)
        {
            tileLevelText.text = data.level.ToString();
        }

        //Debug.Log($"[TileDupeBarUI] {typeName}: {data.currentDupes}/{data.maxDupes} dupes (level {data.level}).");
    }
}
