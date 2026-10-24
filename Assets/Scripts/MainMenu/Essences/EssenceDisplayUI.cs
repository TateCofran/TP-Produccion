using TMPro;
using UnityEngine;

public class EssenceDisplayUI : MonoBehaviour
{
    [Header("Text References")]
    [SerializeField] private TextMeshProUGUI blueEssenceText;
    [SerializeField] private TextMeshProUGUI redEssenceText;

    [Header("Panel Type")]
    [Tooltip("Whether this panel uses blue essences (LabPanel) or red essences (WorkshopPanel)")]
    [SerializeField] private bool isBluePanel = true;

    private void OnEnable()
    {
        UpdateEssenceDisplay();
    }

    public void UpdateEssenceDisplay()
    {
        if (isBluePanel)
        {
            if (blueEssenceText != null)
                blueEssenceText.text = $"Blue Essences: {EssenceBank.TotalBlue}";
        }
        else
        {
            if (redEssenceText != null)
                redEssenceText.text = $"Red Essences: {EssenceBank.TotalRed}";
        }
    }
}
