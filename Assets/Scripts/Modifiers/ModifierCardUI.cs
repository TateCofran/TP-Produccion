using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ModifierCardUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Button button;

    public void Bind(ModifierSO mod, Action onClick)
    {
        if (button == null) button = GetComponentInChildren<Button>(true);

        if (nameText) nameText.text = mod != null ? mod.displayName : string.Empty;
        if (descriptionText) descriptionText.text = mod != null ? mod.description : string.Empty;

        if (iconImage)
        {
            iconImage.sprite = mod != null ? mod.icon : null;
            iconImage.enabled = iconImage.sprite != null;
        }

        if (button)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke());
            button.interactable = true;
        }
        else
        {
            Debug.LogWarning("[ModifierCardUI] No Button found on the card");
        }
    }
}
