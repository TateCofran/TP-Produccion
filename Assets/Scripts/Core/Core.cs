using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class Core : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 10;
    private int currentHealth;

    [Header("Debug / Cheats")]
    [SerializeField] private bool infiniteHealth = false;
    public bool InfiniteHealth => infiniteHealth;

    [Header("Screen Shake")]
    [SerializeField] private CinemachineImpulseSource impulseSource;

    [Header("Post Process (Vignette)")]
    [Tooltip("Volume que tiene el efecto de Vignette (Global Volume).")]
    [SerializeField] private Volume damageVolume;
    [Tooltip("Intensidad máxima del vignette al recibir daño.")]
    [SerializeField] private float hitVignetteIntensity = 0.45f;
    [Tooltip("Velocidad a la que se desvanece el vignette.")]
    [SerializeField] private float vignetteFadeSpeed = 4f;

    private Vignette vignette;
    private Coroutine vignetteCoroutine;

    private void Start()
    {
        currentHealth = maxHealth;
        UIController.Instance.UpdateCoreHealth(currentHealth, maxHealth);

        // Cachear el Vignette del Volume
        if (damageVolume != null && damageVolume.profile != null)
        {
            if (damageVolume.profile.TryGet(out vignette))
            {
                // Aseguramos que arranque apagado y rojo
                vignette.intensity.value = 0f;
                vignette.color.value = Color.red;
            }
            else
            {
                Debug.LogWarning("Core: el Volume asignado no tiene un Vignette override.");
            }
        }
        else
        {
            Debug.LogWarning("Core: no hay Volume asignado para el efecto de daño.");
        }
    }

    public void SetInfiniteHealth(bool enabled)
    {
        infiniteHealth = enabled;
        currentHealth = maxHealth;
        UIController.Instance.UpdateCoreHealth(currentHealth, maxHealth);
    }

    public void TakeDamage(int amount)
    {
        if (!infiniteHealth)
        {
            currentHealth -= amount;
            if (currentHealth < 0) currentHealth = 0;
        }
        else
        {
            currentHealth = maxHealth;
        }

        UIController.Instance.UpdateCoreHealth(currentHealth, maxHealth);

        if (impulseSource != null)
        {
            impulseSource.GenerateImpulse();
        }

        TutorialEventHub.RaiseCoreDamaged();

        if (vignette != null)
        {
            if (vignetteCoroutine != null)
                StopCoroutine(vignetteCoroutine);

            vignetteCoroutine = StartCoroutine(HitVignetteRoutine());
        }

        if (!infiniteHealth && currentHealth <= 0)
        {
            Debug.Log("Core destroyed!");
            if (GameManager.Instance != null)
                GameManager.Instance.GameOver();
        }
    }

    private IEnumerator HitVignetteRoutine()
    {
        vignette.intensity.value = hitVignetteIntensity;

        while (vignette.intensity.value > 0f)
        {
            float newValue = Mathf.MoveTowards(
                vignette.intensity.value,
                0f,
                vignetteFadeSpeed * Time.deltaTime
            );
            vignette.intensity.value = newValue;

            yield return null;
        }

        vignette.intensity.value = 0f;
        vignetteCoroutine = null;
    }
}
