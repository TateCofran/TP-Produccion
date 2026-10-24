using UnityEngine;
using UnityEngine.VFX;

[DisallowMultipleComponent]
public sealed class PooledVfx : MonoBehaviour
{
    public GameObject Prefab { get; private set; }
    public bool IsActive => active;

    private ParticleSystem[] particles;
    private VisualEffect[] visualEffects;
    private TrailRenderer[] trails;

    private bool active;
    private float releaseAt;
    private Transform follow;
    private Vector3 followOffset;

    internal void Init(GameObject prefab)
    {
        Prefab = prefab;
        particles = GetComponentsInChildren<ParticleSystem>(true);
        visualEffects = GetComponentsInChildren<VisualEffect>(true);
        trails = GetComponentsInChildren<TrailRenderer>(true);
    }

    internal void Activate(Vector3 position, Quaternion rotation, float lifetime, Transform followTarget)
    {
        transform.SetPositionAndRotation(position, rotation);

        follow = followTarget;
        followOffset = followTarget != null ? position - followTarget.position : Vector3.zero;
        releaseAt = lifetime > 0f ? Time.time + lifetime : float.PositiveInfinity;
        active = true;

        gameObject.SetActive(true);
        Restart();
    }

    private void Restart()
    {
        for (int i = 0; i < trails.Length; i++)
            if (trails[i] != null) trails[i].Clear();

        for (int i = 0; i < particles.Length; i++)
        {
            if (particles[i] == null) continue;
            particles[i].Clear(false);
            particles[i].Play(false);
        }

        for (int i = 0; i < visualEffects.Length; i++)
        {
            if (visualEffects[i] == null) continue;
            visualEffects[i].Reinit();
            visualEffects[i].Play();
        }
    }

    private void LateUpdate()
    {
        if (!active) return;

        if (follow != null)
        {
            if (follow.gameObject.activeInHierarchy)
                transform.position = follow.position + followOffset;
            else
                follow = null;
        }

        if (Time.time >= releaseAt)
            Release();
    }

    public void Release()
    {
        if (!active) return;
        active = false;
        follow = null;
        gameObject.SetActive(false);
        VfxPool.Return(this);
    }

    private void OnDestroy()
    {
        active = false;
    }
}
