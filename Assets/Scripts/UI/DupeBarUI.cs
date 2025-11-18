using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DupeBarUI : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private Transform dupeContainer;      // Contenedor donde se instancian las imágenes dupe
    [SerializeField] private GameObject activeDupeImage;   // Prefab de la imagen roja de dupe
    [SerializeField] private GameObject inactiveDupeImage;
    [SerializeField] private TextMeshProUGUI turretType;   // Texto con el nombre de la torreta
    [SerializeField] private TextMeshProUGUI turretLevelText;

    private List<GameObject> activeDupes = new List<GameObject>();

    private void OnEnable()
    {
        // Buscar texto si no está asignado
        if (turretType == null)
            turretType = GetComponentInChildren<TextMeshProUGUI>();

        if (TurretDupeUI.Instance == null)
        {
            Debug.LogWarning("No hay instancia de TurretDupeUI activa.");
            return;
        }

        // Mostrar los dupes iniciales según los datos actuales
        UpdateDupeBar();
    }

    public string GetTurretTypeName()
    {
        return turretType != null ? turretType.text : "";
    }


    public void UpdateDupeBar()
    {
        // Limpiar los dupes anteriores
        foreach (var dupe in activeDupes)
            Destroy(dupe);
        activeDupes.Clear();

        // Obtener los datos del tipo actual
        string typeName = turretType.text;
        var data = TurretDupeUI.Instance.GetTurretData(typeName);
        Debug.Log("data tipo actual: " + data);
        
        // Calcular el ancho de cada imagen
        float ancho = (80f - ((data.maxDupes - 1) * 4f)) / data.maxDupes;

        // Instanciar las imágenes según el número de dupes actuales
        for (int i = 1; i <= data.maxDupes; i++)
        {
            GameObject prefab = (i <= data.currentDupes) ? activeDupeImage : inactiveDupeImage;
            GameObject dupeImg = Instantiate(prefab, dupeContainer);
            
            RectTransform rt = dupeImg.GetComponent<RectTransform>();

            // Ajustar tamaño
            rt.sizeDelta = new Vector2(ancho, 9f);

            activeDupes.Add(dupeImg);
        }

        if (turretLevelText != null)
        {
            turretLevelText.text = data.level.ToString();
        }

        Debug.Log($"[{typeName}] Dupes UI generada: {data.currentDupes}/{data.maxDupes} (ancho {ancho})");
    }
}
