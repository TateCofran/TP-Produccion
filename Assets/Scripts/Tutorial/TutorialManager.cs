using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections;

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

    // ==== NUEVO: API pública de paso actual ====
    public int CurrentStepIndex => currentStep;

    public TutorialEventType CurrentStepType
    {
        get
        {
            if (steps == null || currentStep < 0 || currentStep >= steps.Length)
                return TutorialEventType.Manual; // valor por defecto
            return steps[currentStep].eventType;
        }
    }

    /// <summary>
    /// Devuelve true si el tutorial está corriendo y el paso actual es del tipo dado.
    /// </summary>
    public bool IsCurrentStep(TutorialEventType type)
    {
        if (!running) return false;
        if (steps == null || currentStep < 0 || currentStep >= steps.Length) return false;
        return steps[currentStep].eventType == type;
    }

    /// <summary>
    /// Evento opcional para que otros sistemas reaccionen cuando cambia el paso.
    /// (int = índice actual, TutorialStep = datos del paso)
    /// </summary>
    public event Action<int, TutorialStep> OnStepChanged;
    // ===========================================

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
        // Ahora el manager solo escucha al EventHub centralizado
        TutorialEventHub.TilePlaced += HandleTilePlaced;
        TutorialEventHub.TurretPlaced += HandleTurretPlaced;
        TutorialEventHub.WaveStarted += HandleWaveStarted;
        TutorialEventHub.CoreDamaged += HandleCoreDamaged;
        TutorialEventHub.WorldSwitched += HandleWorldSwitched;
    }

    private void OnDisable()
    {
        TutorialEventHub.TilePlaced -= HandleTilePlaced;
        TutorialEventHub.TurretPlaced -= HandleTurretPlaced;
        TutorialEventHub.WaveStarted -= HandleWaveStarted;
        TutorialEventHub.CoreDamaged -= HandleCoreDamaged;
        TutorialEventHub.WorldSwitched -= HandleWorldSwitched;
    }

    private void Start()
    {
        if (autoStart)
            StartTutorial();
        else if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private TutorialStep[] BuildSteps()
    {
        return new TutorialStep[]
        {
            new TutorialStep(
                "Control de Cámara",
                "Usá WASD (o las flechas) para mover la cámara.\n" +
                "Usá la RUEDA DEL MOUSE para hacer zoom in/out.\n\n" +
                "Probá moverte y hacer zoom, y cuando estés cómodo pulsá 'Continuar'.",
                TutorialEventType.Manual,
                allowTiles:false, allowTurrets:false, allowWave:false, allowWorld:false
            ),

            new TutorialStep(
                "Bienvenido",
                "Te enseñaré cómo defender el núcleo.",
                TutorialEventType.Manual,
                allowTiles:false, allowTurrets:false, allowWave:false, allowWorld:false
            ),

            new TutorialStep(
                "Colocá tu primer Tile",
                "Elegí un Tile y colocá un Camino.",
                TutorialEventType.TilePlaced,
                allowTiles:true, allowTurrets:false, allowWave:false, allowWorld:false
            ),

            new TutorialStep(
                "Colocá una Torreta",
                "Seleccioná una torreta y colócala en una celda libre.",
                TutorialEventType.TurretPlaced,
                allowTiles:false, allowTurrets:true, allowWave:false, allowWorld:false
            ),

            new TutorialStep(
                "Iniciá la Oleada",
                "Al colocar una torreta, la oleada comienza automáticamente.",
                TutorialEventType.WaveStarted,
                allowTiles:false, allowTurrets:false, allowWave:true, allowWorld:false
            ),

            new TutorialStep(
                "Defendé el Núcleo",
                "Si el Núcleo llega a 0, perdés. ¡Defendelo!",
                TutorialEventType.Manual,
                allowTiles:true, allowTurrets:true, allowWave:true, allowWorld:true
            ),

            new TutorialStep(
                "Cambio de Mundo",
                "Podés cambiar entre el Mundo Normal y el Otro Mundo con la tecla J.\n",
                TutorialEventType.Manual,
                allowTiles:true, allowTurrets:true, allowWave:true, allowWorld:false
            ),

            new TutorialStep(
                "Probá cambiar de mundo",
                "Presioná J para cambiar de mundo ahora.",
                TutorialEventType.WorldSwitched,
                allowTiles:true, allowTurrets:true, allowWave:true, allowWorld:true
            ),

            new TutorialStep(
                "Esencias por Cambio de Mundo",
                "Cada vez que cambiás de mundo ganás esencias.\n" +
                "Las esencias se usan para mejoras permanentes en el Laboratorio y el Taller.\n",
                TutorialEventType.Manual,
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
            // Mostrar el panel de tiles inmediatamente
            var swUI = FindFirstObjectByType<ShiftingWorldUI>();
            if (swUI != null)
                swUI.ForceOpenTilePanel();
        }
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

    public void NotifyCoreDamaged()
    {
        CheckEvent(TutorialEventType.CoreDamaged);
    }

    private void CheckEvent(TutorialEventType evt)
    {
        if (!running) return;
        if (currentStep < 0 || currentStep >= steps.Length) return;

        var s = steps[currentStep];
        if (s.eventType == evt)
            NextStep();
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
