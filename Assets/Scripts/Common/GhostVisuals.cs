using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public static class GhostVisuals
{
    public const string ShaderName = "Shadow Realms/Ghost Fresnel";
    public const string DefaultLayerName = "Ghost";

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int AlphaId = Shader.PropertyToID("_Alpha");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private static Material fallbackMaterial;
    private static Transform inactiveHolder;
    private static readonly List<Renderer> rendererBuffer = new();
    private static MaterialPropertyBlock block;

    public static Material ResolveMaterial(Material preferred)
    {
        if (preferred != null) return preferred;
        if (fallbackMaterial != null) return fallbackMaterial;

        var shader = Shader.Find(ShaderName);
        if (shader == null)
        {
            Debug.LogWarning($"[GhostVisuals] Shader '{ShaderName}' not found. Assign a ghost material");
            return null;
        }

        fallbackMaterial = new Material(shader) { name = "GhostFallback", hideFlags = HideFlags.DontSave };
        return fallbackMaterial;
    }

    public static int ResolveLayer(string layerName)
    {
        int layer = LayerMask.NameToLayer(string.IsNullOrEmpty(layerName) ? DefaultLayerName : layerName);
        if (layer < 0) layer = LayerMask.NameToLayer("Ignore Raycast");
        return Mathf.Max(0, layer);
    }

    public static Transform InactiveHolder
    {
        get
        {
            if (inactiveHolder != null) return inactiveHolder;
            var go = new GameObject("GhostBuildHolder");
            go.SetActive(false);
            go.hideFlags = HideFlags.HideInHierarchy;
            inactiveHolder = go.transform;
            return inactiveHolder;
        }
    }

    public static GameObject InstantiateInert(GameObject prefab, Vector3 localPosition, Quaternion localRotation, Transform parent)
    {
        if (prefab == null) return null;
        var go = Object.Instantiate(prefab, InactiveHolder);
        StripGameplay(go);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = localRotation;
        return go;
    }

    public static void StripGameplay(GameObject root)
    {
        if (root == null) return;

        for (int pass = 0; pass < 8; pass++)
        {
            bool removed = false;
            bool remaining = false;
            foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb == null) continue;
                remaining = true;
                if (IsRequiredByOthers(mb)) continue;
                Object.DestroyImmediate(mb);
                removed = true;
            }
            if (!remaining || !removed) break;
        }

        foreach (var col in root.GetComponentsInChildren<Collider>(true))
            if (col != null) Object.DestroyImmediate(col);

        foreach (var rb in root.GetComponentsInChildren<Rigidbody>(true))
            if (rb != null) Object.DestroyImmediate(rb);

        foreach (var behaviour in root.GetComponentsInChildren<Behaviour>(true))
        {
            if (behaviour == null) continue;
            if (behaviour is Animator || behaviour is AudioSource || behaviour is Light || behaviour is Camera)
                behaviour.enabled = false;
        }

        foreach (var ps in root.GetComponentsInChildren<ParticleSystem>(true))
            if (ps != null) ps.gameObject.SetActive(false);
    }

    private static bool IsRequiredByOthers(Component target)
    {
        var targetType = target.GetType();
        foreach (var other in target.GetComponents<Component>())
        {
            if (other == null || other == target) continue;
            var attrs = (RequireComponent[])System.Attribute.GetCustomAttributes(other.GetType(), typeof(RequireComponent), true);
            foreach (var attr in attrs)
            {
                if (Requires(attr.m_Type0, targetType) || Requires(attr.m_Type1, targetType) || Requires(attr.m_Type2, targetType))
                    return true;
            }
        }
        return false;
    }

    private static bool Requires(System.Type required, System.Type candidate)
    {
        return required != null && required.IsAssignableFrom(candidate);
    }

    public static void ApplyGhost(GameObject root, Material material, int layer)
    {
        if (root == null) return;

        SetLayerRecursive(root.transform, layer);
        if (material == null) return;

        root.GetComponentsInChildren(true, rendererBuffer);
        foreach (var r in rendererBuffer)
        {
            if (r == null || r is ParticleSystemRenderer || r is LineRenderer || r is TrailRenderer) continue;
            int count = Mathf.Max(1, r.sharedMaterials.Length);
            var mats = new Material[count];
            for (int i = 0; i < count; i++) mats[i] = material;
            r.sharedMaterials = mats;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
        }
        rendererBuffer.Clear();
    }

    public static void SetTint(GameObject root, Color tint, float alpha)
    {
        if (root == null) return;
        block ??= new MaterialPropertyBlock();

        root.GetComponentsInChildren(true, rendererBuffer);
        foreach (var r in rendererBuffer)
        {
            if (r == null || r is ParticleSystemRenderer || r is LineRenderer || r is TrailRenderer) continue;
            r.GetPropertyBlock(block);
            block.SetColor(BaseColorId, tint);
            block.SetColor(ColorId, new Color(tint.r, tint.g, tint.b, alpha));
            block.SetFloat(AlphaId, alpha);
            r.SetPropertyBlock(block);
        }
        rendererBuffer.Clear();
    }

    public static void SetTint(Renderer r, Color tint, float alpha)
    {
        if (r == null) return;
        block ??= new MaterialPropertyBlock();
        r.GetPropertyBlock(block);
        block.SetColor(BaseColorId, tint);
        block.SetColor(ColorId, new Color(tint.r, tint.g, tint.b, alpha));
        block.SetFloat(AlphaId, alpha);
        r.SetPropertyBlock(block);
    }

    public static void SetLayerRecursive(Transform t, int layer)
    {
        if (t == null) return;
        t.gameObject.layer = layer;
        for (int i = 0; i < t.childCount; i++) SetLayerRecursive(t.GetChild(i), layer);
    }
}
