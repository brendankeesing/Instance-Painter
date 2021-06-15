using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections;

[CustomEditor(typeof(InstanceMesh))]
public class InstanceMeshEditor : Editor
{
    public InstanceMesh mesh { get { return (InstanceMesh)target; } }
    public InstanceGroup group { get { return mesh.group; } }
    int _selectedObject = 0;
    InstanceMeshLOD _selectedLOD = null;
    bool _showLODFoldout = true;

    public override void OnInspectorGUI()
    {
        Undo.RecordObject(target, "Edit InstanceMesh");

        mesh.useReferences = EditorGUILayout.Toggle("Use References", mesh.useReferences);
        mesh.performCulling = EditorGUILayout.Toggle("Cull", mesh.performCulling);
        mesh.fadeDistance = Mathf.Max(EditorGUILayout.FloatField("Fade Distance", mesh.fadeDistance), 0.0f);

        // make sure the mesh has the right amount of objects in it
        mesh.UpdateObjects();

        // can't do anything without any objects
        if (group.objectNames.Count == 0)
        {
            EditorGUILayout.HelpBox("Instance Group does not have an objects in it.", MessageType.Warning);
            return;
        }

        // get strings for object popup list
        string[] objectstrings = new string[group.objectNames.Count];
        for (int i = 0; i < objectstrings.Length; ++i)
            objectstrings[i] = i.ToString() + ": " + group.objectNames[i] + " [" + mesh.objects[i].lods.Count.ToString() + "]";
        _selectedObject = Mathf.Clamp(EditorGUILayout.Popup("Selected Object", _selectedObject, objectstrings), 0, objectstrings.Length);

        InstanceMeshObject currentobj = mesh.objects[_selectedObject];

        ++EditorGUI.indentLevel;
        currentobj.hide = EditorGUILayout.Toggle("Hide", currentobj.hide);

        // get strings for LOD popup list
        string[] lodstrings = new string[currentobj.lods.Count];
        for (int i = 0; i < lodstrings.Length; ++i)
            lodstrings[i] = "LOD" + i.ToString() + ": " + (currentobj.lods[i].mesh == null ? "--" : currentobj.lods[i].mesh.name) + " [" + currentobj.lods[i].distance + "]";

        // if no LODs exist yet, just show an add button
        if (currentobj.lods.Count == 0)
        {
            Rect rect = EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Selected LOD");
            rect.x += EditorGUIUtility.labelWidth - 16 * EditorGUI.indentLevel;
            rect.width -= EditorGUIUtility.labelWidth - 16 * EditorGUI.indentLevel;
            if (GUI.Button(rect, "Add LOD"))
                currentobj.lods.Add(new InstanceMeshLOD());
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            int selectedLODindex = currentobj.lods.IndexOf(_selectedLOD);
            if (selectedLODindex < 0)
                selectedLODindex = 0;

            Rect rect = EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Selected LOD");

            float buttonwidth = EditorGUIUtility.singleLineHeight * 2;
            rect.x += EditorGUIUtility.labelWidth - 16 * EditorGUI.indentLevel;
            rect.width -= EditorGUIUtility.labelWidth + buttonwidth * 2 - 16 * EditorGUI.indentLevel;

            selectedLODindex = Mathf.Clamp(EditorGUI.Popup(rect, selectedLODindex, lodstrings), 0, lodstrings.Length);
            _selectedLOD = currentobj.lods[selectedLODindex];

            rect.x += rect.width;
            rect.width = buttonwidth;
            if (GUI.Button(rect, "+"))
            {
                _selectedLOD = new InstanceMeshLOD();
                currentobj.lods.Add(_selectedLOD);
            }
            rect.x += buttonwidth;
            if (GUI.Button(rect, "-"))
            {
                currentobj.lods.Remove(_selectedLOD);
                return;
            }

            EditorGUILayout.EndHorizontal();

            _showLODFoldout = EditorGUILayout.Foldout(_showLODFoldout, "LOD" + selectedLODindex);
            if (_showLODFoldout)
            {
                ++EditorGUI.indentLevel;

                _selectedLOD.distance = Mathf.Max(EditorGUILayout.FloatField("Distance", _selectedLOD.distance), 0.0f);

                _selectedLOD.mesh = (Mesh)EditorGUILayout.ObjectField("Mesh", _selectedLOD.mesh, typeof(Mesh), true);
                if (_selectedLOD.mesh)
                {
                    _selectedLOD.ResizeMaterials();
                    for (int i = 0; i < _selectedLOD.materials.Length; ++i)
                    {
                        if (mesh.fadeDistance < 0.001f)
                            _selectedLOD.materials[i] = (Material)EditorGUILayout.ObjectField("Material " + i.ToString(), _selectedLOD.materials[i], typeof(Material), true);
                        else
                        {
                            HorizontalRect hr = new HorizontalRect("Material " + i.ToString(), 2);
                            _selectedLOD.materials[i] = (Material)EditorGUI.ObjectField(hr.Next(), _selectedLOD.materials[i], typeof(Material), true);
                            _selectedLOD.fadeMaterials[i] = (Material)EditorGUI.ObjectField(hr.Next("Fade"), _selectedLOD.fadeMaterials[i], typeof(Material), true);
                        }
                    }
                }

                _selectedLOD.castShadows = EditorGUILayout.Toggle("Cast Shadows", _selectedLOD.castShadows);
                _selectedLOD.receiveShadows = EditorGUILayout.Toggle("Receive Shadows", _selectedLOD.receiveShadows);
                _selectedLOD.billboard = EditorGUILayout.Toggle("Billboard", _selectedLOD.billboard);

                _selectedLOD.modifyBaseMesh = EditorGUILayout.Toggle("Modify Base Mesh", _selectedLOD.modifyBaseMesh);
                if (_selectedLOD.modifyBaseMesh)
                {
                    ++EditorGUI.indentLevel;
                    _selectedLOD.basePosition = EditorGUILayout.Vector3Field("Position", _selectedLOD.basePosition);
                    _selectedLOD.baseRotation = Quaternion.Euler(EditorGUILayout.Vector3Field("Rotation", _selectedLOD.baseRotation.eulerAngles));
                    _selectedLOD.baseScale = EditorGUILayout.Vector3Field("Scale", _selectedLOD.baseScale);
                    --EditorGUI.indentLevel;
                }

                --EditorGUI.indentLevel;
            }
        }

        --EditorGUI.indentLevel;

        if (GUI.changed)
        {
            currentobj.UpdateLODs();
            EditorUtility.SetDirty(mesh);
        }
    }

    [MenuItem("GameObject/Other/InstanceMesh")]
    static void CreateObject()
    {
        GameObject obj = new GameObject("InstanceMesh");
        obj.AddComponent<InstanceGroup>();
        obj.AddComponent<InstancePainter>();
        obj.AddComponent<InstanceMesh>();
        Selection.activeGameObject = obj;
    }
}
