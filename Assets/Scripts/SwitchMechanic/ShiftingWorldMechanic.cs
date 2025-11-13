using UnityEngine;

public class ShiftingWorldMechanic : MonoBehaviour
{
    public enum World { Normal, Otro }

    [Header("Refs")]
    [SerializeField] private GridGenerator grid;
    [SerializeField] private ShiftingWorldUI ui;   // ← La UI mostrará barras
    [SerializeField] private ShiftingWorldAudio shiftingWorldAudio;
    [SerializeField] private FogWorldMaterialSwitcher fogWorld; // ← NUEVO

    [Header("Estado")]
    [SerializeField] private World currentWorld = World.Normal;

    [Header("Progreso (0..100)")]
    [SerializeField, Range(0f, 100f)] private float normalProgress = 0f;
    [SerializeField, Range(0f, 100f)] private float otherProgress = 0f;

    [Header("Ajustes de velocidad")]
    [SerializeField] private float speedPerSecond = 20f;
    [SerializeField, Range(0f, 1f)] private float passiveMultiplier = 0.25f;

    [Header("Cambio de mundo")]
    [SerializeField] private KeyCode toggleKey = KeyCode.J;
    [SerializeField, Tooltip("Segundos de cooldown para volver a cambiar de mundo")]
    private float toggleCooldownSeconds = 5f;
    private float toggleCooldownLeft = 0f; // 0 = listo para cambiar

    // Flags de panel/espera (se mantienen como antes)
    private bool normalPanelOpen = false;
    private bool otherPanelOpen = false;
    private bool normalWaitingChoice = false;
    private bool otherWaitingChoice = false;
    private bool isOtherWorld = false;

    public World CurrentWorld => currentWorld;

    private void Awake()
    {
        if (ui != null)
        {
            ui.OnTurretChosen += HandleTurretChosen;
            ui.OnTurretLevelUp += HandleTurretLevelUp;
            ui.OnTurretPlacedSuccessfully += HandleTurretPlacedSuccessfully;
        }
    }

    private void Start()
    {
        // Sincronizar Fog al estado inicial del mundo
        if (fogWorld != null)
        {
            bool startInOther = (currentWorld == World.Otro);
            isOtherWorld = startInOther;
            fogWorld.SetWorld(startInOther);
        }
    }

    private void Update()
    {
        // ↓↓↓ COOLDOWN del cambio de mundo ↓↓↓
        if (toggleCooldownLeft > 0f)
        {
            toggleCooldownLeft = Mathf.Max(0f, toggleCooldownLeft - Time.deltaTime);
        }

        // (UI) mostrar progreso de cooldown como “recarga” 0→1
        if (ui != null)
        {
            float cooldownNormalized = (toggleCooldownSeconds <= 0f)
                ? 1f
                : 1f - (toggleCooldownLeft / toggleCooldownSeconds); // 1 = listo
            ui.SetWorldToggleCooldown(cooldownNormalized);
        }

        // Cambio de mundo (solo si cooldown terminó)
        if (Input.GetKeyDown(toggleKey) && toggleCooldownLeft <= 0f)
        {
            currentWorld = (currentWorld == World.Normal) ? World.Otro : World.Normal;

            isOtherWorld = (currentWorld == World.Otro);

            // sonido de cambio de mundo
            if (shiftingWorldAudio != null)
                shiftingWorldAudio.PlayWorldSound(isOtherWorld);

            // avisar al Fog
            if (fogWorld != null)
                fogWorld.SetWorld(isOtherWorld);

            toggleCooldownLeft = Mathf.Max(0.01f, toggleCooldownSeconds); // arranca cooldown
            Debug.Log($"[ShiftingWorldMechanic] Cambié de mundo → {currentWorld}. Cooldown: {toggleCooldownSeconds:0.##}s");

            if (ui != null)
                ui.SetWorldIcon(currentWorld);

            // NOTA: no se resetean progresos ni se cierran paneles.
        }

        // Cálculo de deltas
        float dt = Time.deltaTime;
        float activeDelta = speedPerSecond * dt;
        float passiveDelta = speedPerSecond * passiveMultiplier * dt;

        // Ambos mundos suben siempre (activo normal, inactivo reducido)
        if (currentWorld == World.Normal)
        {
            if (!normalWaitingChoice)
                normalProgress = Mathf.Min(100f, normalProgress + activeDelta);
            if (!otherWaitingChoice)
                otherProgress = Mathf.Min(100f, otherProgress + passiveDelta);
        }
        else
        {
            if (!otherWaitingChoice)
                otherProgress = Mathf.Min(100f, otherProgress + activeDelta);
            if (!normalWaitingChoice)
                normalProgress = Mathf.Min(100f, normalProgress + passiveDelta);
        }

        // (UI) actualizar barras de progreso (0..1)
        if (ui != null)
            ui.SetWorldProgress(normalProgress / 100f, otherProgress / 100f);

        // Chequeos de thresholds (como ya lo tenías)
        TryFireNormalReached();
        TryFireOtherReached();
    }

    private void TryFireNormalReached()
    {
        if (normalWaitingChoice || normalPanelOpen) return;
        if (Mathf.FloorToInt(normalProgress) >= 100)
        {
            normalWaitingChoice = true;
            normalPanelOpen = true;
            if (ui != null)
            {
                ui.ShowNormalReached(() =>
                {
                    normalPanelOpen = false;
                    normalWaitingChoice = false;
                    normalProgress = 0f; // reset tras elegir/colocar tile
                });
            }
        }
    }

    private void TryFireOtherReached()
    {
        if (otherWaitingChoice || otherPanelOpen) return;
        if (Mathf.FloorToInt(otherProgress) >= 100)
        {
            otherWaitingChoice = true;
            otherPanelOpen = true;
            if (ui != null)
            {
                ui.ShowOtherReached(() =>
                {
                    otherPanelOpen = false;
                    otherWaitingChoice = false;
                    otherProgress = 0f; // reset tras colocar torreta
                });
            }
        }
    }

    private void OnDestroy()
    {
        if (ui != null)
        {
            ui.OnTurretChosen -= HandleTurretChosen;
            ui.OnTurretLevelUp -= HandleTurretLevelUp;
            ui.OnTurretPlacedSuccessfully -= HandleTurretPlacedSuccessfully;
        }
    }

    private void HandleTurretLevelUp(TurretDataSO turret, TurretLevelData levelData)
    {
        Debug.Log($"[ShiftingWorldMechanic] {turret.displayName} subió al nivel {levelData.currentLevel}!");
    }

    private void HandleTurretChosen(TurretDataSO so) { /* noop visual/log si querés */ }

    private void HandleTurretPlacedSuccessfully(World world)
    {
        if (world == World.Normal)
        {
            normalProgress = 0f;
            normalWaitingChoice = false;
            normalPanelOpen = false;
        }
        else
        {
            otherProgress = 0f;
            otherWaitingChoice = false;
            otherPanelOpen = false;
        }
    }

    // ==== Helpers para la UI (si querés exponer ints también) ====
    public int GetNormalInt() => Mathf.Min(100, Mathf.FloorToInt(normalProgress));
    public int GetOtherInt() => Mathf.Min(100, Mathf.FloorToInt(otherProgress));
    public ShiftingWorldMechanic.World GetCurrentWorld()
    {
        return currentWorld; // asumiendo que tu campo se llama 'currentWorld'
    }
}
