using UnityEngine;

public static class RewardClaimFX
{
    public static Color ColorFor(RewardType type)
    {
        switch (type)
        {
            case RewardType.BlueEssence: return new Color(0.35f, 0.65f, 1f, 1f);
            case RewardType.RedEssence: return new Color(1f, 0.35f, 0.3f, 1f);
            case RewardType.FreeTurretDupe: return new Color(1f, 0.6f, 0.15f, 1f);
            case RewardType.FreeTileDupe: return new Color(0.4f, 0.95f, 0.45f, 1f);
            case RewardType.ExtraModifierPick: return new Color(0.85f, 0.5f, 1f, 1f);
            default: return Color.white;
        }
    }

    public static float PitchFor(RewardType type)
    {
        switch (type)
        {
            case RewardType.BlueEssence: return 1f;
            case RewardType.RedEssence: return 0.85f;
            case RewardType.FreeTurretDupe: return 1.15f;
            case RewardType.FreeTileDupe: return 1.3f;
            case RewardType.ExtraModifierPick: return 1.5f;
            default: return 1f;
        }
    }

    public static void Beam(Vector3 position, Color color, float height, float width, float duration)
    {
        FeedbackFxPool.Beam(position, position + Vector3.up * height, color, width, duration);
    }

    public static void Burst(GameObject burstPrefab, Vector3 position, Color color, float lifetime)
    {
        if (burstPrefab == null) return;
        var go = VfxPool.Spawn(burstPrefab, position, Quaternion.identity, lifetime);
        if (go == null) return;
        foreach (var ps in go.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = ps.main;
            main.startColor = color;
        }
    }

    public static AudioClip BuildTone(float baseFrequency)
    {
        const int rate = 44100;
        const float length = 0.45f;
        int samples = Mathf.CeilToInt(rate * length);
        var data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)rate;
            float env = Mathf.Exp(-t * 7f) * Mathf.Clamp01(t * 200f);
            float f = baseFrequency * (t < 0.12f ? 1f : 1.5f);
            data[i] = (Mathf.Sin(2f * Mathf.PI * f * t) * 0.55f + Mathf.Sin(2f * Mathf.PI * f * 2f * t) * 0.2f) * env;
        }
        var clip = AudioClip.Create("RewardClaimTone", samples, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
