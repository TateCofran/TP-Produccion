using System;
using System.Reflection;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

public class PlayButtonController : MonoBehaviour
{
    [Header("Cámaras (Cinemachine)")]
    [SerializeField] private CinemachineCamera menuCam;
    [SerializeField] private CinemachineCamera gameplayCam;
    [Tooltip("Si lo dejás null, toma el CinemachineBrain de la cámara principal (Camera.main).")]
    [SerializeField] private CinemachineBrain brain;

    [Header("Blend (velocidad de transición)")]
    [Tooltip("Nombre del estilo de blend. Ej: EaseInOut, Linear, Cut, HardIn, HardOut, EaseIn, EaseOut")]
    [SerializeField] private string blendStyleName = "EaseInOut";
    [Tooltip("Duración del blend entre cámaras (segundos).")]
    [SerializeField, Min(0f)] private float blendTime = 1.5f;

    [Tooltip("Asset de Custom Blends (opcional). Si lo asignás, manda sobre el default blend).")]
    [SerializeField] private CinemachineBlenderSettings customBlends;

    [Tooltip("Aplicar este blend solo para este swap y luego restaurar el Brain.")]
    [SerializeField] private bool onlyAffectsThisSwap = true;

    [Header("Animación / Timeline (opcional)")]
    [SerializeField] private Animator targetAnimator;
    [SerializeField] private string animatorTrigger = "Play";
    [SerializeField] private PlayableDirector timeline;

    [Header("UI")]
    [SerializeField] private Button playButton;
    [SerializeField] private GameObject uiPanelToHide;
    [SerializeField] private GameObject basePanelToShow;

    private bool _started;

    // Backups (guardamos como object para evitar tipos que cambian entre versiones)
    private object _originalDefaultBlend;           // CinemachineBlendDefinition
    private CinemachineBlenderSettings _originalCustomBlends;

    private void Awake()
    {
        if (brain == null && Camera.main != null)
            brain = Camera.main.GetComponent<CinemachineBrain>();

        // Estado inicial: menú activo
        if (menuCam != null) menuCam.Priority = 20;
        if (gameplayCam != null) gameplayCam.Priority = 10;

        // Asegurar que el BasePanel arranque desactivado
        if (basePanelToShow != null)
            basePanelToShow.SetActive(false);
    }

    public void OnPlayButtonClick()
    {
        if (_started) return;
        _started = true;

        // Desactivar el panel de UI
        if (uiPanelToHide != null)
            uiPanelToHide.SetActive(false);

        //Desactivar el botón para evitar doble clic
        if (playButton != null)
            playButton.interactable = false;

        //Configurar el blend
        if (brain != null)
        {
            if (onlyAffectsThisSwap)
            {
                _originalDefaultBlend = GetBrainDefaultBlend(brain);
                _originalCustomBlends = GetBrainCustomBlends(brain);
            }

            if (customBlends != null)
            {
                SetBrainCustomBlends(brain, customBlends);
            }
            else
            {
                SetBrainCustomBlends(brain, null);
                var def = CreateBlendDefinition(blendStyleName, blendTime);
                if (def != null)
                    SetBrainDefaultBlend(brain, def);
            }
        }

        //Cambiar prioridad de cámaras
        if (menuCam != null) menuCam.Priority = 10;
        if (gameplayCam != null) gameplayCam.Priority = 30;

        //Iniciar animación/timeline inmediatamente
        StartAnimationNow();

        // Restaurar blend al terminar (si corresponde)
        if (onlyAffectsThisSwap && brain != null)
            StartCoroutine(RestoreBlendWhenFinished());

        // Esperar al final del blend para mostrar el BasePanel
        if (brain != null)
            StartCoroutine(WaitForBlendThenShowPanel());
    }
    private System.Collections.IEnumerator WaitForBlendThenShowPanel()
    {
        // Esperar un frame para asegurar que empiece el blend
        yield return null;

        // Esperar mientras Cinemachine está blendando
        while (brain.IsBlending)
            yield return null;

        // Mostrar el BasePanel al terminar el blend
        if (basePanelToShow != null)
            basePanelToShow.SetActive(true);

        // Restaurar blend si corresponde
        if (onlyAffectsThisSwap)
        {
            if (_originalDefaultBlend != null)
                SetBrainDefaultBlend(brain, _originalDefaultBlend);
            SetBrainCustomBlends(brain, _originalCustomBlends);
        }
    }
    private System.Collections.IEnumerator RestoreBlendWhenFinished()
    {
        // Esperar a que arranque el blend
        yield return null;
        while (brain.IsBlending)
            yield return null;

        // Restaurar valores originales
        if (_originalDefaultBlend != null) SetBrainDefaultBlend(brain, _originalDefaultBlend);
        SetBrainCustomBlends(brain, _originalCustomBlends);
    }

