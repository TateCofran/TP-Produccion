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
        [Header("Logical Identifier")]
        public string key;

        [Header("Camera To Activate")]
        public CinemachineCamera camera;

        [Header("Panel Linked To This Camera")]
        public GameObject panel;

        [Header("Optional UI Inside The Panel")]
        public Button startGameButton;
        public string sceneToLoad = "GameScene";

        [Header("Optional Alembic (before showing the panel)")]
        public AlembicStreamPlayer alembic;
        [Min(0f)] public float alembicDurationFallback = 2.0f;

        [Header("Optional Animation (Start - Play)")]
        [Tooltip("Animator of the object that animates when entering this entry.")]
        public Animator panelAnimator;
        [Tooltip("Trigger that starts the animation.")]
        public string panelAnimatorTrigger = "Play";
        [Tooltip("If enabled and this key requires the toggle panel, waits for the animation to end before showing it.")]
        public bool waitToggleUntilAnimEnds = false;
        [Tooltip("Maximum time to wait for the animation (0 = no limit).")]
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

    [Header("Flow")]
    [Tooltip("Wait for the blend to finish before playing Alembic and showing panels.")]
    [SerializeField] private bool waitBlendBeforeAlembicAndPanel = true;

    [Header("Entries (cameras/panels)")]
    [SerializeField] private List<Entry> entries = new();

    [Header("Persistent Toggle Panel")]
    [Tooltip("Panel that stays active on some cameras. Do not include it in UIPanelController.panels.")]
    [SerializeField] private GameObject togglePanel;

    [Tooltip("Start key used to detect a return to the initial state.")]
    [SerializeField] private string startKeyName = "CameraStart";

    [Tooltip("Keys where the toggle panel is always active (unless gated by an animation).")]
    [SerializeField] private string[] toggleOnKeys = new[] { "CameraPlay", "CameraMechanic", "CameraLab" };

    [SerializeField] private CinemachineCameraActivator cameraActivator;
    [SerializeField] private UIPanelController panelController;

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

        if (panelController != null)
            panelController.HideAllExcept(togglePanel);

        if (!string.IsNullOrWhiteSpace(defaultKey))
        {
            _currentKey = defaultKey;

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

        if (togglePanel != null && targetNeedsToggle && target.waitToggleUntilAnimEnds)
            togglePanel.SetActive(false);

        cameraActivator?.SetActive(target.camera, activePriority, inactivePriority);

        Coroutine animRoutine = null;
        if (target.panelAnimator != null && !string.IsNullOrEmpty(target.panelAnimatorTrigger))
        {
            animRoutine = StartCoroutine(_animationWaiter.TriggerAndWait(
                target.panelAnimator,
                target.panelAnimatorTrigger,
                target.animationTimeoutSeconds));
        }

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

        if (target.alembic != null && target.alembicDurationFallback > 0f)
            yield return _alembicPlayer.Play(target.alembic, target.alembicDurationFallback);

        if (panelController != null)
        {
            panelController.HideAllExcept(togglePanel);
            panelController.Show(target.panel);
        }
        else if (target.panel != null)
        {
            target.panel.SetActive(true);
        }

        if (togglePanel != null && targetNeedsToggle)
        {
            if (animRoutine != null && target.waitToggleUntilAnimEnds)
                yield return animRoutine;

            togglePanel.SetActive(true);
        }
        else
        {
            if (togglePanel != null && string.Equals(_currentKey, startKeyName, StringComparison.OrdinalIgnoreCase))
                togglePanel.SetActive(false);
        }

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
