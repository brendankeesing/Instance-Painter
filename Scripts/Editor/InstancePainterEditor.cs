using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(InstancePainter))]
public class InstancePainterEditor : Editor
{
    public InstancePainter painter { get { return (InstancePainter)target; } }
    Texture[] _buttonIcons;
    GUIStyle _headerLabelStyle;
    GUIStyle _buttonDownStyle;

    void OnEnable()
    {
        /* For some reason, this returns the Unity Personal skin, but not the pro, so we will use GUI.skin in OnInspectorGUI().
        _headerLabelStyle = new GUIStyle(EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).label);
        _headerLabelStyle.fontStyle = FontStyle.Bold;
        _headerLabelStyle.alignment = TextAnchor.MiddleCenter;
        */

        _buttonDownStyle = new GUIStyle(EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).button);
        _buttonDownStyle.normal = _buttonDownStyle.active;

        // Button icons must be in a Resource folder and start with "InstancePainter".
        // The name of the brush can be found in InstanceBrush
        _buttonIcons = new Texture[painter.brushes.Length];
        for (int i = 0; i < _buttonIcons.Length; ++i)
            _buttonIcons[i] = Resources.Load("InstancePainter" + painter.brushes[i].brushName, typeof(Texture)) as Texture;
    }

    public override void OnInspectorGUI()
    {
        // See comment above (in OnEnable)
        if (_headerLabelStyle == null)
        {
            _headerLabelStyle = new GUIStyle(GUI.skin.label);
            _headerLabelStyle.fontStyle = FontStyle.Bold;
            _headerLabelStyle.alignment = TextAnchor.MiddleCenter;
        }

        // make sure instance objects exist
        if (painter.group.objectNames.Count == 0)
        {
            EditorGUILayout.HelpBox("Instance Group does not have any objects.", MessageType.Warning);
            return;
        }

        // top buttons
        Rect rect = EditorGUILayout.BeginHorizontal(GUILayout.Height(EditorGUIUtility.singleLineHeight));
        EditorGUILayout.LabelField("");
        rect.height *= 2.3f;

        // calculate button sizes
        float buttonwidth = rect.height * 1.2f;
        rect.x = (rect.width - buttonwidth * painter.brushes.Length) / 2;
        rect.width = buttonwidth;

        // draw buttons
        for (int i = 0; i < painter.brushes.Length; ++i)
        {
            // if there was no icon image loaded, display the first 3 characters of the name
            GUIContent content = new GUIContent
            (
                _buttonIcons[i] ? "" : painter.brushes[i].brushName.Substring(0, 3),
                _buttonIcons[i] ? _buttonIcons[i] : null,
                painter.brushes[i].brushName
            );

            if (i == painter.selectedBrushIndex)
            {
                if (GUI.Button(rect, content, _buttonDownStyle))
                    painter.selectedBrushIndex = -1;
            }
            else
            {
                if (GUI.Button(rect, content))
                    painter.selectedBrushIndex = i;
            }
            rect.x += rect.width;
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.LabelField("");
        EditorGUILayout.Separator();

        // helper text
        string headertext = painter.selectedBrush == null ? "Select a Brush" : painter.selectedBrush.brushName;
        EditorGUILayout.LabelField(headertext, _headerLabelStyle);
        EditorGUILayout.Separator();

        // let the brush draw the rest of the inspector
        if (painter.selectedBrush != null)
        {
            Undo.RecordObject(target, "Edit InstanceBrush" + painter.selectedBrush.brushName);
            painter.selectedBrush.painter = painter;
            painter.selectedBrush.UpdateInspector();
        }

        if (GUI.changed)
            EditorUtility.SetDirty(target);
    }

    void OnSceneGUI()
    {
        if (painter.selectedBrush != null)
        {
            painter.Update();

            // Stop the selection of other objects and allow dragging without the drag rect appearing
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            EditorUtility.SetDirty(target);
        }
    }
}
