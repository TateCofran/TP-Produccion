using System;
using System.Collections;
using Unity.Cinemachine;

public class CinemachineBlendController : IBlendController
{
    private object _originalDefaultBlend;
    private CinemachineBlenderSettings _originalCustom;

    public IEnumerator ApplyBlendOnce(CinemachineBrain brain, string blendStyleName, float blendTime, CinemachineBlenderSettings custom, bool restoreAfter)
    {
        if (brain == null) yield break;

        // Backup
        if (restoreAfter)
        {
            _originalDefaultBlend = GetBrainDefaultBlend(brain);
            _originalCustom = GetBrainCustomBlends(brain);
        }

        if (custom != null)
        {
            SetBrainCustomBlends(brain, custom);
        }
        else
        {
            SetBrainCustomBlends(brain, null);
            var def = CreateBlendDefinition(blendStyleName, blendTime);
            if (def != null)
                SetBrainDefaultBlend(brain, def);
        }

        // Esperar a que inicie el blend (siguiente frame)
        yield return null;
        // Esperar a que termine el blend
        while (IsBlending(brain))
            yield return null;

        // Restaurar si corresponde
        if (restoreAfter)
        {
            if (_originalDefaultBlend != null) SetBrainDefaultBlend(brain, _originalDefaultBlend);
            SetBrainCustomBlends(brain, _originalCustom);
        }
    }

    public bool IsBlending(CinemachineBrain brain) => brain != null && brain.IsBlending;

    // ===== Helpers de compatibilidad CM2/CM3 (reflection) =====
    private static object CreateBlendDefinition(string styleName, float time)
    {
        var cbdType = typeof(CinemachineBlendDefinition);
        var enumType = cbdType.GetNestedType("Style", System.Reflection.BindingFlags.Public) ??
                       cbdType.GetNestedType("Styles", System.Reflection.BindingFlags.Public);
        if (enumType == null) return null;

        object styleEnum;
        try { styleEnum = Enum.Parse(enumType, styleName, ignoreCase: true); }
        catch { return null; }

        var ctor = cbdType.GetConstructor(new Type[] { enumType, typeof(float) });
        if (ctor == null) return null;
        return ctor.Invoke(new object[] { styleEnum, time });
    }

    private static object GetBrainDefaultBlend(CinemachineBrain b)
    {
        var t = b.GetType();
        var prop = t.GetProperty("DefaultBlend", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        if (prop != null) return prop.GetValue(b);
        var field = t.GetField("m_DefaultBlend", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (field != null) return field.GetValue(b);
        return null;
    }

    private static void SetBrainDefaultBlend(CinemachineBrain b, object blendDef)
    {
        if (blendDef == null) return;
        var t = b.GetType();

        var prop = t.GetProperty("DefaultBlend", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        if (prop != null && prop.CanWrite) { prop.SetValue(b, blendDef); return; }

        var field = t.GetField("m_DefaultBlend", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (field != null) field.SetValue(b, blendDef);
    }

    private static CinemachineBlenderSettings GetBrainCustomBlends(CinemachineBrain b)
    {
        var t = b.GetType();
        var prop = t.GetProperty("CustomBlends", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        if (prop != null) return (CinemachineBlenderSettings)prop.GetValue(b);

        var field = t.GetField("m_CustomBlends", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (field != null) return (CinemachineBlenderSettings)field.GetValue(b);

        return null;
    }

    private static void SetBrainCustomBlends(CinemachineBrain b, CinemachineBlenderSettings settings)
    {
        var t = b.GetType();
        var prop = t.GetProperty("CustomBlends", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        if (prop != null && prop.CanWrite) { prop.SetValue(b, settings); return; }

        var field = t.GetField("m_CustomBlends", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (field != null) field.SetValue(b, settings);
    }
}