    private void StartAnimationNow()
    {
        if (targetAnimator != null && !string.IsNullOrEmpty(animatorTrigger))
        {
            targetAnimator.ResetTrigger(animatorTrigger);
            targetAnimator.SetTrigger(animatorTrigger);
        }

        if (timeline != null)
        {
            timeline.time = 0;
            timeline.Play();
        }
    }

    // ===== Helpers por reflection (compatibles CM 2.x y 3.x) =====

    // Crea un CinemachineBlendDefinition sin referenciar el enum anidado
    private static object CreateBlendDefinition(string styleName, float time)
    {
        var cbdType = typeof(CinemachineBlendDefinition);
        // Buscar enum interno: "Style" (CM2/CM3) o "Styles" (algunas versiones)
        var enumType = cbdType.GetNestedType("Style", BindingFlags.Public) ??
                       cbdType.GetNestedType("Styles", BindingFlags.Public);
        if (enumType == null) return null;

        // Parsear el nombre recibido al enum
        object styleEnum;
        try
        {
            styleEnum = Enum.Parse(enumType, styleName, ignoreCase: true);
        }
        catch
        {
            Debug.LogWarning($"[PlayButtonController] Estilo '{styleName}' no válido. Probá: Cut, EaseInOut, Linear, HardIn, HardOut, EaseIn, EaseOut.");
            return null;
        }

        // Buscar ctor (Style, float)
        var ctor = cbdType.GetConstructor(new Type[] { enumType, typeof(float) });
        if (ctor == null) return null;

        return ctor.Invoke(new object[] { styleEnum, time });
    }

    private static object GetBrainDefaultBlend(CinemachineBrain b)
    {
        var t = b.GetType();

        // CM3: propiedad pública DefaultBlend
        var prop = t.GetProperty("DefaultBlend", BindingFlags.Instance | BindingFlags.Public);
        if (prop != null)
            return prop.GetValue(b);

        // CM2: campo m_DefaultBlend
        var field = t.GetField("m_DefaultBlend", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field != null)
            return field.GetValue(b);

        return null;
    }

    private static void SetBrainDefaultBlend(CinemachineBrain b, object blendDef)
    {
        if (blendDef == null) return;
        var t = b.GetType();

        var prop = t.GetProperty("DefaultBlend", BindingFlags.Instance | BindingFlags.Public);
        if (prop != null && prop.CanWrite)
        {
            prop.SetValue(b, blendDef);
            return;
        }

        var field = t.GetField("m_DefaultBlend", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field != null)
        {
            field.SetValue(b, blendDef);
        }
    }

    private static CinemachineBlenderSettings GetBrainCustomBlends(CinemachineBrain b)
    {
        var t = b.GetType();

        var prop = t.GetProperty("CustomBlends", BindingFlags.Instance | BindingFlags.Public);
        if (prop != null)
            return (CinemachineBlenderSettings)prop.GetValue(b);

        var field = t.GetField("m_CustomBlends", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field != null)
            return (CinemachineBlenderSettings)field.GetValue(b);

        return null;
    }

    private static void SetBrainCustomBlends(CinemachineBrain b, CinemachineBlenderSettings settings)
    {
        var t = b.GetType();

        var prop = t.GetProperty("CustomBlends", BindingFlags.Instance | BindingFlags.Public);
        if (prop != null && prop.CanWrite)
        {
            prop.SetValue(b, settings);
            return;
        }

        var field = t.GetField("m_CustomBlends", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field != null)
        {
            field.SetValue(b, settings);
        }
    }
}