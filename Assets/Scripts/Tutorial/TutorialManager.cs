using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    [Header("UI")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descText;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button skipButton;

    [Header("Config")]
    [SerializeField] private bool autoStart = true;

    [Header("Animations")]
    [SerializeField] private Animator panelAnimator;
    [SerializeField] private Animator dimmerAnimator;

    private static readonly int PanelVisible = Animator.StringToHash("Visible");
    private static readonly int DimmerActive = Animator.StringToHash("Active");

    private int currentStep = -1;
    private bool running = false;

    private TutorialStep[] steps;

    [SerializeField] private string mainMenuSceneName = "Menu";

    public int CurrentStepIndex => currentStep;

    public TutorialEventType CurrentStepType
    {
        get
        {
            if (steps == null || currentStep < 0 || currentStep >= steps.Length)
                return TutorialEventType.Manual;
            return steps[currentStep].eventType;
        }
    }
    public bool IsCurrentStep(TutorialEventType type)
    {
        if (!running) return false;
        if (steps == null || currentStep < 0 || currentStep >= steps.Length) return false;
        return steps[currentStep].eventType == type;
    }
    public event Action<int, TutorialStep> OnStepChanged;

    #region Action Locks for Other Systems
    public bool AllowPlaceTiles { get; private set; } = true;
    public bool AllowPlaceTurrets { get; private set; } = true;
    public bool AllowStartWave { get; private set; } = true;
    public bool AllowWorldSwitch { get; private set; } = true;
    #endregion

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        steps = BuildSteps();

        if (skipButton != null)
            skipButton.onClick.AddListener(SkipTutorial);

        if (continueButton != null)
            continueButton.onClick.AddListener(NextStep);
    }

    private void OnEnable()
    {
        TutorialEventHub.TilePlaced += HandleTilePlaced;
        TutorialEventHub.TurretPlaced += HandleTurretPlaced;
        TutorialEventHub.WaveStarted += HandleWaveStarted;
        TutorialEventHub.CoreDamaged += HandleCoreDamaged;
        TutorialEventHub.WorldSwitched += HandleWorldSwitched;
        TutorialEventHub.WaveCompleted += HandleWaveCompleted;
    }

    private void OnDisable()
    {
        TutorialEventHub.TilePlaced -= HandleTilePlaced;
        TutorialEventHub.TurretPlaced -= HandleTurretPlaced;
        TutorialEventHub.WaveStarted -= HandleWaveStarted;
        TutorialEventHub.CoreDamaged -= HandleCoreDamaged;
        TutorialEventHub.WorldSwitched -= HandleWorldSwitched;
        TutorialEventHub.WaveCompleted -= HandleWaveCompleted;
    }

    private void Start()
    {
        bool shouldRunTutorial =
            TutorialProgress.ForceTutorialThisRun ||
            !TutorialProgress.IsCompleted;

        if (autoStart && shouldRunTutorial)
        {
            StartTutorial();
        }
        else
        {
            running = false;
            panelRoot.SetActive(false);

            AllowPlaceTiles = true;
            AllowPlaceTurrets = true;
            AllowStartWave = true;
            AllowWorldSwitch = true;
        }

        TutorialProgress.ForceTutorialThisRun = false;
    }

    private TutorialStep[] BuildSteps()
    {
        return new TutorialStep[]
        {
            new TutorialStep(
                "Welcome",
                "I will teach you how to defend the core.",
                TutorialEventType.Manual,
                allowTiles:false, allowTurrets:false, allowWave:false, allowWorld:false
            ),
            new TutorialStep(
                "Camera Controls",
                "Use WASD (or the arrow keys) to move the camera.\n" +
                "Use the MOUSE WHEEL to zoom in and out.\n\n" +
                "Try moving and zooming, and press 'Continue' when you are ready.",
                TutorialEventType.Manual,
                allowTiles:false, allowTurrets:false, allowWave:false, allowWorld:false
            ),
            new TutorialStep(
                "Place Your First Tile",
                "Choose a tile and place a path.",
                TutorialEventType.TilePlaced,
                allowTiles:true, allowTurrets:false, allowWave:false, allowWorld:false
            ),

            new TutorialStep(
                "Place A Turret",
                "Select a turret and place it on a free cell.",
                TutorialEventType.TurretPlaced,
                allowTiles:false, allowTurrets:true, allowWave:false, allowWorld:false
            ),

            new TutorialStep(
                "Start The Wave",
                "Placing a turret starts the wave automatically.",
                TutorialEventType.WaveStarted,
                allowTiles:false, allowTurrets:false, allowWave:true, allowWorld:false
            ),

            new TutorialStep(
                "Defend The Core",
                "If the core reaches 0 you lose. Defend it!",
                TutorialEventType.Manual,
                allowTiles:true, allowTurrets:true, allowWave:true, allowWorld:true
            ),

            new TutorialStep(
                "World Switch",
                "You can switch between the Normal World and the Other World with the J key.\n",
                TutorialEventType.Manual,
                allowTiles:true, allowTurrets:true, allowWave:true, allowWorld:false
            ),

            new TutorialStep(
                "Try Switching Worlds",
                "Press J to switch worlds now.",
                TutorialEventType.WorldSwitched,
                allowTiles:true, allowTurrets:true, allowWave:true, allowWorld:true
            ),

            new TutorialStep(
                "Essences Per World Switch",
                "Every time you switch worlds you earn essences.\n" +
                "Essences are spent on permanent upgrades in the Lab and the Workshop.\n",
                TutorialEventType.Manual,
                allowTiles:true, allowTurrets:true, allowWave:true, allowWorld:true
            ),
            new TutorialStep(
                "Survive A Wave",
                "Defend your core until the current wave ends.",
                TutorialEventType.WaveCompleted,
                allowTiles:true, allowTurrets:true, allowWave:true, allowWorld:true
            ),
        };
    }

    public void StartTutorial()
    {
        running = true;
        currentStep = -1;
        NextStep();
    }

    public void SkipTutorial()
    {
        running = false;
        HidePanel();

        AllowPlaceTiles = true;
        AllowPlaceTurrets = true;
        AllowStartWave = true;
        AllowWorldSwitch = true;

        TutorialProgress.MarkCompleted();
    }

    public void NextStep()
    {
        currentStep++;

        if (currentStep >= steps.Length)
        {
            FinishTutorial();
            return;
        }

        ApplyStep(steps[currentStep]);
    }

    private void FinishTutorial()
    {
        running = false;
        HidePanel();

        AllowPlaceTiles = true;
        AllowPlaceTurrets = true;
        AllowStartWave = true;
        AllowWorldSwitch = true;

        Debug.Log("[Tutorial] COMPLETADO.");
    }

    private void ApplyStep(TutorialStep s)
    {
        if (!panelRoot) return;

        ShowPanel();

        if (titleText != null)
            titleText.text = s.title;
        if (descText != null)
            descText.text = s.description;

        AllowPlaceTiles = s.allowTiles;
        AllowPlaceTurrets = s.allowTurrets;
        AllowStartWave = s.allowWave;
        AllowWorldSwitch = s.allowWorld;

        if (continueButton != null)
            continueButton.gameObject.SetActive(s.eventType == TutorialEventType.Manual);

        OnStepChanged?.Invoke(currentStep, s);
        if (s.eventType == TutorialEventType.TilePlaced)
        {
            var swUI = FindFirstObjectByType<ShiftingWorldUI>();
            if (swUI != null)
                swUI.ForceOpenTilePanel();
        }
        if (s.eventType == TutorialEventType.WaveCompleted)
        {
            StartCoroutine(AutoHideStepMessage());
        }
    }
    private IEnumerator AutoHideStepMessage()
    {
        yield return new WaitForSecondsRealtime(3f);

        HidePanel();
    }

    private void HandleTilePlaced()
    {
        CheckEvent(TutorialEventType.TilePlaced);
    }

    private void HandleTurretPlaced()
    {
        CheckEvent(TutorialEventType.TurretPlaced);
    }

    private void HandleWaveStarted()
    {
        CheckEvent(TutorialEventType.WaveStarted);
    }

    private void HandleCoreDamaged()
    {
        CheckEvent(TutorialEventType.CoreDamaged);
    }

    private void HandleWorldSwitched()
    {
        CheckEvent(TutorialEventType.WorldSwitched);
    }
    private void HandleWaveCompleted()
    {
        CheckEvent(TutorialEventType.WaveCompleted);
    }
    public void NotifyCoreDamaged()
    {
        CheckEvent(TutorialEventType.CoreDamaged);
    }

    private void CheckEvent(TutorialEventType evt)
    {
        if (!running) return;
        if (currentStep < 0 || currentStep >= steps.Length) return;

        var s = steps[currentStep];
        if (s.eventType != evt)
            return;

        if (evt == TutorialEventType.WaveCompleted && currentStep == steps.Length - 1)
        {
            HandleFinalTutorialReward();
        }
        else
        {
            NextStep();
        }
    }
    private void HandleFinalTutorialReward()
    {
        running = false;

        ShowPanel();

        bool isFirstTime = !TutorialProgress.IsCompleted;

        TutorialProgress.MarkCompleted();

        if (isFirstTime)
        {
            if (titleText != null)
                titleText.text = "Tutorial completed!";
            if (descText != null)
                descText.text =
                    "You survived the wave.\n\n" +
                    "You earned <b>10 Normal World essences</b>.\n" +
                    "Returning to the menu...";

            EssenceBank.Add(10, 0);
        }
        else
        {
            if (titleText != null)
                titleText.text = "Tutorial completed";
            if (descText != null)
                descText.text =
                    "Well done!\n" +
                    "Returning to the menu...";
        }

        if (continueButton != null) continueButton.gameObject.SetActive(false);
        if (skipButton != null) skipButton.gameObject.SetActive(false);

        StartCoroutine(ReturnToMenuAfterDelay(3f));
    }
    public void ForceStartTutorial()
    {
        running = true;
        currentStep = -1;

        panelRoot.SetActive(false);
        NextStep();
    }

    private IEnumerator ReturnToMenuAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);

        if (!string.IsNullOrEmpty(mainMenuSceneName))
            SceneManager.LoadScene(mainMenuSceneName);
    }
    private void ShowPanel()
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);

        if (panelAnimator != null && panelAnimator.runtimeAnimatorController != null)
            panelAnimator.SetBool(PanelVisible, true);

        if (dimmerAnimator != null && dimmerAnimator.runtimeAnimatorController != null)
            dimmerAnimator.SetBool(DimmerActive, true);
    }

    private void HidePanel()
    {
        if (panelAnimator != null && panelAnimator.runtimeAnimatorController != null)
            panelAnimator.SetBool(PanelVisible, false);

        if (dimmerAnimator != null && dimmerAnimator.runtimeAnimatorController != null)
            dimmerAnimator.SetBool(DimmerActive, false);

        StartCoroutine(HidePanelAfterAnim());
    }

    private IEnumerator HidePanelAfterAnim()
    {
        yield return new WaitForSecondsRealtime(0.2f);

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }
}
