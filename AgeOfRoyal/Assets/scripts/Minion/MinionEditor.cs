using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Minion))]
public class MinionEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); // Draw everything as normal

        Minion minion = (Minion)target;
        /*
                if (Application.isPlaying)
                {*/
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("=== Runtime Buff Info ===", EditorStyles.boldLabel);

        try
        {
            UnitPowerUp total = minion.TotalBuff;
            EditorGUILayout.LabelField("Total Buffs:", total.Short);
            minion.Buffs.ForEach(b =>
            {
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.LabelField($"Buff#{minion.Buffs.IndexOf(b)}:");
                EditorGUILayout.Space();
                EditorGUILayout.LabelField($"Buff#{minion.Buffs.IndexOf(b)}, Source:", b.Source.ToString());
                EditorGUILayout.LabelField($"Buff#{minion.Buffs.IndexOf(b)}, SourceId:", b.SourceId.ToString());

                EditorGUILayout.Space();
                EditorGUILayout.LabelField($"Buff#{minion.Buffs.IndexOf(b)}, Type:", b.BuffType.ToString());
                EditorGUILayout.LabelField($"Buff#{minion.Buffs.IndexOf(b)}, Powerup:", b.PowerUp.Short);
                EditorGUILayout.LabelField($"Buff#{minion.Buffs.IndexOf(b)}, Heal:", b.Heal.ToString());
                EditorGUILayout.LabelField($"Buff#{minion.Buffs.IndexOf(b)}, End:", b.EndTime.ToString());
            });
        }
        catch
        {
            EditorGUILayout.HelpBox("Failed to calculate TotalBuff (likely during edit-time).", MessageType.Info);
        }
    /* }
        else
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Runtime-only info (like TotalBuff) will be visible during Play mode.", MessageType.None);
        }*/
    }
}
