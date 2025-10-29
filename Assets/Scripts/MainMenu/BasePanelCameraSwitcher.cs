using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Formats.Alembic.Importer;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Reflection;

public class BasePanelCameraSwitcher : MonoBehaviour
{
    [Header("Cinemachine")]
    [SerializeField] private CinemachineBrain brain; // si está vacío, toma Camera.main
    [SerializeField] private CinemachineCamera cameraLab;
    [SerializeField] private CinemachineCamera cameraPlay;
    [SerializeField] private CinemachineCamera cameraMechanic;

    [Header("Prioridades")]
    [SerializeField] private int activePriority = 30;
    [SerializeField] private int inactivePriority = 10;

    [Header("UI Buttons")]
    [SerializeField] private Button btnLaboratory;
    [SerializeField] private Button btnPlay;
    [SerializeField] private Button btnWorkshop;

    [Header("Panels")]
    [SerializeField] private GameObject labPanelToShow;
    [SerializeField] private GameObject workshopPanelToShow;
    [SerializeField] private GameObject playPanelToShow;       // panel con botón "Start Game"
    [SerializeField] private Button playStartGameButton;       // botón dentro de playPanelToShow

    [Header("Alembic - Laboratory")]
    [SerializeField] private AlembicStreamPlayer labAlembic;
    [SerializeField, Min(0f)] private float labDuration = 2.0f; // seg fallback si no hay duración

    [Header("Alembic - Workshop")]
    [SerializeField] private AlembicStreamPlayer workshopAlembic;
    [SerializeField, Min(0f)] private float workshopDuration = 2.0f;

    [Header("Timing")]
    [Tooltip("Esperar a que termine el blend de cámara antes de iniciar la animación Alembic.")]
    [SerializeField] private bool waitBlendBeforeAlembic = true;

    private void Awake()
    {
        if (brain == null && Camera.main != null)
            brain = Camera.main.GetComponent<CinemachineBrain>();

        // Prioridades iniciales (Play activo por defecto)
        SetPriority(cameraLab, inactivePriority);
        SetPriority(cameraMechanic, inactivePriority);
        SetPriority(cameraPlay, activePriority);

        // Panels off de arranque
        if (labPanelToShow) labPanelToShow.SetActive(false);
        if (workshopPanelToShow) workshopPanelToShow.SetActive(false);
        if (playPanelToShow) playPanelToShow.SetActive(false);

        // Botones
        if (btnLaboratory) btnLaboratory.onClick.AddListener(OnLaboratory);
        if (btnWorkshop) btnWorkshop.onClick.AddListener(OnWorkshop);
        if (btnPlay) btnPlay.onClick.AddListener(OnPlayPanel);

        if (playStartGameButton)
            playStartGameButton.onClick.AddListener(() => LoadScene("GameScene"));
    }

    // ---------- Botones ----------
    public void OnLaboratory()
    {
        SafeHideAllPanels();
        StartCoroutine(SwitchCamThenAlembicThenPanel(
            cameraLab, labAlembic, labDuration, labPanelToShow));
    }

    public void OnWorkshop()
    {
        SafeHideAllPanels();
        StartCoroutine(SwitchCamThenAlembicThenPanel(
            cameraMechanic, workshopAlembic, workshopDuration, workshopPanelToShow));
    }

    public void OnPlayPanel()
    {
        SafeHideAllPanels();
        SwitchTo(cameraPlay);
        if (playPanelToShow) playPanelToShow.SetActive(true);
    }

