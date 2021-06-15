using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

[CustomEditor(typeof(InstanceAnimatorSway))]
public class InstanceAnimatorSwayEditor : Editor
{
    public InstanceAnimatorSway sway { get { return (InstanceAnimatorSway)target; } }

    bool[] _showFoldouts = { };

    public override void OnInspectorGUI()
    {
        Undo.RecordObject(target, "Edit InstanceAnimatorSway");

        InstanceGroup group = sway.GetComponent<InstanceGroup>();

        sway.maxDistance = Mathf.Max(EditorGUILayout.FloatField("Max Distance", sway.maxDistance), 0.0f);

        // make sure there are the right amount of sway objects
        if (sway.swayObjects.Length != group.objectNames.Count)
            sway.ResetObjects();

        // resize the foldouts list
        if (_showFoldouts.Length != sway.swayObjects.Length)
            System.Array.Resize(ref _showFoldouts, sway.swayObjects.Length);

        for (int i = 0; i < sway.swayObjects.Length; ++i)
        {
            _showFoldouts[i] = EditorGUILayout.Foldout(_showFoldouts[i], group.objectNames[i]);
            if (_showFoldouts[i])
            {
                InstanceAnimatorSwayObject swayobj = sway.swayObjects[i];

                ++EditorGUI.indentLevel;
                swayobj.amount = Mathf.Max(EditorGUILayout.FloatField("Amount", swayobj.amount), 0.0f);
                swayobj.speed = Mathf.Max(EditorGUILayout.FloatField("Speed", swayobj.speed), 0.0f);
                --EditorGUI.indentLevel;
            }
        }

        if (GUI.changed)
            EditorUtility.SetDirty(target);
    }
}