using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class ResolutionsSettings : MonoBehaviour
{
    [SerializeField] TMP_Dropdown resolutionDropdown;
    private List<Resolution> filteredResolutions;

    private void Start()
    {
        Resolution[] allRes = Screen.resolutions;
        filteredResolutions = new List<Resolution>();

        // --- LIMPIA OPCIONES DEL DROPDOWN ---
        resolutionDropdown.ClearOptions();

        // --- FILTRA DUPLICADOS POR WIDTH + HEIGHT ---
        HashSet<string> seen = new HashSet<string>();

        foreach (var res in allRes)
        {
            string key = res.width + "x" + res.height;

            if (!seen.Contains(key))
            {
                seen.Add(key);
                filteredResolutions.Add(res);
            }
        }

        // --- CREA LAS OPCIONES ---
        List<string> options = new List<string>();
        foreach (var res in filteredResolutions)
        {
            options.Add(res.width + " x " + res.height);
        }

        resolutionDropdown.AddOptions(options);

        // Selecciona automáticamente la resolución actual
        int currentIndex = filteredResolutions.FindIndex(r =>
            r.width == Screen.width && r.height == Screen.height);

        if (currentIndex != -1)
            resolutionDropdown.value = currentIndex;

        resolutionDropdown.onValueChanged.AddListener(SetResolution);
    }

    public void SetResolution(int index)
    {
        var res = filteredResolutions[index];
        Screen.SetResolution(res.width, res.height, Screen.fullScreenMode);
    }
}
