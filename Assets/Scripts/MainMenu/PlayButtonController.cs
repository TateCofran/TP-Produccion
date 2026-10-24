using System;
using System.Reflection;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

public class PlayButtonController : MonoBehaviour
{
    [Header("Cameras (Cinemachine)")]
    [SerializeField] private CinemachineCamera menuCam;
    [SerializeField] private CinemachineCamera gameplayCam;
    [Tooltip("If left empty, uses the CinemachineBrain on the main camera (Camera.main).")]
    [SerializeField] private CinemachineBrain brain;

    [Header("Blend (transition speed)")]
    [Tooltip("Blend style name. For example: EaseInOut, Linear, Cut, HardIn, HardOut, EaseIn, EaseOut")]
    [SerializeField] private string blendStyleName = "EaseInOut";
    [Tooltip("Blend duration between cameras (seconds).")]
    [SerializeField, Min(0f)] private float blendTime = 1.5f;

    [Tooltip("Optional Custom Blends asset. When assigned it overrides the default blend.")]
    [SerializeField] private CinemachineBlenderSettings customBlends;

    [Tooltip("Apply this blend only for this swap and then restore the Brain.")]
    [SerializeField] private bool onlyAffectsThisSwap = true;

    [Header("Animation / Timeline (optional)")]
    [SerializeField] private Animator targetAnimator;
    [SerializeField] private string animatorTrigger = "Play";
    [SerializeField] private PlayableDirector timeline;

    [Header("UI")]
    [SerializeField] private Button playButton;
    [SerializeField] private GameObject uiPanelToHide;
    [SerializeField] private GameObject basePanelToShow;

    private bool _started;

    private object _originalDefaultBlend;
    private CinemachineBlenderSettings _originalCustomBlends;

    private void Awake()
    {
        if (brain == null && Camera.main != null)
            brain = Camera.main.GetComponent<CinemachineBrain>();

        if (menuCam != null) menuCam.Priority = 20;
        if (gameplayCam != null) gameplayCam.Priority = 10;

        if (basePanelToShow != null)
            basePanelToShow.SetActive(false);
    }

    public void OnPlayButtonClick()
    {
        if (_started) return;
        _started = true;

        if (uiPanelToHide != null)
            uiPanelToHide.SetActive(false);

        if (playButton != null)
            playButton.interactable = false;

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

        if (menuCam != null) menuCam.Priority = 10;
        if (gameplayCam != null) gameplayCam.Priority = 30;

        StartAnimationNow();

        if (onlyAffectsThisSwap && brain != null)
            StartCoroutine(RestoreBlendWhenFinished());

        if (brain != null)
            StartCoroutine(WaitForBlendThenShowPanel());
    }
    private System.Collections.IEnumerator WaitForBlendThenShowPanel()
    {
        yield return null;

        while (brain.IsBlending)
            yield return null;

        if (basePanelToShow != null)
            basePanelToShow.SetActive(true);

        if (onlyAffectsThisSwap)
        {
            if (_originalDefaultBlend != null)
                SetBrainDefaultBlend(brain, _originalDefaultBlend);
            SetBrainCustomBlends(brain, _originalCustomBlends);
        }
    }
    private System.Collections.IEnumerator RestoreBlendWhenFinished()
    {
        yield return null;
        while (brain.IsBlending)
            yield return null;

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

    private static object CreateBlendDefinition(string styleName, float time)
    {
        var cbdType = typeof(CinemachineBlendDefinition);

        var enumType = cbdType.GetNestedType("Style", BindingFlags.Public) ??
                       cbdType.GetNestedType("Styles", BindingFlags.Public);
        if (enumType == null) return null;

        object styleEnum;
        try
        {
            styleEnum = Enum.Parse(enumType, styleName, ignoreCase: true);
        }
        catch
        {
            Debug.LogWarning($"[PlayButtonController] Invalid style '{styleName}'. Valid values: Cut, EaseInOut, Linear, HardIn, HardOut, EaseIn, EaseOut");
            return null;
        }

        var ctor = cbdType.GetConstructor(new Type[] { enumType, typeof(float) });
        if (ctor == null) return null;

        return ctor.Invoke(new object[] { styleEnum, time });
    }

    private static object GetBrainDefaultBlend(CinemachineBrain b)
    {
        var t = b.GetType();

        var prop = t.GetProperty("DefaultBlend", BindingFlags.Instance | BindingFlags.Public);
        if (prop != null)
            return prop.GetValue(b);

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
