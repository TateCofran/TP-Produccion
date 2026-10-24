#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GameManager))]
public class GameManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(10);
        EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);

        var gm = (GameManager)target;

        int newWave = EditorGUILayout.IntField("Wave to jump to", gm.DebugWaveToJump);
        if (newWave != gm.DebugWaveToJump)
        {
            gm.DebugWaveToJump = Mathf.Max(1, newWave);
            EditorUtility.SetDirty(gm);
        }

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Jump To Wave"))
        {
            gm.Debug_Editor_JumpToWave();
        }
        if (GUILayout.Button("Force Win"))
        {
            gm.Debug_Editor_ForceWin();
        }
        if (GUILayout.Button("Force Lose"))
        {
            gm.Debug_Editor_ForceLose();
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(12);

        EditorGUILayout.LabelField("Debug Cheats Extra", EditorStyles.boldLabel);

        if (GUILayout.Button("Complete Wave (Kill Enemies)"))
        {
            gm.Debug_Editor_CompleteCurrentWaveAndKillEnemies();
        }

        if (GUILayout.Button("Toggle Core Infinite Health"))
        {
            gm.Debug_Editor_ToggleCoreInfiniteHealth();
        }
    }
}
#endif
