using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class NewTurretsCreator
{
    private const string DataFolder = "Assets/Scriptable Object/Turrets/Turrets";
    private const string PrefabFolder = "Assets/Prefabs/Turrets";
    private const string MaterialFolder = "Assets/Materials/Turrets";

    private class Spec
    {
        public string assetName;
        public string id;
        public string displayName;
        public string type;
        public TurretType turretType;
        public float damage;
        public float range;
        public float fireRate;
        public WorldAffinity affinity;
        public string basePrefab;
        public string prefabName;
        public Color tint;
        public Type[] remove;
        public Action<GameObject> configure;
    }

    [MenuItem("Tools/Shadow Realms/Create New Turrets (MD 10)")]
    public static void CreateMenu() => CreateAll();

    public static string CreateAll()
    {
        if (!AssetDatabase.IsValidFolder(MaterialFolder))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
            AssetDatabase.CreateFolder("Assets/Materials", "Turrets");
        }

        var log = new System.Text.StringBuilder();
        var db = AssetDatabase.LoadAssetAtPath<TurretDatabaseSO>($"{DataFolder}/Database.asset");

        foreach (var spec in Specs())
        {
            var so = CreateOrUpdateData(spec);
            var prefab = CreatePrefab(spec, so);
            so.prefab = prefab;
            EditorUtility.SetDirty(so);

            if (db != null && !db.allTurrets.Contains(so))
            {
                db.allTurrets.Add(so);
                EditorUtility.SetDirty(db);
            }
            log.AppendLine($"{spec.displayName}: {AssetDatabase.GetAssetPath(so)} + {AssetDatabase.GetAssetPath(prefab)}");
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[NewTurretsCreator] Created or updated 8 turrets");
        return log.ToString();
    }

    private static TurretDataSO CreateOrUpdateData(Spec spec)
    {
        string path = $"{DataFolder}/{spec.assetName}.asset";
        var so = AssetDatabase.LoadAssetAtPath<TurretDataSO>(path);
        if (so == null)
        {
            so = ScriptableObject.CreateInstance<TurretDataSO>();
            AssetDatabase.CreateAsset(so, path);
        }

        so.id = spec.id;
        so.displayName = spec.displayName;
        so.type = spec.type;
        so.turretType = spec.turretType;
        so.damage = spec.damage;
        so.range = spec.range;
        so.fireRate = spec.fireRate;
        so.projectileSpeed = 1f;
        so.affinity = spec.affinity;
        so.affinityBonus = 0.3f;
        so.offWorldPenalty = 0.2f;
        EditorUtility.SetDirty(so);
        return so;
    }

    private static GameObject CreatePrefab(Spec spec, TurretDataSO so)
    {
        string basePath = $"{PrefabFolder}/{spec.basePrefab}.prefab";
        string path = $"{PrefabFolder}/{spec.prefabName}.prefab";

        var root = PrefabUtility.LoadPrefabContents(basePath);
        try
        {
            root.name = spec.prefabName;

            foreach (var t in spec.remove)
            {
                foreach (var c in root.GetComponents(t))
                    UnityEngine.Object.DestroyImmediate(c, true);
            }

            var holder = root.GetComponent<TurretDataHolder>();
            if (holder != null) holder.turretDataSO = so;

            var partRenderers = new HashSet<Renderer>();
            foreach (var r in root.GetComponentsInChildren<Renderer>(true)) partRenderers.Add(r);

            spec.configure?.Invoke(root);

            TintMaterials(root, spec, partRenderers);

            return PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void TintMaterials(GameObject root, Spec spec, HashSet<Renderer> originalRenderers)
    {
        var cache = new Dictionary<Material, Material>();
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
        {
            if (r == null || !originalRenderers.Contains(r)) continue;
            if (r is LineRenderer || r is ParticleSystemRenderer || r is TrailRenderer || r is SpriteRenderer) continue;

            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                var src = mats[i];
                if (src == null) continue;
                if (!cache.TryGetValue(src, out var tinted))
                {
                    string matPath = $"{MaterialFolder}/{spec.prefabName}_{SafeName(src.name)}.mat";
                    tinted = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                    if (tinted == null)
                    {
                        tinted = new Material(src);
                        AssetDatabase.CreateAsset(tinted, matPath);
                    }
                    else
                    {
                        tinted.CopyPropertiesFromMaterial(src);
                    }
                    string colorProp = src.HasProperty("_BaseColor") ? "_BaseColor" : (src.HasProperty("_Color") ? "_Color" : null);
                    if (colorProp != null)
                    {
                        Color orig = src.GetColor(colorProp);
                        float brightness = Mathf.Clamp(orig.grayscale * 1.6f, 0.12f, 1.2f);
                        Color target = new Color(spec.tint.r * brightness, spec.tint.g * brightness, spec.tint.b * brightness, orig.a);
                        tinted.SetColor(colorProp, Color.Lerp(orig, target, 0.7f));
                    }
                    EditorUtility.SetDirty(tinted);
                    cache[src] = tinted;
                }
                mats[i] = tinted;
            }
            r.sharedMaterials = mats;
        }
    }

    private static string SafeName(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
        return name.Replace(' ', '_');
    }

    private static T Replace<T>(GameObject root) where T : Component
    {
        var existing = root.GetComponent<TurretTargeting>();
        TurretTargeting.TargetingMode mode = existing != null ? existing.mode : TurretTargeting.TargetingMode.Closest;
        if (existing != null) UnityEngine.Object.DestroyImmediate(existing, true);
        var added = root.AddComponent<T>();
        if (added is TurretTargeting tt) tt.mode = mode;
        return added;
    }

    private static void SetField(UnityEngine.Object target, string field, object value)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(field);
        if (prop == null) throw new Exception($"Field {field} not found on {target.GetType().Name}");
        switch (value)
        {
            case float f: prop.floatValue = f; break;
            case int i: prop.intValue = i; break;
            case bool b: prop.boolValue = b; break;
            case Enum e: prop.enumValueIndex = Convert.ToInt32(e); break;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Material GetMaterial(string name, Color color, bool transparent)
    {
        string path = $"{MaterialFolder}/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;

        mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetColor("_BaseColor", color);
        if (transparent)
        {
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = 3000;
        }
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    private static GameObject AddPart(GameObject root, string name, PrimitiveType type, Vector3 localPos, Vector3 localScale, Material mat)
    {
        var existing = root.transform.Find(name);
        if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);

        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(root.transform, false);
        go.layer = root.layer;
        Vector3 inv = new Vector3(1f / Mathf.Max(0.0001f, root.transform.lossyScale.x), 1f / Mathf.Max(0.0001f, root.transform.lossyScale.y), 1f / Mathf.Max(0.0001f, root.transform.lossyScale.z));
        go.transform.localPosition = Vector3.Scale(localPos, inv);
        go.transform.localScale = Vector3.Scale(localScale, inv);
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    private static IEnumerable<Spec> Specs()
    {
        yield return new Spec
        {
            assetName = "MortarTurret", id = "turret_mortar", displayName = "Mortar", type = "aoe", turretType = TurretType.Mortar,
            damage = 14f, range = 9f, fireRate = 0.35f, affinity = WorldAffinity.Normal,
            basePrefab = "TurretFirePrefab", prefabName = "TurretMortarPrefab", tint = new Color(0.75f, 0.55f, 0.35f),
            remove = new[] { typeof(FireTurret) },
            configure = root =>
            {
                var m = root.AddComponent<MortarTurret>();
                SetField(m, "explosionRadius", 1.5f);
                var t = Replace<MinRangeTargeting>(root);
                SetField(t, "minRange", 2.5f);
            }
        };
        yield return new Spec
        {
            assetName = "BallistaTurret", id = "turret_ballista", displayName = "Ballista", type = "attack", turretType = TurretType.Ballista,
            damage = 10f, range = 11f, fireRate = 0.5f, affinity = WorldAffinity.Neutral,
            basePrefab = "TurretSniper", prefabName = "TurretBallistaPrefab", tint = new Color(0.85f, 0.65f, 0.42f),
            remove = new[] { typeof(BasicRaycastShooter) },
            configure = root =>
            {
                root.AddComponent<BallistaTurret>();
                var t = Replace<PriorityTargeting>(root);
                SetField(t, "priority", PriorityTargeting.Priority.MostAdvanced);
            }
        };
        yield return new Spec
        {
            assetName = "RiftCannonTurret", id = "turret_rift_cannon", displayName = "Rift Cannon", type = "attack", turretType = TurretType.RiftCannon,
            damage = 6f, range = 7f, fireRate = 1f, affinity = WorldAffinity.Other,
            basePrefab = "TurretTeslaPrefab", prefabName = "TurretRiftCannonPrefab", tint = new Color(0.7f, 0.45f, 1f),
            remove = new[] { typeof(TeslaTurret) },
            configure = root =>
            {
                root.AddComponent<BasicRaycastShooter>();
                root.AddComponent<RiftCannonTurret>();
            }
        };
        yield return new Spec
        {
            assetName = "WarBannerTurret", id = "turret_war_banner", displayName = "War Banner", type = "support", turretType = TurretType.WarBanner,
            damage = 0f, range = 2.5f, fireRate = 0f, affinity = WorldAffinity.Neutral,
            basePrefab = "TurretBasicPrefab", prefabName = "TurretWarBannerPrefab", tint = new Color(1f, 0.5f, 0.4f),
            remove = new[] { typeof(BasicRaycastShooter), typeof(TurretShooter), typeof(TurretTargeting) },
            configure = root =>
            {
                root.AddComponent<WarBannerAura>();
                var pole = GetMaterial("M_BannerPole", new Color(0.35f, 0.25f, 0.15f), false);
                var cloth = GetMaterial("M_BannerCloth", new Color(0.85f, 0.15f, 0.15f), false);
                AddPart(root, "BannerPole", PrimitiveType.Cylinder, new Vector3(0f, 1.4f, 0f), new Vector3(0.08f, 1f, 0.08f), pole);
                AddPart(root, "BannerCloth", PrimitiveType.Cube, new Vector3(0.3f, 2f, 0f), new Vector3(0.6f, 0.45f, 0.04f), cloth);
            }
        };
        yield return new Spec
        {
            assetName = "PurifierTurret", id = "turret_purifier", displayName = "Purifier", type = "attack", turretType = TurretType.Purifier,
            damage = 8f, range = 7f, fireRate = 5f, affinity = WorldAffinity.Normal,
            basePrefab = "TurretTeslaPrefab", prefabName = "TurretPurifierPrefab", tint = new Color(1f, 0.95f, 0.7f),
            remove = new[] { typeof(TeslaTurret) },
            configure = root =>
            {
                root.AddComponent<PurifierTurret>();
                var t = Replace<PriorityTargeting>(root);
                SetField(t, "priority", PriorityTargeting.Priority.Shielded);
            }
        };
        yield return new Spec
        {
            assetName = "GuillotineTurret", id = "turret_guillotine", displayName = "Guillotine", type = "attack", turretType = TurretType.Guillotine,
            damage = 8f, range = 5f, fireRate = 0.25f, affinity = WorldAffinity.Other,
            basePrefab = "TurretSniper", prefabName = "TurretGuillotinePrefab", tint = new Color(0.75f, 0.8f, 0.9f),
            remove = new[] { typeof(BasicRaycastShooter) },
            configure = root =>
            {
                var g = root.AddComponent<GuillotineTurret>();
                SetField(g, "bossPercent", 4f);
                var t = Replace<PriorityTargeting>(root);
                SetField(t, "priority", PriorityTargeting.Priority.HighestMaxHealth);
                var steel = GetMaterial("M_GuillotineBlade", new Color(0.8f, 0.85f, 0.9f), false);
                AddPart(root, "Blade", PrimitiveType.Cube, new Vector3(0f, 1.9f, 0f), new Vector3(0.7f, 0.12f, 0.05f), steel);
            }
        };
        yield return new Spec
        {
            assetName = "ReaperTurret", id = "turret_reaper", displayName = "Reaper", type = "attack", turretType = TurretType.Reaper,
            damage = 5f, range = 6f, fireRate = 1.2f, affinity = WorldAffinity.Neutral,
            basePrefab = "TurretBasicPrefab", prefabName = "TurretReaperPrefab", tint = new Color(0.5f, 0.35f, 0.65f),
            remove = new[] { typeof(BasicRaycastShooter) },
            configure = root =>
            {
                root.AddComponent<ReaperTurret>();
            }
        };
        yield return new Spec
        {
            assetName = "WardenTurret", id = "turret_warden", displayName = "Warden", type = "support", turretType = TurretType.Warden,
            damage = 0f, range = 2.5f, fireRate = 0f, affinity = WorldAffinity.Neutral,
            basePrefab = "TurretEnergyPrefab", prefabName = "TurretWardenPrefab", tint = new Color(0.45f, 0.9f, 1f),
            remove = new[] { typeof(TurretEnergy), typeof(TurretEnergyMaterialSwitcher) },
            configure = root =>
            {
                root.AddComponent<WardenAura>();
                var dome = GetMaterial("M_WardenDome", new Color(0.4f, 0.9f, 1f, 0.25f), true);
                AddPart(root, "WardenDome", PrimitiveType.Sphere, new Vector3(0f, 0.9f, 0f), new Vector3(1.4f, 1.4f, 1.4f), dome);
            }
        };
    }
}
