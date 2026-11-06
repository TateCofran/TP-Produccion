using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    public const string OptionPrefKey = "Options_CameraShake";

    [Header("Overrides")]
    [Tooltip("Extra multiplier on top of FeedbackSettings.shakeIntensity.")]
    [SerializeField, Range(0f, 2f)] private float localMultiplier = 1f;

    private Camera cam;
    private float trauma;
    private float seed;
    private Vector3 appliedPosition;
    private Quaternion appliedRotation = Quaternion.identity;
    private bool applied;

    public float Trauma => trauma;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        cam = GetComponent<Camera>();
        seed = Random.value * 100f;
    }

    private void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += HandleBeginCamera;
        RenderPipelineManager.endCameraRendering += HandleEndCamera;
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= HandleBeginCamera;
        RenderPipelineManager.endCameraRendering -= HandleEndCamera;
        Restore();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public static void AddTrauma(float amount)
    {
        if (Instance != null) Instance.Add(amount);
    }

    public void Add(float amount)
    {
        trauma = Mathf.Clamp01(trauma + Mathf.Max(0f, amount));
    }

    public static float OptionMultiplier => Mathf.Clamp01(PlayerPrefs.GetFloat(OptionPrefKey, 1f));

    private void Update()
    {
        if (trauma <= 0f) return;
        trauma = Mathf.Max(0f, trauma - FeedbackSettings.Current.shakeDecay * Time.unscaledDeltaTime);
    }

    private void HandleBeginCamera(ScriptableRenderContext context, Camera renderingCamera)
    {
        if (renderingCamera != cam || trauma <= 0f) return;

        var settings = FeedbackSettings.Current;
        float strength = trauma * trauma * settings.shakeIntensity * localMultiplier * OptionMultiplier;
        if (strength <= 0f) return;

        float t = Time.unscaledTime * settings.shakeFrequency;
        float x = (Mathf.PerlinNoise(seed, t) * 2f - 1f) * settings.shakeMaxOffset * strength;
        float y = (Mathf.PerlinNoise(seed + 10f, t) * 2f - 1f) * settings.shakeMaxOffset * strength;
        float roll = (Mathf.PerlinNoise(seed + 20f, t) * 2f - 1f) * settings.shakeMaxAngle * strength;

        appliedPosition = transform.right * x + transform.up * y;
        appliedRotation = Quaternion.AngleAxis(roll, transform.forward);
        transform.position += appliedPosition;
        transform.rotation = appliedRotation * transform.rotation;
        applied = true;
    }

    private void HandleEndCamera(ScriptableRenderContext context, Camera renderingCamera)
    {
        if (renderingCamera != cam) return;
        Restore();
    }

    private void Restore()
    {
        if (!applied) return;
        transform.rotation = Quaternion.Inverse(appliedRotation) * transform.rotation;
        transform.position -= appliedPosition;
        applied = false;
    }
}
