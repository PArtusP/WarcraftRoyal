#if UNITY_EDITOR
using System.Collections.Generic;
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
        EditorGUILayout.LabelField("target link:", minion.TargetHealthLink != null ?  minion.TargetHealthLink.source.ToString() : "none");
        EditorGUILayout.LabelField("source link:", minion.SourceHealthLink != null ? minion.SourceHealthLink.source.ToString() : "none");
        EditorGUILayout.LabelField("===----------===", EditorStyles.boldLabel);

        try
        {
            UnitPowerUp total = minion.TotalBuff;
            EditorGUILayout.LabelField("Total Power up:", total.Short);
            EditorGUILayout.LabelField("Cached Power up:", minion.CachedBuff.value.Short);
            minion.Buffs.ForEach(b =>
            { 
                EditorGUILayout.Space(); 
                EditorGUILayout.LabelField($"Buff#{minion.Buffs.IndexOf(b)}: ", b.ToString());
                EditorGUILayout.LabelField($"Buff#{minion.Buffs.IndexOf(b)}, Source:", b.Source.ToString());
                EditorGUILayout.LabelField($"Buff#{minion.Buffs.IndexOf(b)}, SourceId:", b.SourceId.ToString()); 
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
#endif