using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

using System.Collections.Generic;

[CustomEditor(typeof(InstanceGroup))]
public class InstanceGroupEditor : Editor
{
    public InstanceGroup group { get { return (InstanceGroup)target; } }

    // This is an undocumented unity feature and could be removed in future versions!!
    ReorderableList _objectList;

    bool _showObjectListFoldout = false;
    bool _showInstanceFoldout = false;
    int _displayedInstance = 0;

    void OnEnable()
    {
        _objectList = new ReorderableList(group.objectNames, typeof(string), true, false, true, true);
        _objectList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            int instancecount = 0;
            for (int i = 0; i < group.count; ++i)
            {
                if (group[i].objectID == index)
                    ++instancecount;
            }
            rect.y += 2;
            float lw = EditorGUIUtility.labelWidth;
            EditorGUI.LabelField(new Rect(rect.x, rect.y, lw, EditorGUIUtility.singleLineHeight), "ObjectID " + index.ToString() + " (" + instancecount.ToString() + ")");
            group.objectNames[index] = EditorGUI.TextField(new Rect(rect.x + lw, rect.y, rect.width - lw, EditorGUIUtility.singleLineHeight), group.objectNames[index]);
        };
        _objectList.onAddCallback = (ReorderableList list) => { group.objectNames.Add("New Object"); };
    }

    public override void OnInspectorGUI()
    {
        Undo.RecordObject(target, "Edit InstanceGroup");

        _showObjectListFoldout = EditorGUILayout.Foldout(_showObjectListFoldout, string.Format("Objects ({0})", group.objectNames.Count));
        if (_showObjectListFoldout)
            _objectList.DoLayoutList();

        _showInstanceFoldout = EditorGUILayout.Foldout(_showInstanceFoldout, string.Format("Instances ({0})", group.count));
        if (_showInstanceFoldout )
        {
            if (GUILayout.Button("Add"))
                group.AddInstance(new InstanceObject(group));
            if (group.count > 0)
            {
                InstanceObject instance = group[_displayedInstance];
                if (GUILayout.Button("Remove"))
                    group.RemoveInstance(instance);
                else
                {
                    _displayedInstance = Mathf.Clamp(EditorGUILayout.IntField("Index", _displayedInstance), 0, group.count - 1);
                    instance.objectID = EditorGUILayout.Popup("Object", instance.objectID, group.objectNames.ToArray());
                    instance.position = EditorGUILayout.Vector3Field("Position", instance.position);
                    instance.rotation = Quaternion.Euler(EditorGUILayout.Vector3Field("Rotation", instance.rotation.eulerAngles));
                    instance.scale = EditorGUILayout.Vector3Field("Scale", instance.scale);
                    instance.skew = EditorGUILayout.Vector2Field("Skew", instance.skew);
                    instance.color = EditorGUILayout.ColorField("Color", instance.color);
                    if (instance.reference && GUILayout.Button("Go To Reference"))
                        Selection.activeGameObject = instance.reference.gameObject;
                }
            }
            else
                EditorGUILayout.HelpBox("Click Add to create an instance", MessageType.None);
        }

        if (GUI.changed)
            EditorUtility.SetDirty(target);
    }
}
