using UnityEngine;

public class ShiftingWorldMechanic : MonoBehaviour
{
    public enum World { Normal, Other }

    [Header("Refs")]
    [SerializeField] private GridGenerator grid;
    [SerializeField] private ShiftingWorldUI ui;
    [SerializeField] private ShiftingWorldAudio shiftingWorldAudio;
    [SerializeField] private FogWorldMaterialSwitcher fogWorld;

    [Header("State")]
    [SerializeField] private World currentWorld = World.Normal;

    [Header("Progress (0..100)")]
    [SerializeField, Range(0f, 100f)] private float normalProgress = 0f;
    [SerializeField, Range(0f, 100f)] private float otherProgress = 0f;

    [Header("Speed Settings")]
    [SerializeField] private float speedPerSecond = 20f;
    [SerializeField, Range(0f, 1f)] private float passiveMultiplier = 0.25f;

    [Header("World Switch")]
    [SerializeField] private KeyCode toggleKey = KeyCode.J;
    [SerializeField, Tooltip("Cooldown in seconds before the world can be switched again")]
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
        WorldState.Initialize(currentWorld);

        if (fogWorld != null)
        {
            bool startInOther = (currentWorld == World.Other);
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
                : 1f - (toggleCooldownLeft / toggleCooldownSeconds);
            ui.SetWorldToggleCooldown(cooldownNormalized);
        }
        if (!GameTime.IsPaused && Input.GetKeyDown(toggleKey) && toggleCooldownLeft <= 0f)
        {
            if (TutorialManager.Instance != null && !TutorialManager.Instance.AllowWorldSwitch)
                return;

            currentWorld = (currentWorld == World.Normal) ? World.Other : World.Normal;
            isOtherWorld = (currentWorld == World.Other);
            WorldState.Set(currentWorld);

            if (shiftingWorldAudio != null)
                shiftingWorldAudio.PlayWorldSound(isOtherWorld);

            if (fogWorld != null)
                fogWorld.SetWorld(isOtherWorld);

            if (ui != null)
                ui.SetWorldIcon(currentWorld);

            toggleCooldownLeft = Mathf.Max(0.01f, toggleCooldownSeconds);

            Debug.Log($"[ShiftingWorldMechanic] Switched world to {currentWorld}");

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
                    normalProgress = 0f;
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
                    otherProgress = 0f;
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
        Debug.Log($"[ShiftingWorldMechanic] {turret.displayName} reached level {levelData.currentLevel}");
    }

    private void HandleTurretChosen(TurretDataSO so) {  }

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

    public int GetNormalInt() => Mathf.Min(100, Mathf.FloorToInt(normalProgress));
    public int GetOtherInt() => Mathf.Min(100, Mathf.FloorToInt(otherProgress));
    public ShiftingWorldMechanic.World GetCurrentWorld()
    {
        return currentWorld;
    }
}
