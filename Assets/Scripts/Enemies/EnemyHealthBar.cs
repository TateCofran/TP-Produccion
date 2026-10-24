using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class EnemyHealthBar : MonoBehaviour, IHealthDisplay
{
    [Header("Prefabs & Refs")]
    [SerializeField] private GameObject healthBarPrefab;
    [SerializeField] private float healthBarHeight = 1.5f;

    [SerializeField] private float damageTextYOffset = 2f;
    [SerializeField] private float damageTextXOffset = -30f;

    [SerializeField] private float reduceSpeed = 6f;

    [Header("Health Bar Height")]
    [FormerlySerializedAs("usarAlturaDesdeCollider")]
    [SerializeField] private bool useColliderHeight = true;
    [FormerlySerializedAs("extraAltura")]
    [SerializeField] private float extraHeight = 0.5f;

    private GameObject healthBarInstance;
    private Image healthBarFill;

    private float currentFill = 1f;
    private float targetFill = 1f;
    private Coroutine smoothBarRoutine;

    private Image shieldOverlayFill;
    private float shieldAlpha = 0.35f;

    public void Initialize(Transform parent, float maxHealth)
    {
        if (healthBarPrefab == null)
        {
            Debug.LogWarning("[EnemyHealthBar] Health bar prefab is not assigned");
            return;
        }

        float yOffset = healthBarHeight;

        if (useColliderHeight && parent != null)
        {
            if (parent.TryGetComponent<Collider>(out var col))
            {
                yOffset = col.bounds.extents.y + extraHeight;
            }
            else if (parent.TryGetComponent<Renderer>(out var rend))
            {
                yOffset = rend.bounds.extents.y + extraHeight;
            }
            else
            {
                yOffset = healthBarHeight + extraHeight;
            }
        }
        else
        {
            yOffset = healthBarHeight + extraHeight;
        }

        Vector3 barPosition = parent.position + Vector3.up * yOffset;

        if (healthBarInstance == null)
        {
            healthBarInstance = Instantiate(
                healthBarPrefab,
                barPosition,
                Quaternion.identity,
                parent
            );
            healthBarFill = healthBarInstance.transform.Find("Background/Filled").GetComponent<Image>();
        }
        else
        {
            healthBarInstance.transform.SetParent(parent);
            healthBarInstance.transform.position = barPosition;
            healthBarInstance.SetActive(true);

            if (healthBarFill == null)
                healthBarFill = healthBarInstance.transform.Find("Background/Filled").GetComponent<Image>();
        }

        UpdateHealthBar(maxHealth, maxHealth);

        healthBarFill.color = new Color(0.26f, 0.54f, 0.27f, 1f);
    }

    public void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        if (healthBarFill == null) return;

        targetFill = Mathf.Clamp01(currentHealth / maxHealth);

        if (smoothBarRoutine != null)
            StopCoroutine(smoothBarRoutine);

        if (gameObject.activeInHierarchy)
            smoothBarRoutine = StartCoroutine(SmoothFill());
    }

    private IEnumerator SmoothFill()
    {
        while (!Mathf.Approximately(currentFill, targetFill))
        {
            currentFill = Mathf.Lerp(healthBarFill.fillAmount, targetFill, reduceSpeed * Time.deltaTime);
            healthBarFill.fillAmount = currentFill;

            if (currentFill > 0.5f)
                healthBarFill.color = new Color(0.26f, 0.54f, 0.27f, 1f);
            else if (currentFill > 0.3f)
                healthBarFill.color = new Color(0.93f, 0.94f, 0.16f, 1f);
            else
                healthBarFill.color = new Color(0.9f, 0.1f, 0.1f, 1f);

            yield return null;
        }

        smoothBarRoutine = null;
    }

    public void ShowDamageText(float damageValue)
    {
        if (healthBarInstance == null) return;

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

        var popup = damageTextTransform.GetComponent<DamageTextPopup>();
        if (popup != null) popup.Play();
    }

    public void SetShieldOverlay(float normalizedValue)
    {
        if (shieldOverlayFill == null)
        {
            var bg = healthBarInstance?.transform.Find("Background");
            if (bg != null)
            {
                var overlayGO = new GameObject("ShieldOverlay", typeof(Image));
                overlayGO.transform.SetParent(bg, false);
                overlayGO.transform.SetSiblingIndex(1);

                shieldOverlayFill = overlayGO.GetComponent<Image>();
                shieldOverlayFill.color = new Color(0f, 0.8f, 1f, shieldAlpha);
                shieldOverlayFill.type = Image.Type.Filled;
                shieldOverlayFill.fillMethod = Image.FillMethod.Horizontal;
                shieldOverlayFill.raycastTarget = false;
            }
        }

        if (shieldOverlayFill != null)
        {
            shieldOverlayFill.fillAmount = Mathf.Clamp01(normalizedValue);

            shieldOverlayFill.enabled = (normalizedValue > 0.001f);
        }
    }

    public void DestroyBar()
    {
        if (healthBarInstance != null)
        {
            healthBarInstance.SetActive(false);
            UpdateHealthBar(1, 1);
        }
    }

    private void LateUpdate()
    {
        if (healthBarInstance == null) return;
        if (Camera.main == null) return;

        Transform cam = Camera.main.transform;

        Vector3 dir = (healthBarInstance.transform.position - cam.position).normalized;
        healthBarInstance.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
    }
}
