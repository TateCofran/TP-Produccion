using UnityEngine;

public class ShiftingWorldMechanic : MonoBehaviour
{
    public enum World { Normal, Otro }

    [Header("Refs")]
    [SerializeField] private GridGenerator grid;
    [SerializeField] private ShiftingWorldUI ui;  
    [SerializeField] private ShiftingWorldAudio shiftingWorldAudio;
    [SerializeField] private FogWorldMaterialSwitcher fogWorld;

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
    private float toggleCooldownLeft = 0f;

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
        if (fogWorld != null)
        {
            bool startInOther = (currentWorld == World.Otro);
            isOtherWorld = startInOther;
            fogWorld.SetWorld(startInOther);
        }
    }

    private void Update()
    {

        if (toggleCooldownLeft > 0f)
        {
            toggleCooldownLeft = Mathf.Max(0f, toggleCooldownLeft - Time.deltaTime);
        }

        if (ui != null)
        {
            float cooldownNormalized = (toggleCooldownSeconds <= 0f)
                ? 1f
                : 1f - (toggleCooldownLeft / toggleCooldownSeconds); // 1 = listo
            ui.SetWorldToggleCooldown(cooldownNormalized);
        }
        if (Input.GetKeyDown(toggleKey) && toggleCooldownLeft <= 0f)
        {
            if (TutorialManager.Instance != null && !TutorialManager.Instance.AllowWorldSwitch)
                return;

            currentWorld = (currentWorld == World.Normal) ? World.Otro : World.Normal;
            isOtherWorld = (currentWorld == World.Otro);

            if (shiftingWorldAudio != null)
                shiftingWorldAudio.PlayWorldSound(isOtherWorld);

            if (fogWorld != null)
                fogWorld.SetWorld(isOtherWorld);

            if (ui != null)
                ui.SetWorldIcon(currentWorld);

            toggleCooldownLeft = Mathf.Max(0.01f, toggleCooldownSeconds);

            Debug.Log($"[ShiftingWorldMechanic] Cambié de mundo → {currentWorld}");

            TutorialEventHub.RaiseWorldSwitched();
        }

        float dt = Time.deltaTime;
        float activeDelta = speedPerSecond * dt;
        float passiveDelta = speedPerSecond * passiveMultiplier * dt;

        float extraEnergyPerSecond = WorldEnergyRegistry.GetTotalChargePerSecond();
        float extraDelta = extraEnergyPerSecond * dt;


        if (currentWorld == World.Normal)
        {
            if (!normalWaitingChoice)
                normalProgress = Mathf.Min(100f, normalProgress + activeDelta + extraDelta);
            if (!otherWaitingChoice)
                otherProgress = Mathf.Min(100f, otherProgress + passiveDelta);
        }
        else
        {
            if (!otherWaitingChoice)
                otherProgress = Mathf.Min(100f, otherProgress + activeDelta + extraDelta);
            if (!normalWaitingChoice)
                normalProgress = Mathf.Min(100f, normalProgress + passiveDelta);
        }

        if (ui != null)
            ui.SetWorldProgress(normalProgress / 100f, otherProgress / 100f);

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
