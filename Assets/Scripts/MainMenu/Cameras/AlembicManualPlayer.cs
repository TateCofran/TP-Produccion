using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Formats.Alembic.Importer;

public class AlembicManualPlayer : IAlembicPlayer
{
    public IEnumerator Play(AlembicStreamPlayer player, float fallbackSeconds)
    {
        if (player == null) yield break;

        double clipDuration = GetAlembicDurationSafe(player);
        if (clipDuration <= 0.0) clipDuration = Math.Max(0.01, fallbackSeconds);

        SetAlembicTimeSafe(player, 0.0);

        float elapsed = 0f;
        while (elapsed < fallbackSeconds)
        {
            elapsed += Time.deltaTime;
            float t01 = Mathf.Clamp01(elapsed / fallbackSeconds);
            SetAlembicTimeSafe(player, t01 * clipDuration);
            yield return null;
        }

        SetAlembicTimeSafe(player, clipDuration);
    }

    private static void SetAlembicTimeSafe(AlembicStreamPlayer p, double t)
    {
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
        }
    }

    private static double GetAlembicDurationSafe(AlembicStreamPlayer p)
    {
        var tp = p.GetType();
        var propDur = tp.GetProperty("Duration");
        if (propDur != null && propDur.PropertyType == typeof(double))
        {
            var v = propDur.GetValue(p, null);
            if (v is double d1) return d1;
        }

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
        return 0.0;
    }
}
