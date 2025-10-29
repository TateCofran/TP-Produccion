// CameraPanelManager.cs
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Formats.Alembic.Importer;

[DisallowMultipleComponent]
public class CameraPanelManager : MonoBehaviour
{
    [Serializable]
    public class Entry
    {
        [Header("Identificador lógico")]
        public string key; // Ej.: "CameraStart", "CameraPlay", "CameraMechanic", "CameraLab"

        [Header("Cámara a activar")]
        public CinemachineCamera camera;

        [Header("Panel asociado a esta cámara")]
        public GameObject panel;

        [Header("UI opcional dentro del panel")]
        public Button startGameButton;
        public string sceneToLoad = "GameScene";

        [Header("Alembic opcional (antes de mostrar el panel)")]
        public AlembicStreamPlayer alembic;
        [Min(0f)] public float alembicDurationFallback = 2.0f;

        [Header("Animación opcional (Start - Play)")]
        [Tooltip("Animator del objeto que debe animarse al entrar en esta Entry.")]
        public Animator panelAnimator;
        [Tooltip("Trigger para iniciar la animación.")]
        public string panelAnimatorTrigger = "Play";
        [Tooltip("Si está activo y esta key requiere togglePanel, se espera al fin de animación para mostrar el toggle.")]
        public bool waitToggleUntilAnimEnds = false;
        [Tooltip("Tiempo máximo de espera de animación (0 = sin límite).")]
        public float animationTimeoutSeconds = 0f;
    }

    [Header("General")]
    [SerializeField] private string defaultKey = "CameraStart";
    [SerializeField] private int activePriority = 30;
    [SerializeField] private int inactivePriority = 10;

    [Header("Cinemachine")]
    [SerializeField] private CinemachineBrain brain;
    [SerializeField] private string blendStyleName = "EaseInOut";
    [SerializeField, Min(0f)] private float blendTime = 1.2f;
    [SerializeField] private CinemachineBlenderSettings customBlends;
    [SerializeField] private bool restoreBlendAfterSwap = true;

    [Header("Flujo")]
    [Tooltip("Esperar fin del blend antes de reproducir Alembic y mostrar paneles.")]
    [SerializeField] private bool waitBlendBeforeAlembicAndPanel = true;

    [Header("Entradas (cámaras/paneles)")]
    [SerializeField] private List<Entry> entries = new();

    [Header("Toggle Panel persistente")]
    [Tooltip("Panel que debe permanecer activo en ciertas cámaras. No incluirlo en UIPanelController.panels.")]
    [SerializeField] private GameObject togglePanel;

    [Tooltip("Key de Start para detectar regreso al estado inicial.")]
    [SerializeField] private string startKeyName = "CameraStart";

    [Tooltip("Keys en las que el TogglePanel debe estar SIEMPRE activo (salvo que se gatee por animación).")]
    [SerializeField] private string[] toggleOnKeys = new[] { "CameraPlay", "CameraMechanic", "CameraLab" };

    // Dependencias
    [SerializeField] private CinemachineCameraActivator cameraActivator;
    [SerializeField] private UIPanelController panelController;

    // Servicios
    private IBlendController _blendController;
    private IAlembicPlayer _alembicPlayer;
    private IAnimationWaiter _animationWaiter;

    private readonly Dictionary<string, Entry> _map = new();
    private string _currentKey;

    private void Awake()
    {
        if (brain == null && Camera.main != null)
            brain = Camera.main.GetComponent<CinemachineBrain>();

        _map.Clear();
        foreach (var e in entries)
        {
            if (e != null && !string.IsNullOrWhiteSpace(e.key))
                _map[e.key] = e;
        }

        if (cameraActivator == null)
            cameraActivator = GetComponent<CinemachineCameraActivator>();
        if (panelController == null)
            panelController = GetComponent<UIPanelController>();

        _blendController = new CinemachineBlendController();
        _alembicPlayer = new AlembicManualPlayer();
        _animationWaiter = new AnimatorTriggerWaiter();

        // Ocultar paneles controlados, sin tocar el togglePanel
        if (panelController != null)
            panelController.HideAllExcept(togglePanel);

        if (!string.IsNullOrWhiteSpace(defaultKey))
        {
            _currentKey = defaultKey;
            // No encendemos el toggle aún; lo decidirá Activate según la key
            Activate(defaultKey);
        }
    }

    public void Activate(string key)
    {
        if (!_map.TryGetValue(key, out var target)) return;
        _currentKey = key;
        StopAllCoroutines();
        StartCoroutine(DoActivate(target));
    }

    private IEnumerator DoActivate(Entry target)
    {
        bool targetNeedsToggle = toggleOnKeys != null &&
                                 toggleOnKeys.Any(k => string.Equals(k, _currentKey, StringComparison.OrdinalIgnoreCase));

        // 0) Si esta Entry requiere gatear el toggle por animación, lo apagamos por ahora
        if (togglePanel != null && targetNeedsToggle && target.waitToggleUntilAnimEnds)
            togglePanel.SetActive(false);

        // 1) Activar cámara
        cameraActivator?.SetActive(target.camera, activePriority, inactivePriority);

        // 2) Lanzar animación EN PARALELO al blend (si corresponde)
        Coroutine animRoutine = null;
        if (target.panelAnimator != null && !string.IsNullOrEmpty(target.panelAnimatorTrigger))
        {
            // Disparo y espero más adelante (en paralelo con el blend)
            animRoutine = StartCoroutine(_animationWaiter.TriggerAndWait(
                target.panelAnimator,
                target.panelAnimatorTrigger,
                target.animationTimeoutSeconds));
        }

        // 3) Blend
        if (brain != null)
        {
            if (waitBlendBeforeAlembicAndPanel)
            {
                yield return _blendController.ApplyBlendOnce(brain, blendStyleName, blendTime, customBlends, restoreBlendAfterSwap);
            }
            else
            {
                StartCoroutine(_blendController.ApplyBlendOnce(brain, blendStyleName, blendTime, customBlends, restoreBlendAfterSwap));
            }
        }

        // 4) Alembic (opcional)
        if (target.alembic != null && target.alembicDurationFallback > 0f)
            yield return _alembicPlayer.Play(target.alembic, target.alembicDurationFallback);

        // 5) Mostrar panel de la entrada, sin apagar el togglePanel
        if (panelController != null)
        {
            panelController.HideAllExcept(togglePanel);
            panelController.Show(target.panel);
        }
        else if (target.panel != null)
        {
            target.panel.SetActive(true);
        }

        // 6) Si el toggle está gateado por animación, esperar a que termine para encenderlo
        if (togglePanel != null && targetNeedsToggle)
        {
            if (animRoutine != null && target.waitToggleUntilAnimEnds)
                yield return animRoutine; // espera real al final de la animación

            // Encender ahora (o encender directamente si no se gateó)
            togglePanel.SetActive(true);
        }
        else
        {
            // Si la key actual no necesita toggle, apagarlo
            if (togglePanel != null && string.Equals(_currentKey, startKeyName, StringComparison.OrdinalIgnoreCase))
                togglePanel.SetActive(false);
        }

        // 7) Hook botón Start Game (si existe)
        if (target.startGameButton != null && !string.IsNullOrEmpty(target.sceneToLoad))
        {
            target.startGameButton.onClick.RemoveAllListeners();
            string scene = target.sceneToLoad;
            target.startGameButton.onClick.AddListener(() => LoadScene(scene));
        }
    }

    public void LoadScene(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName))
            SceneManager.LoadScene(sceneName);
    }
}
