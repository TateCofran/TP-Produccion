#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GameManager))]
public class GameManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Inspector original
        DrawDefaultInspector();

        GUILayout.Space(10);
        EditorGUILayout.LabelField("Debug / Testeo", EditorStyles.boldLabel);

        var gm = (GameManager)target;

        // Campo de oleada
        int newWave = EditorGUILayout.IntField("Oleada a saltar", gm.DebugWaveToJump);
        if (newWave != gm.DebugWaveToJump)
        {
            gm.DebugWaveToJump = Mathf.Max(1, newWave);
            EditorUtility.SetDirty(gm);
        }

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Saltar a Oleada"))
        {
            gm.Debug_Editor_JumpToWave();
        }
        if (GUILayout.Button("Forzar Victoria"))
        {
            gm.Debug_Editor_ForceWin();
        }
        if (GUILayout.Button("Forzar Derrota"))
        {
            gm.Debug_Editor_ForceLose();
        }
        GUILayout.EndHorizontal();
    }
}
#endif
