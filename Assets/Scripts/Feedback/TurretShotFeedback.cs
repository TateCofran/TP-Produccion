using UnityEngine;

[DisallowMultipleComponent]
public class TurretShotFeedback : MonoBehaviour
{
    private static readonly string[] BarrelNames = { "barrel", "cannon", "canon", "cañon", "gun", "muzzle", "cano" };

    [Header("Recoil")]
    [Tooltip("Optional. Moved back along its local Z on every shot.")]
    [SerializeField] private Transform barrel;
    [SerializeField] private bool autoFindBarrel = true;
    [SerializeField] private float recoilDistance = -0.1f;
    [SerializeField, Min(0.01f)] private float recoilDuration = 0.08f;

    [Header("Muzzle Flash")]
    [SerializeField] private bool muzzleFlash = true;
    [SerializeField] private Color muzzleColor = new Color(1f, 0.85f, 0.45f, 0.9f);
    [SerializeField, Min(0.01f)] private float muzzleSize = 0.6f;
    [SerializeField, Min(0.01f)] private float muzzleDuration = 0.06f;

    [Header("Tracer (Sniper)")]
    [SerializeField] private Color tracerColor = new Color(1f, 0.95f, 0.7f, 1f);
    [SerializeField, Min(0.001f)] private float tracerWidth = 0.05f;
    [SerializeField, Min(0.01f)] private float tracerDuration = 0.05f;

    [Header("Lightning (Tesla)")]
    [SerializeField] private Color lightningColor = new Color(0.6f, 0.85f, 1f, 1f);
    [SerializeField, Min(0.001f)] private float lightningWidth = 0.07f;
    [SerializeField, Min(0.01f)] private float lightningDuration = 0.14f;
    [SerializeField, Min(0f)] private float lightningJitter = 0.25f;
    [SerializeField, Min(2)] private int lightningSegments = 9;

    private Vector3 barrelRest;
    private float recoilStart = -999f;
    private bool recoiling;
    private bool searched;

    public static TurretShotFeedback For(Component shooter)
    {
        if (shooter == null) return null;
        if (shooter.TryGetComponent<TurretShotFeedback>(out var fb)) return fb;
        fb = shooter.GetComponentInParent<TurretShotFeedback>();
        if (fb != null) return fb;
        return shooter.gameObject.AddComponent<TurretShotFeedback>();
    }

    private void Awake()
    {
        EnsureBarrel();
    }

    private void EnsureBarrel()
    {
        if (searched) return;
        searched = true;
        if (barrel == null && autoFindBarrel)
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                string n = t.name.ToLowerInvariant();
                foreach (var key in BarrelNames)
                {
                    if (!n.Contains(key)) continue;
                    barrel = t;
                    break;
                }
                if (barrel != null) break;
            }
        }
        if (barrel != null) barrelRest = barrel.localPosition;
    }

    public void OnShot(Vector3 firePoint)
    {
        EnsureBarrel();
        if (barrel != null)
        {
            if (!recoiling) barrelRest = barrel.localPosition;
            recoilStart = Time.time;
            recoiling = true;
        }

        if (muzzleFlash) FeedbackFxPool.MuzzleFlash(firePoint, muzzleColor, muzzleSize, muzzleDuration);
    }

    public void Tracer(Vector3 from, Vector3 to)
    {
        FeedbackFxPool.Tracer(from, to, tracerColor, tracerWidth, tracerDuration);
    }

    public void Lightning(Vector3 from, Vector3 to)
    {
        FeedbackFxPool.Lightning(from, to, lightningColor, lightningWidth, lightningDuration, lightningJitter, lightningSegments);
    }

    private void Update()
    {
        if (!recoiling || barrel == null) return;

        float k = (Time.time - recoilStart) / recoilDuration;
        if (k >= 1f)
        {
            recoiling = false;
            barrel.localPosition = barrelRest;
            return;
        }

        float offset = k < 0.25f ? Mathf.Lerp(0f, recoilDistance, k / 0.25f) : Mathf.Lerp(recoilDistance, 0f, (k - 0.25f) / 0.75f);
        barrel.localPosition = barrelRest + Vector3.forward * offset;
    }

    private void OnDisable()
    {
        if (barrel != null && recoiling) barrel.localPosition = barrelRest;
        recoiling = false;
    }
}
