using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class TurretIconBaker
{
    private const string OutputFolder = "Assets/Sprites/TurretIcons";
    private const int IconSize = 256;

    [MenuItem("Tools/Shadow Realms/Bake Turret Icons")]
    public static void BakeMenu() => Bake(false);

    [MenuItem("Tools/Shadow Realms/Bake Turret Icons (Force)")]
    public static void BakeForceMenu() => Bake(true);

    public static int Bake(bool force)
    {
        var databases = AssetDatabase.FindAssets("t:TurretDatabaseSO");
        if (databases.Length == 0)
        {
            Debug.LogWarning("[TurretIconBaker] No TurretDatabaseSO found");
            return 0;
        }

        if (!Directory.Exists(OutputFolder)) Directory.CreateDirectory(OutputFolder);

        int baked = 0;
        Scene preview = EditorSceneManager.NewPreviewScene();
        try
        {
            foreach (var guid in databases)
            {
                var db = AssetDatabase.LoadAssetAtPath<TurretDatabaseSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (db == null || db.allTurrets == null) continue;

                foreach (var turret in db.allTurrets)
                {
                    if (turret == null || turret.prefab == null) continue;
                    if (turret.icon != null && !force) continue;

                    string id = !string.IsNullOrEmpty(turret.id) ? turret.id : turret.name;
                    string path = $"{OutputFolder}/{id}.png";

                    if (!RenderIcon(preview, turret.prefab, path)) continue;

                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                    ConfigureImporter(path);

                    turret.icon = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    EditorUtility.SetDirty(turret);
                    baked++;
                }
            }
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(preview);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[TurretIconBaker] Baked {baked} turret icons into {OutputFolder}");
        return baked;
    }

    private static bool RenderIcon(Scene scene, GameObject prefab, string path)
    {
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        if (instance == null) return false;
        instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        var lightGo = new GameObject("IconLight");
        SceneManager.MoveGameObjectToScene(lightGo, scene);
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        var camGo = new GameObject("IconCamera");
        SceneManager.MoveGameObjectToScene(camGo, scene);
        var cam = camGo.AddComponent<Camera>();
        cam.scene = scene;
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
        cam.cullingMask = ~0;
        cam.nearClipPlane = 0.01f;
        cam.farClipPlane = 100f;

        var bounds = CalculateBounds(instance);
        float extent = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
        if (extent <= 0f) extent = 0.5f;

        Quaternion view = Quaternion.Euler(30f, -45f, 0f);
        cam.transform.rotation = view;
        cam.transform.position = bounds.center - view * Vector3.forward * (extent * 4f);
        cam.orthographicSize = extent * 1.15f;

        var rt = RenderTexture.GetTemporary(IconSize, IconSize, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 4;
        var previous = RenderTexture.active;
        bool ok = false;
        try
        {
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(IconSize, IconSize, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, IconSize, IconSize), 0, 0);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            ok = true;
        }
        finally
        {
            cam.targetTexture = null;
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            Object.DestroyImmediate(camGo);
            Object.DestroyImmediate(lightGo);
            Object.DestroyImmediate(instance);
        }

        return ok;
    }

    private static Bounds CalculateBounds(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>();
        bool has = false;
        var bounds = new Bounds(go.transform.position, Vector3.zero);
        foreach (var r in renderers)
        {
            if (r is ParticleSystemRenderer || r is LineRenderer || r is TrailRenderer) continue;
            if (!has) { bounds = r.bounds; has = true; }
            else bounds.Encapsulate(r.bounds);
        }
        return bounds;
    }

    private static void ConfigureImporter(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
    }
}
