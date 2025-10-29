using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class WaveLightingController : MonoBehaviour
{
    [Header("Light Reference")]
    [SerializeField] private Light mainLight;

    [Header("Day-Night Colors")]
    [SerializeField] private Color dayColor = new Color(1f, 0.96f, 0.84f);
    [SerializeField] private Color sunsetColor = new Color(1f, 0.65f, 0.4f);
    [SerializeField] private Color nightColor = new Color(0.2f, 0.3f, 0.6f);

    [Header("Intensities")]
    [SerializeField] private float dayIntensity = 1.2f;
    [SerializeField] private float sunsetIntensity = 0.8f;
    [SerializeField] private float nightIntensity = 0.3f;

    [Header("Transition Settings")]
    [SerializeField] private float transitionTime = 2f;
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Coroutine transitionRoutine;

    private void Awake()
    {
        if (mainLight == null)
        {
            mainLight = RenderSettings.sun; // toma la luz direccional del scene settings
        }
    }

    private void OnEnable()
    {
        if (WaveManager.Instance != null)
            WaveManager.Instance.OnWaveStarted += HandleWaveStarted;
    }

    private void OnDisable()
    {
        if (WaveManager.Instance != null)
            WaveManager.Instance.OnWaveStarted -= HandleWaveStarted;
    }

    private void HandleWaveStarted(int waveNumber, int _)
    {
        var (targetColor, targetIntensity) = GetLightingForWave(waveNumber);
        if (transitionRoutine != null) StopCoroutine(transitionRoutine);
        transitionRoutine = StartCoroutine(TransitionLight(targetColor, targetIntensity));
    }

    private (Color, float) GetLightingForWave(int wave)
    {
        if (wave <= 15)
            return (dayColor, dayIntensity);
        if (wave <= 30)
            return (sunsetColor, sunsetIntensity);
        return (nightColor, nightIntensity);
    }

    private IEnumerator TransitionLight(Color targetColor, float targetIntensity)
    {
        if (mainLight == null) yield break;

        Color startColor = mainLight.color;
        float startIntensity = mainLight.intensity;
        float elapsed = 0f;

        while (elapsed < transitionTime)
        {
            elapsed += Time.deltaTime;
            float t = fadeCurve.Evaluate(elapsed / transitionTime);

            mainLight.color = Color.Lerp(startColor, targetColor, t);
            mainLight.intensity = Mathf.Lerp(startIntensity, targetIntensity, t);

            yield return null;
        }

        mainLight.color = targetColor;
        mainLight.intensity = targetIntensity;
    }
}
