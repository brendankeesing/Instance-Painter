using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

[CustomEditor(typeof(InstanceAnimatorSkew))]
public class InstanceAnimatorSkewEditor : Editor
{
    public InstanceAnimatorSkew skew { get { return (InstanceAnimatorSkew)target; } }

    bool[] _showFoldouts = { };

    public override void OnInspectorGUI()
    {
        Undo.RecordObject(target, "Edit InstanceAnimatorSkew");

        InstanceGroup group = skew.GetComponent<InstanceGroup>();

        skew.maxDistance = Mathf.Max(EditorGUILayout.FloatField("Max Distance", skew.maxDistance), 0.0f);

        // make sure there are the right amount of skew objects
        if (skew.skewObjects.Length != group.objectNames.Count)
            skew.ResetObjects();

        // resize the foldouts list
        if (_showFoldouts.Length != skew.skewObjects.Length)
            System.Array.Resize(ref _showFoldouts, skew.skewObjects.Length);

        for (int i = 0; i < skew.skewObjects.Length; ++i)
        {
            _showFoldouts[i] = EditorGUILayout.Foldout(_showFoldouts[i], group.objectNames[i]);
            if (_showFoldouts[i])
            {
                InstanceAnimatorSkewObject skewobj = skew.skewObjects[i];

                ++EditorGUI.indentLevel;
                skewobj.amount = Mathf.Max(EditorGUILayout.FloatField("Amount", skewobj.amount * 100.0f) / 100.0f, 0.0f);
                skewobj.speed = Mathf.Max(EditorGUILayout.FloatField("Speed", skewobj.speed * 100.0f) / 100.0f, 0.0f);
                --EditorGUI.indentLevel;
            }
        }

        if (GUI.changed)
            EditorUtility.SetDirty(target);
    }
}