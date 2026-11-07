using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class TileVariantsCreator
{
    private const string TilesFolder = "Assets/Scriptable Object/Tiles";
    private const string SpecialFolder = "Assets/Scriptable Object/Tiles/Special";
    private const string PrefabFolder = "Assets/Prefabs";

    [MenuItem("Tools/Shadow Realms/Create Tile Variants (MD 11)")]
    public static void CreateMenu() => Debug.Log("[TileVariantsCreator] " + CreateAll());

    public static string CreateAll()
    {
        var log = new System.Text.StringBuilder();
        log.AppendLine(MarkMirrors());

        if (!AssetDatabase.IsValidFolder(SpecialFolder)) AssetDatabase.CreateFolder(TilesFolder, "Special");

        var dps = Load("Dps/Tile-UpDPS");
        var slow = Load("Slow/Tile-UpSlow");
        var stun = Load("Stun/Tile-UpStun");
        float dpsValue = dps != null ? dps.defaultPathMods.dps : 5f;
        float stunValue = stun != null ? stun.defaultPathMods.stun : 5f;
        float slowValue = slow != null ? slow.defaultPathMods.slow : 0.35f;

        var up = new[] { V(2, 0), V(2, 1), V(2, 2), V(2, 3), V(2, 4) };
        var left = new[] { V(2, 0), V(2, 1), V(2, 2), V(1, 2), V(0, 2) };

        Create("Tile-ForkT", V(2, 0), new[] { V(0, 2), V(4, 2) },
            new[] { V(2, 0), V(2, 1), V(2, 2), V(1, 2), V(0, 2), V(3, 2), V(4, 2) }, default, null, log);

        var deadEnd = Create("Tile-DeadEnd", V(2, 0), new Vector2Int[0], new[] { V(2, 0), V(2, 1), V(2, 2) }, default, null, log);
        deadEnd.onPlaceEssenceReward = 15;
        EditorUtility.SetDirty(deadEnd);

        Create("Tile-Serpent", V(1, 0), new[] { V(1, 4) },
            new[] { V(1, 0), V(1, 1), V(2, 1), V(3, 1), V(3, 2), V(3, 3), V(2, 3), V(1, 3), V(1, 4) }, default, null, log);

        Create("Tile-Plaza", V(0, 0), new[] { V(0, 4) },
            new[] { V(0, 0), V(0, 1), V(0, 2), V(0, 3), V(0, 4) }, default, null, log);

        Create("Tile-Hybrid-SlowDps", V(2, 0), new[] { V(2, 4) }, up,
            new TileLayout.PathModifiers { dps = dpsValue },
            new Dictionary<Vector2Int, TileLayout.PathModifiers>
            {
                { V(2, 0), new TileLayout.PathModifiers { slow = 0.3f } },
                { V(2, 1), new TileLayout.PathModifiers { slow = 0.3f } }
            }, log);

        Create("Tile-Hybrid-StunDps", V(2, 0), new[] { V(0, 2) }, left,
            new TileLayout.PathModifiers { dps = dpsValue },
            new Dictionary<Vector2Int, TileLayout.PathModifiers>
            {
                { V(2, 0), new TileLayout.PathModifiers { stun = stunValue } }
            }, log);

        var dpsNormal = Create("Tile-UpDPS-Normal", V(2, 0), new[] { V(2, 4) }, up, new TileLayout.PathModifiers { dps = dpsValue }, null, log);
        dpsNormal.activeIn = WorldAffinity.Normal;
        dpsNormal.lockedStrengthMultiplier = 2f;
        EditorUtility.SetDirty(dpsNormal);

        var slowOther = Create("Tile-UpSlow-Other", V(2, 0), new[] { V(2, 4) }, up, new TileLayout.PathModifiers { slow = slowValue }, null, log);
        slowOther.activeIn = WorldAffinity.Other;
        slowOther.lockedStrengthMultiplier = 2f;
        EditorUtility.SetDirty(slowOther);

        Create("Tile-UpExposure", V(2, 0), new[] { V(2, 4) }, up, new TileLayout.PathModifiers { exposure = true }, null, log);
        Create("Tile-LeftAntiHeal", V(2, 0), new[] { V(0, 2) }, left, new TileLayout.PathModifiers { antiHeal = true }, null, log);

        var exposurePrefab = CreateCellPrefab("PathExposure", "PathSlow", new Color(0.62f, 0.32f, 0.95f), log);
        var antiHealPrefab = CreateCellPrefab("PathAntiHeal", "PathDps", new Color(0.12f, 0.35f, 0.14f), log);

        AssetDatabase.SaveAssets();
        TileIconGenerator.ClearCache();
        return log.ToString();
    }

    private static Vector2Int V(int x, int y) => new Vector2Int(x, y);

    private static TileLayout Load(string relative) =>
        AssetDatabase.LoadAssetAtPath<TileLayout>($"{TilesFolder}/{relative}.asset");

    public static string MarkMirrors()
    {
        var all = AssetDatabase.FindAssets("t:TileLayout").Select(g => AssetDatabase.LoadAssetAtPath<TileLayout>(AssetDatabase.GUIDToAssetPath(g))).Where(l => l != null).ToList();
        var lines = new List<string>();
        var processed = new HashSet<TileLayout>();

        foreach (var a in all.OrderBy(l => l.name.Contains("Right") ? 1 : 0).ThenBy(l => l.name))
        {
            if (processed.Contains(a)) continue;
            foreach (var b in all)
            {
                if (b == a || processed.Contains(b) || b.IsMirror) continue;
                if (!IsHorizontalMirror(a, b)) continue;
                if (IsSymmetric(a)) continue;

                var original = a.name.Contains("Right") ? b : a;
                var mirror = original == a ? b : a;
                mirror.SetMirrorOf(original);
                EditorUtility.SetDirty(mirror);
                processed.Add(a);
                processed.Add(b);
                lines.Add($"{mirror.name} -> {original.name}");
                break;
            }
        }

        return "Mirrors: " + (lines.Count == 0 ? "none" : string.Join(", ", lines));
    }

    private static bool IsSymmetric(TileLayout l) => IsHorizontalMirror(l, l);

    private static bool IsHorizontalMirror(TileLayout a, TileLayout b)
    {
        if (a.gridWidth != b.gridWidth || a.gridHeight != b.gridHeight) return false;
        int w = a.gridWidth;
        Vector2Int M(Vector2Int c) => new Vector2Int(w - 1 - c.x, c.y);

        for (int y = 0; y < a.gridHeight; y++)
            for (int x = 0; x < w; x++)
            {
                var c = V(x, y);
                bool pa = a.IsPath(c);
                if (pa != b.IsPath(M(c))) return false;
                if (!pa) continue;
                var ma = a.GetPathModifiers(c);
                var mb = b.GetPathModifiers(M(c));
                if (!Mathf.Approximately(ma.dps, mb.dps) || !Mathf.Approximately(ma.slow, mb.slow) || !Mathf.Approximately(ma.stun, mb.stun) || ma.exposure != mb.exposure || ma.antiHeal != mb.antiHeal) return false;
            }

        if (M(a.entry) != b.entry) return false;
        var ea = new HashSet<Vector2Int>(a.exits.Select(M));
        var eb = new HashSet<Vector2Int>(b.exits);
        return ea.SetEquals(eb) && a.activeIn == b.activeIn;
    }

    private static TileLayout Create(string name, Vector2Int entry, Vector2Int[] exits, Vector2Int[] path,
        TileLayout.PathModifiers defaults, Dictionary<Vector2Int, TileLayout.PathModifiers> overrides, System.Text.StringBuilder log)
    {
        string path_ = $"{SpecialFolder}/{name}.asset";
        var layout = AssetDatabase.LoadAssetAtPath<TileLayout>(path_);
        if (layout == null)
        {
            layout = ScriptableObject.CreateInstance<TileLayout>();
            AssetDatabase.CreateAsset(layout, path_);
        }

        layout.gridWidth = 5;
        layout.gridHeight = 5;
        layout.cellSize = 1f;
        layout.origin = Vector3.zero;
        layout.hasCore = false;
        layout.coreCell = new Vector2Int(-1, -1);
        layout.entry = entry;
        layout.exits = new List<Vector2Int>(exits);
        layout.tiles = path.Select(p => new TileLayout.TileEntry { grid = p, type = TileLayout.TileType.Path }).ToList();
        layout.defaultPathMods = defaults;
        layout.perCellOverrides = overrides == null
            ? new List<TileLayout.PathModOverride>()
            : overrides.Select(kv => new TileLayout.PathModOverride { grid = kv.Key, mods = kv.Value }).ToList();

        typeof(TileLayout).GetMethod("BuildCaches", BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(layout, null);
        EditorUtility.SetDirty(layout);
        log.AppendLine($"{name}: entry {entry}, {exits.Length} exits, {path.Length} path cells");
        return layout;
    }

    private static GameObject CreateCellPrefab(string name, string baseName, Color color, System.Text.StringBuilder log)
    {
        string basePath = $"{PrefabFolder}/{baseName}.prefab";
        string path = $"{PrefabFolder}/{name}.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(basePath) == null)
        {
            log.AppendLine($"Base prefab {basePath} not found");
            return null;
        }

        var root = PrefabUtility.LoadPrefabContents(basePath);
        try
        {
            root.name = name;
            string matFolder = "Assets/Materials/Tiles";
            if (!AssetDatabase.IsValidFolder(matFolder)) AssetDatabase.CreateFolder("Assets/Materials", "Tiles");
            var cache = new Dictionary<Material, Material>();
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    var src = mats[i];
                    if (src == null) continue;
                    if (!cache.TryGetValue(src, out var copy))
                    {
                        string matPath = $"{matFolder}/{name}_{src.name.Replace(' ', '_')}.mat";
                        copy = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                        if (copy == null)
                        {
                            copy = new Material(src);
                            AssetDatabase.CreateAsset(copy, matPath);
                        }
                        string prop = copy.HasProperty("_BaseColor") ? "_BaseColor" : (copy.HasProperty("_Color") ? "_Color" : null);
                        if (prop != null)
                        {
                            Color orig = src.GetColor(prop);
                            float brightness = Mathf.Clamp(orig.grayscale * 1.5f, 0.2f, 1.2f);
                            copy.SetColor(prop, Color.Lerp(orig, color * brightness, 0.8f));
                        }
                        EditorUtility.SetDirty(copy);
                        cache[src] = copy;
                    }
                    mats[i] = copy;
                }
                r.sharedMaterials = mats;
            }
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            log.AppendLine($"Prefab {path}");
            return prefab;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
