using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(InstanceGameObject))]
public class InstanceGameObjectEditor : Editor
{
    InstanceGameObject _target { get { return (InstanceGameObject)target; } }

    public override void OnInspectorGUI()
    {
        Undo.RecordObject(target, "Edit InstanceGameObject");

        // make sure there is one game object for every object name in group
        if (_target.group.objectNames.Count != _target.prefabObjects.Count)
            _target.OnObjectChange();

        for (int i = 0; i < _target.prefabObjects.Count; ++i)
            _target.prefabObjects[i] = (GameObject)EditorGUILayout.ObjectField(_target.group.objectNames[i], _target.prefabObjects[i], typeof(GameObject), true);

        _target.useReferences = EditorGUILayout.Toggle("Use References", _target.useReferences);

        if (GUILayout.Button("Unlink All References") && EditorUtility.DisplayDialog("Unlink All References", "Are you sure you want to separate all game object instance from the Instance Group?", "Yes", "No"))
            _target.UnlinkAllReferences();

        if (GUI.changed)
            EditorUtility.SetDirty(_target);
    }

    [MenuItem("GameObject/Other/InstanceGameObject")]
    static void CreateObject()
    {
        GameObject obj = new GameObject("InstanceGameObject");
        obj.AddComponent<InstanceGroup>();
        obj.AddComponent<InstancePainter>();
        obj.AddComponent<InstanceGameObject>();
        obj.GetComponent<InstanceGameObject>().useReferences = true;
        Selection.activeGameObject = obj;
    }
}
