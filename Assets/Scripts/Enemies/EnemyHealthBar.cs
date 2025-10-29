using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;
using System.Collections;

public class EnemyHealthBar : MonoBehaviour, IHealthDisplay
{
    [Header("Prefabs & Refs")]
    [SerializeField] private GameObject healthBarPrefab;
    [SerializeField] private float healthBarHeight = 1.5f;

    [SerializeField] private float damageTextYOffset = 2f; // separación arriba de la barra
    [SerializeField] private float damageTextXOffset = -30f;
    
    [SerializeField] private float reduceSpeed = 6f;

    private GameObject healthBarInstance;
    private Image healthBarFill;
    
    private float currentFill = 1f;
    private float targetFill = 1f;
    //
    private Coroutine smoothBarRoutine;

    // Llama esto cuando el enemigo se saca del pool o spawnea
    public void Initialize(Transform parent, float maxHealth)
    {

        if (healthBarPrefab == null)
        {
            Debug.LogWarning("No se asignó el prefab de la barra de vida.");
            return;
        }

        if (healthBarInstance == null)
        {
            healthBarInstance = Instantiate(
                healthBarPrefab,
                parent.position + Vector3.up * (healthBarHeight),
                Quaternion.identity,
                parent
            );
            healthBarFill = healthBarInstance.transform.Find("Background/Filled").GetComponent<Image>();
        }
        else
        {
            healthBarInstance.transform.SetParent(parent);
            healthBarInstance.transform.position = parent.position + Vector3.up * (healthBarHeight);
            healthBarInstance.SetActive(true);
            if (healthBarFill == null)
                healthBarFill = healthBarInstance.transform.Find("Background/Filled").GetComponent<Image>();
        }

        UpdateHealthBar(maxHealth, maxHealth);
        
        healthBarFill.color = new Color(0.26f, 0.54f, 0.27f, 1f);
    }

    // Llama esto cada vez que el enemigo recibe daño
    public void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        if (healthBarFill == null) return;

        targetFill = Mathf.Clamp01(currentHealth / maxHealth);

        if (smoothBarRoutine != null)
            StopCoroutine(smoothBarRoutine);

        if (gameObject.activeInHierarchy) smoothBarRoutine = StartCoroutine(SmoothFill());
        
    }

    private IEnumerator SmoothFill()
    {
        while (!Mathf.Approximately(currentFill, targetFill))
        {
            //currentFill = Mathf.MoveTowards(currentFill, targetFill, reduceSpeed * Time.deltaTime);
            currentFill = Mathf.Lerp(healthBarFill.fillAmount, targetFill, reduceSpeed * Time.deltaTime);
            healthBarFill.fillAmount = currentFill;

            //según el fill visual
            if (currentFill > 0.5f)
                healthBarFill.color = new Color(0.26f, 0.54f, 0.27f, 1f);
            else if (currentFill > 0.3f)
                healthBarFill.color = new Color(0.93f, 0.94f, 0.16f, 1f);
            else
                healthBarFill.color = new Color(0.9f, 0.1f, 0.1f, 1f);

            yield return null; // espera al siguiente frame
        }

        smoothBarRoutine = null;
    }

    public void ShowDamageText(float damageValue)
    {
        if (healthBarInstance == null) return;

        // Referencia al texto dentro de la barra
        Transform damageTextTransform = healthBarInstance.transform.Find("DamageText");
        if (damageTextTransform == null) return;

        damageTextTransform.gameObject.SetActive(true);
        var tmp = damageTextTransform.GetComponent<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.text = "-" + Mathf.RoundToInt(damageValue);
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
        }

        // Trigger del popup
        var popup = damageTextTransform.GetComponent<DamageTextPopup>();
        if (popup != null) popup.Play();
    }


    // Llama esto cuando el enemigo muere o se devuelve al pool
    public void DestroyBar()
    {
        if (healthBarInstance != null)
        {
            healthBarInstance.SetActive(false);
            UpdateHealthBar(1, 1);
        }
    }
}
