using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TileLayout))]
public class TileLayoutEditor : Editor
{
    private TileLayout layout;

    private void OnEnable()
    {
        layout = (TileLayout)target;
        EnsureTilesSize();
        if (layout.exits == null) layout.exits = new System.Collections.Generic.List<Vector2Int>();
    }

    private void EnsureTilesSize()
    {
        int desired = Mathf.Max(1, layout.gridWidth) * Mathf.Max(1, layout.gridHeight);
        if (layout.tiles == null) layout.tiles = new System.Collections.Generic.List<TileLayout.TileEntry>();
        if (layout.tiles.Count != desired)
        {
            layout.tiles.Clear();
            for (int y = 0; y < layout.gridHeight; y++)
                for (int x = 0; x < layout.gridWidth; x++)
                    layout.tiles.Add(new TileLayout.TileEntry { grid = new Vector2Int(x, y), type = TileLayout.TileType.Grass });
        }
    }

    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.LabelField("Configuración de Grid", EditorStyles.boldLabel);
        layout.gridWidth = Mathf.Max(1, EditorGUILayout.IntField("Grid Width", layout.gridWidth));
        layout.gridHeight = Mathf.Max(1, EditorGUILayout.IntField("Grid Height", layout.gridHeight));
        layout.cellSize = Mathf.Max(0.1f, EditorGUILayout.FloatField("Cell Size", layout.cellSize));
        layout.origin = EditorGUILayout.Vector3Field("Origin", layout.origin);

        EditorGUILayout.Space();
        EnsureTilesSize();

        EditorGUILayout.LabelField("Tiles (click para Entry / Exit)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Elegí Grass/Path. Botón Entry (único). Botón Exit (toggle, puede haber varios).", MessageType.Info);

        for (int y = layout.gridHeight - 1; y >= 0; y--)
        {
            EditorGUILayout.BeginHorizontal();
            for (int x = 0; x < layout.gridWidth; x++)
            {
                int idx = y * layout.gridWidth + x;
                var entry = layout.tiles[idx];

                EditorGUILayout.BeginVertical("box", GUILayout.Width(110));

                GUILayout.Label($"({x},{y})", EditorStyles.centeredGreyMiniLabel);

                entry.type = (TileLayout.TileType)EditorGUILayout.EnumPopup(entry.type);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Entry", GUILayout.Width(50)))
                    layout.entry = new Vector2Int(x, y);

                bool isExit = layout.exits.Contains(new Vector2Int(x, y));
                string exitLabel = isExit ? "Exit ✓" : "Exit";
                if (GUILayout.Button(exitLabel, GUILayout.Width(50)))
                {
                    var p = new Vector2Int(x, y);
                    if (isExit) layout.exits.Remove(p);
                    else layout.exits.Add(p);
                }
                EditorGUILayout.EndHorizontal();

                // Indicadores
                if (layout.entry == new Vector2Int(x, y))
                    EditorGUILayout.LabelField("● ENTRY", EditorStyles.boldLabel);

                if (layout.exits.Contains(new Vector2Int(x, y)))
                    EditorGUILayout.LabelField("● EXIT", EditorStyles.boldLabel);

                layout.tiles[idx] = entry;
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Endpoints", EditorStyles.boldLabel);
        layout.entry = Vector2IntField("Entry", layout.entry);

        // Mostrar exits actuales
        EditorGUILayout.LabelField($"Exits ({layout.exits.Count})");
        for (int i = 0; i < layout.exits.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            layout.exits[i] = Vector2IntField($"Exit {i + 1}", layout.exits[i]);
            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                layout.exits.RemoveAt(i);
                i--;
            }
            EditorGUILayout.EndHorizontal();
        }

        if (GUILayout.Button("Autodetectar Entry y Exits (según vecinos Path)"))
        {
            if (layout.AutoDetectEndpoints(out var autoE, out var autoXs))
            {
                layout.entry = autoE;
                layout.exits = autoXs;
                Debug.Log($"[TileLayoutEditor] Entry {autoE} | Exits: {autoXs.Count}");
            }
            else
            {
                Debug.LogWarning("[TileLayoutEditor] No se pudieron autodetectar endpoints.");
            }
        }

        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(layout);
        }
    }

    private Vector2Int Vector2IntField(string label, Vector2Int v)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(60));
        int x = EditorGUILayout.IntField("X", v.x);
        int y = EditorGUILayout.IntField("Y", v.y);
        EditorGUILayout.EndHorizontal();
        return new Vector2Int(x, y);
    }
}
