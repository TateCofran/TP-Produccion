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
    // Nuevo overlay para el escudo
    private Image shieldOverlayFill;
    private float shieldAlpha = 0.35f; // opacidad del overlay (puede ajustarse)


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

    public void SetShieldOverlay(float normalizedValue)
    {
        // 1) crear overlay si no existe todavía
        if (shieldOverlayFill == null)
        {
            var bg = healthBarInstance?.transform.Find("Background");
            if (bg != null)
            {
                // Creamos un duplicado del "Filled" pero más transparente
                var overlayGO = new GameObject("ShieldOverlay", typeof(Image));
                overlayGO.transform.SetParent(bg, false);
                overlayGO.transform.SetSiblingIndex(1); // encima del fondo, debajo del fill real

                shieldOverlayFill = overlayGO.GetComponent<Image>();
                shieldOverlayFill.color = new Color(0f, 0.8f, 1f, shieldAlpha); // azul celeste translúcido
                shieldOverlayFill.type = Image.Type.Filled;
                shieldOverlayFill.fillMethod = Image.FillMethod.Horizontal;
                shieldOverlayFill.raycastTarget = false;
            }
        }

        // 2) aplicar valor (0–1)
        if (shieldOverlayFill != null)
        {
            shieldOverlayFill.fillAmount = Mathf.Clamp01(normalizedValue);

            // si no hay escudo, esconder
            shieldOverlayFill.enabled = (normalizedValue > 0.001f);
        }
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

    // Hace que la barra SIEMPRE mire a la cámara
    private void LateUpdate()
    {
        if (healthBarInstance == null) return;
        if (Camera.main == null) return;

        Transform cam = Camera.main.transform;

        // Hacemos que la barra mire hacia la cámara
        Vector3 dir = (healthBarInstance.transform.position - cam.position).normalized;
        healthBarInstance.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
    }
}
