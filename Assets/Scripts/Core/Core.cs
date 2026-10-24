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
    [Tooltip("Volume that contains the Vignette override (Global Volume).")]
    [SerializeField] private Volume damageVolume;
    [Tooltip("Maximum vignette intensity when taking damage.")]
    [SerializeField] private float hitVignetteIntensity = 0.45f;
    [Tooltip("Speed at which the vignette fades out.")]
    [SerializeField] private float vignetteFadeSpeed = 4f;

    private Vignette vignette;
    private Coroutine vignetteCoroutine;

    public static Core Active { get; private set; }

    private void OnEnable()
    {
        Active = this;
    }

    private void OnDisable()
    {
        if (Active == this) Active = null;
    }

    private void Start()
    {
        currentHealth = maxHealth;
        UIController.Instance?.UpdateCoreHealth(currentHealth, maxHealth);

        if (damageVolume != null && damageVolume.profile != null)
        {
            if (damageVolume.profile.TryGet(out vignette))
            {
                vignette.intensity.value = 0f;
                vignette.color.value = Color.red;
            }
            else
            {
                Debug.LogWarning("[Core] The assigned Volume has no Vignette override");
            }
        }
        else
        {
            Debug.LogWarning("[Core] No Volume assigned for the damage effect");
        }
    }

    public void SetInfiniteHealth(bool enabled)
    {
        infiniteHealth = enabled;
        currentHealth = maxHealth;
        UIController.Instance?.UpdateCoreHealth(currentHealth, maxHealth);
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

        UIController.Instance?.UpdateCoreHealth(currentHealth, maxHealth);

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