    // ---------- Flujo principal ----------
    private System.Collections.IEnumerator SwitchCamThenAlembicThenPanel(
        CinemachineCamera targetCam,
        AlembicStreamPlayer alembic,
        float durationSeconds,
        GameObject panelToShow)
    {
        SetButtonsInteractable(false);

        // 1) Cambiar cámara
        SwitchTo(targetCam);

        // 2) Esperar blend si corresponde
        if (waitBlendBeforeAlembic && brain != null)
        {
            yield return null;
            while (brain.IsBlending)
                yield return null;
        }

        // 3) Reproducir Alembic avanzando tiempo manualmente (compat Time/CurrentTime)
        if (alembic != null && durationSeconds > 0f)
        {
            double clipDuration = GetAlembicDurationSafe(alembic);
            if (clipDuration <= 0.0) clipDuration = durationSeconds; // fallback

            SetAlembicTimeSafe(alembic, 0.0);

            float elapsed = 0f;
            while (elapsed < durationSeconds)
            {
                elapsed += Time.deltaTime;
                float t01 = Mathf.Clamp01(elapsed / durationSeconds);
                SetAlembicTimeSafe(alembic, t01 * clipDuration);
                yield return null;
            }

            SetAlembicTimeSafe(alembic, clipDuration);
        }

        // 4) Mostrar panel destino
        if (panelToShow) panelToShow.SetActive(true);

        SetButtonsInteractable(true);
    }

    // ---------- Utilidades ----------
    private void SwitchTo(CinemachineCamera target)
    {
        if (!target) return;

        SetPriority(cameraLab, target == cameraLab ? activePriority : inactivePriority);
        SetPriority(cameraPlay, target == cameraPlay ? activePriority : inactivePriority);
        SetPriority(cameraMechanic, target == cameraMechanic ? activePriority : inactivePriority);
    }

    private void SetPriority(CinemachineCamera vcam, int p)
    {
        if (vcam) vcam.Priority = p;
    }

    private void SetButtonsInteractable(bool value)
    {
        if (btnLaboratory) btnLaboratory.interactable = value;
        if (btnPlay) btnPlay.interactable = value;
        if (btnWorkshop) btnWorkshop.interactable = value;
    }

    private void SafeHideAllPanels()
    {
        if (labPanelToShow) labPanelToShow.SetActive(false);
        if (workshopPanelToShow) workshopPanelToShow.SetActive(false);
        if (playPanelToShow) playPanelToShow.SetActive(false);
    }

    public void LoadScene(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName))
            SceneManager.LoadScene(sceneName);
    }

    // ---------- Alembic helpers (compat varias versiones) ----------
    private static void SetAlembicTimeSafe(AlembicStreamPlayer p, double t)
    {
        // Algunas versiones usan property "Time", otras "CurrentTime".
        var tp = p.GetType();
        var propTime = tp.GetProperty("Time");
        if (propTime != null && propTime.PropertyType == typeof(double) && propTime.CanWrite)
        {
            propTime.SetValue(p, t, null);
            return;
        }
        var propCurrent = tp.GetProperty("CurrentTime");
        if (propCurrent != null && propCurrent.PropertyType == typeof(double) && propCurrent.CanWrite)
        {
            propCurrent.SetValue(p, t, null);
            return;
        }

        // Fallback: nada; tu paquete debería tener al menos una de las dos.
    }

    private static double GetAlembicDurationSafe(AlembicStreamPlayer p)
    {
        var tp = p.GetType();

        // 1) Intentar "Duration" (double)
        var propDur = tp.GetProperty("Duration");
        if (propDur != null && propDur.PropertyType == typeof(double))
        {
            var v = propDur.GetValue(p, null);
            if (v is double d1) return d1;
        }

        // 2) Intentar "StreamDescriptor" con Start/EndTime
        var propDesc = tp.GetProperty("StreamDescriptor");
        if (propDesc != null)
        {
            var desc = propDesc.GetValue(p, null);
            if (desc != null)
            {
                var tDesc = desc.GetType();
                var pStart = tDesc.GetProperty("StartTime");
                var pEnd = tDesc.GetProperty("EndTime");
                if (pStart != null && pEnd != null &&
                    pStart.PropertyType == typeof(double) &&
                    pEnd.PropertyType == typeof(double))
                {
                    var s = (double)pStart.GetValue(desc, null);
                    var e = (double)pEnd.GetValue(desc, null);
                    return Mathf.Max(0f, (float)(e - s));
                }
            }
        }

        // 3) Sin info: devolver 0 para usar fallback del inspector
        return 0.0;
    }
}
