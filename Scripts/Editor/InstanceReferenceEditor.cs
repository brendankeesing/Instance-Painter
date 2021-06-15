using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

[CustomEditor(typeof(InstanceReference))]
public class InstanceReferenceEditor : Editor
{
    public InstanceReference reference { get { return (InstanceReference)target; } }
    public InstanceObject instance { get { return reference.instance; } }
    public InstanceGroup group { get { return instance.group; } }

    int _referenceIndex = -1;

    public override void OnInspectorGUI()
    {
        Undo.RecordObject(target, "Edit InstanceReference");

        // This should, theoretically, never happen, but it happens a lot throughout development.
        // This could happen if you delete the InstanceGroup or InstanceGameObject components. 
        if (instance == null)
        {
            EditorGUILayout.LabelField("Reference is not linked to an instance.");
            EditorGUILayout.LabelField("You probably want to delete this component/gameobject.");
            return;
        }

        if (GUILayout.Button("Select Parent"))
            Selection.activeGameObject = group.gameObject;

        if (GUILayout.Button("Unreference Instance"))
        {
            reference.UnlinkFromInstanceGroup();
            DestroyImmediate(reference);
            return;
        }

        int oldobjectid = instance.objectID;
        instance.objectID = EditorGUILayout.Popup("Object", instance.objectID, group.objectNames.ToArray());
        if (instance.objectID != oldobjectid && instance.objectID >= 0 && instance.objectID < group.objectNames.Count)
            reference.gameObject.name = group.objectNames[instance.objectID];
        instance.skew = EditorGUILayout.Vector2Field("Skew", instance.skew);
        instance.color = EditorGUILayout.ColorField("Color", instance.color);
        if (_referenceIndex < 0)
            _referenceIndex = instance.group.IndexOfInstance(instance);
        EditorGUILayout.LabelField("Instance Index", _referenceIndex.ToString());

        if (GUI.changed)
            EditorUtility.SetDirty(instance.group);
    }
}