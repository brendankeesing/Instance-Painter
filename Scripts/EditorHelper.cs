#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

public class HorizontalRect
{
    Rect _rect;
    float _indent;

    public static void HeaderLabels(params string[] labels)
    {
        float angle = 45.0f;

        GUIStyle labelstyle = new GUIStyle(GUI.skin.label);
        labelstyle.alignment = TextAnchor.MiddleRight;

        // get height of the row
        float maxlabelwidth = 0.0f;
        for (int i = 0; i < labels.Length; ++i)
            maxlabelwidth = Mathf.Max(labelstyle.CalcSize(new GUIContent(labels[i])).x, maxlabelwidth);
        float rowheight = maxlabelwidth * Mathf.Sin(angle) + EditorGUIUtility.singleLineHeight;
        maxlabelwidth *= 2; // not sure why this is needed... but it is

        Rect rect = FirstCellRect(null, labels.Length);
        rect.height = EditorGUIUtility.singleLineHeight;
        float cellwidth = rect.width;
        rect.width = maxlabelwidth;
        rect.x -= maxlabelwidth - cellwidth / 2;
        rect.y += rowheight - EditorGUIUtility.singleLineHeight * 1.5f;

        // draw the labels
        for (int i = 0; i < labels.Length; ++i)
        {
            EditorGUIUtility.RotateAroundPivot(angle, new Vector2(rect.xMax, rect.center.y));
            EditorGUI.LabelField(rect, labels[i], labelstyle);
            GUI.matrix = Matrix4x4.identity;

            // move to the next cell
            rect.x += cellwidth;
        }

        GUILayout.Space(rowheight - EditorGUIUtility.singleLineHeight);
    }

    static Rect FirstCellRect(string label, int values)
    {
        Rect rect = EditorGUILayout.BeginHorizontal();
        if (label != null)
            EditorGUILayout.LabelField(label);
        EditorGUILayout.EndHorizontal();
        rect.x += EditorGUIUtility.labelWidth;
        rect.width -= EditorGUIUtility.labelWidth;
        rect.width /= values;
        return rect;
    }

    public HorizontalRect(string label, int values)
    {
        _rect = FirstCellRect(label, values);

        // for some stupid reason, indenting messes up the positions (increases) and sizes (reduces) of the rects
        _indent = EditorGUI.indentLevel * 16;
    }

    public Rect Next()
    {
        Rect rect = _rect;
        rect.x -= _indent;
        rect.width += _indent;// -5;
        _rect.x += _rect.width;
        return rect;
    }

    public Rect Next(string label)
    {
        Vector2 textsize = GUI.skin.label.CalcSize(new GUIContent(label));
        return Next(label, textsize.x);
    }

    public Rect Next(string label, float labelwidth)
    {
        // label
        Rect rect = _rect;
        rect.x -= _indent;
        rect.width = labelwidth + _indent;
        EditorGUI.LabelField(rect, label);

        // value
        rect = _rect;
        rect.x += labelwidth - _indent;
        rect.width = rect.width - labelwidth + _indent;

        _rect.x += _rect.width;
        return rect;
    }
}

#endif