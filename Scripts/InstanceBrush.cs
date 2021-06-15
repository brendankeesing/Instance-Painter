// Brushes are not accessible in the final build.
#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

// All brushes must inherit from this.
[System.Serializable]
public abstract class InstanceBrush
{
    public InstancePainter painter;
    public string brushName;
    public Color cursorColor;
    public float radius = 5.0f;

    public InstanceBrush(string id, Color color)
    {
        brushName = id;
        cursorColor = color;
    }

    public abstract void UpdateInspector();
    public virtual void UpdatePressed(RaycastHit hit) { }
    public virtual void UpdateDragged(RaycastHit hit) { }

    // Returns the position of the cursor in 3D space.
    public Vector3? Update(InstancePainter parent)
    {
        painter = parent;

        // if nothing is at the cursor
        RaycastHit? hit = painter.GetObjectAtCursor(InstancePainterRaycastType.IGNORE_SELF);
        if (!hit.HasValue)
            return null;

        // if the left mouse button is used
        if (!Event.current.alt && !Event.current.control && Event.current.button == 0)
        {
            if (Event.current.type == EventType.MouseDown)
            {
                UpdatePressed(hit.Value);
                UpdateDragged(hit.Value);
            }
            else if (Event.current.type == EventType.MouseDrag)
            {
                UpdateDragged(hit.Value);
            }
        }

        return hit.Value.point;
    }

    // Gets all of the objects at the cursor within the cursor's radius.
    public List<InstanceObject> GetSelectedObjects(Vector3 cursorposition)
    {
        List<InstanceObject> objects = new List<InstanceObject>();
        for (int i = 0; i < painter.group.count; ++i)
        {
            InstanceObject instance = painter.group[i];
            if ((instance.position - cursorposition).sqrMagnitude <= radius * radius)
                objects.Add(instance);
        }
        return objects;
    }
}

// Used to add more properties to the brush (such as a brush strength and distribution)
[System.Serializable]
public abstract class InstanceBrushDistribution : InstanceBrush
{
    public float brushStrength = 0.5f;
    public AnimationCurve brushDistribution = new AnimationCurve(new Keyframe(0.0f, 1.0f), new Keyframe(1.0f, 1.0f));

    public InstanceBrushDistribution(string id, Color color) : base(id, color) { }

    public void BrushDistributionInspector()
    {
        radius = Mathf.Max(EditorGUILayout.FloatField("Radius", radius), 0.0001f);
        brushStrength = EditorGUILayout.Slider("Strength", brushStrength, 0.0f, 1.0f);
        //brushDistribution = EditorGUILayout.CurveField("Distribution", brushDistribution, cursorColor, new Rect(0, 0, 1, 1));

        Rect rect = EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Distribution");
        int imagesize = (int)EditorGUIUtility.singleLineHeight;

        rect.x += EditorGUIUtility.labelWidth;
        rect.width -= EditorGUIUtility.labelWidth + imagesize;
        brushDistribution = EditorGUI.CurveField(rect, brushDistribution, cursorColor, new Rect(0, 0, 1, 1));

        // add a little image next to the curve field that will show a visual representation how the brush's distribution
        Texture2D image = new Texture2D(imagesize, imagesize);
        image.hideFlags = HideFlags.HideAndDontSave; // this magically stops a memory leak error appearing everytime you save the project
        Color[] data = image.GetPixels();
        float imageradius = imagesize / 2;
        Vector2 imagecenter = new Vector2(imageradius, imageradius);
        for (int y = 0; y < imagesize; ++y)
        {
            for (int x = 0; x < imagesize; ++x)
            {
                float lengthsqr = ((imagecenter - new Vector2(x, y)) / imageradius).sqrMagnitude;
                if (lengthsqr >= 1.0f)
                    data[y * imagesize + x] = Color.white;
                else
                    data[y * imagesize + x] = Color.Lerp(Color.white, Color.black, brushDistribution.Evaluate(1.0f - Mathf.Sqrt(lengthsqr)));
            }
        }
        image.SetPixels(data);
        rect.x += rect.width;
        rect.width = imagesize;
        EditorGUI.LabelField(rect, new GUIContent(image));
        EditorGUILayout.EndHorizontal();
    }

    // Modify's all instances within the radius of the cursor.
    public delegate void DelModifyInstance(InstanceObject instance, float strength);
    public void ModifySelectedObjects(Vector3 cursorposition, DelModifyInstance del)
    {
        for (int i = 0; i < painter.group.count; ++i)
        {
            InstanceObject instance = painter.group[i];
            if ((instance.position - cursorposition).sqrMagnitude <= radius * radius)
                del(instance, GetStrength(cursorposition, instance.position));
        }
    }

    // Returns how affected an instance is to the brush (between 0 and 1).
    public float GetStrength(Vector3 cursorposition, Vector3 instanceposition)
    {
        float normalizeddistance = Vector3.Distance(cursorposition, instanceposition) / radius;
        float distribution = brushDistribution.Evaluate(1 - normalizeddistance);
        return Mathf.Clamp(distribution * brushStrength, 0, 1);
    }
}

[System.Serializable]
public class InstanceBrushInsertObject
{
    public float distributionWeight = 1.0f;
    public float[] minObjectDistances = { };
    public bool placeOnNormal = false;
    public float offset = 0.0f;
    public bool randomSwivel = true;
    public float slant = 0.0f;
    public float minScale = 1.0f;
    public float maxScale = 1.0f;
    public Color minColor = Color.white;
    public Color maxColor = Color.white;
    public bool slopeLimit = false;
    public float maxSlope = 0.0f;
    public float maxSlopeDegrees
    {
        get { return (maxSlope + 1.0f) * 90.0f; }
        set { maxSlope = value / 90.0f - 1.0f; }
    }
    public Vector3 flatNormal = Vector3.up;

    bool _showFoldout = false;
    bool _showObjectDistancesFoldout = false;

    internal void PlaceObject(InstanceGroup group, int objectid, RaycastHit hit)
    {
        // check steepness of surface
        if (slopeLimit && Vector3.Dot(hit.normal, flatNormal) < maxSlope)
            return;

        // make sure the object isn't too close to another object
        for (int i = 0; i < group.count; ++i)
        {
            // if the objectID is an invalid ID
            if (group[i].objectID >= minObjectDistances.Length)
                continue;

            float mindist = minObjectDistances[group[i].objectID];
            if ((group[i].position - hit.point).sqrMagnitude <= mindist * mindist)
                return;
        }

        // create instance
        InstanceObject instance = new InstanceObject(group);
        instance.objectID = objectid;
        instance.position = hit.point + hit.normal * offset;
        instance.scale = Vector3.one * Random.Range(minScale, maxScale);
        instance.color = Color.Lerp(minColor, maxColor, Random.Range(0.0f, 1.0f));

        // rotation
        instance.rotation = Quaternion.identity;
        if (placeOnNormal)
            instance.rotation *= Quaternion.FromToRotation(Vector3.up, hit.normal);
        if (randomSwivel)
            instance.rotation *= Quaternion.AngleAxis(Random.Range(0, 360), Vector3.up);
        Vector2 rotdir = Random.insideUnitCircle;
        instance.rotation *= Quaternion.AngleAxis(Random.Range(0.0f, slant), new Vector3(rotdir.x, 0.0f, rotdir.y));

        group.AddInstance(instance);
    }

    // Draws the object in the inspector without a foldout or the object weight.
    internal void UpdateInspector(InstanceGroup group, bool displayminimumdistances)
    {
        if (displayminimumdistances)
        {
            _showObjectDistancesFoldout = EditorGUILayout.Foldout(_showObjectDistancesFoldout, "Minimum Distances");
            if (_showObjectDistancesFoldout)
            {
                if (minObjectDistances.Length != group.objectNames.Count)
                    System.Array.Resize(ref minObjectDistances, group.objectNames.Count);

                ++EditorGUI.indentLevel;
                for (int i = 0; i < minObjectDistances.Length; ++i)
                    minObjectDistances[i] = Mathf.Max(EditorGUILayout.FloatField(group.objectNames[i], minObjectDistances[i]), 0.0f);
                --EditorGUI.indentLevel;
            }
        }

        placeOnNormal = EditorGUILayout.Toggle("Place On Normal", placeOnNormal);
        offset = EditorGUILayout.FloatField("Offset", offset);
        randomSwivel = EditorGUILayout.Toggle("Random Swivel", randomSwivel);
        slant = EditorGUILayout.Slider("Max Slant", slant, 0.0f, 180.0f);

        HorizontalRect hr = new HorizontalRect("Scale ", 2);
        minScale = EditorGUI.FloatField(hr.Next(), minScale);
        maxScale = EditorGUI.FloatField(hr.Next(), maxScale);

        hr = new HorizontalRect("Color ", 2);
        minColor = EditorGUI.ColorField(hr.Next(), minColor);
        maxColor = EditorGUI.ColorField(hr.Next(), maxColor);

        slopeLimit = EditorGUILayout.Toggle("Slope Limit", slopeLimit);
        if (slopeLimit)
        {
            ++EditorGUI.indentLevel;
            maxSlopeDegrees = EditorGUILayout.Slider("Max Slope", maxSlopeDegrees, 0.0f, 180.0f);
            flatNormal = EditorGUILayout.Vector3Field("Flat Normal", flatNormal).normalized;
            --EditorGUI.indentLevel;
        }

        if (maxScale < minScale)
            maxScale = minScale;
    }

    // Draws the object in the inspector with a foldout and a slider for the object weight.
    internal void UpdateInspectorInFoldout(InstanceGroup group, int objectid, bool displayminimumdistances)
    {
        GUIStyle headerstyle = new GUIStyle(EditorStyles.foldout);
        headerstyle.fontStyle = FontStyle.Bold;

        Rect rect = EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("");
        Rect foldoutrect = rect;
        foldoutrect.width = EditorGUIUtility.labelWidth;
        _showFoldout = EditorGUI.Foldout(foldoutrect, _showFoldout, group.objectNames[objectid], headerstyle);
        rect.x += EditorGUIUtility.labelWidth;
        rect.width -= EditorGUIUtility.labelWidth;
        distributionWeight = EditorGUI.Slider(rect, distributionWeight, 0.0f, 1.0f);
        EditorGUILayout.EndHorizontal();

        if (_showFoldout)
        {
            ++EditorGUI.indentLevel;
            UpdateInspector(group, displayminimumdistances);
            --EditorGUI.indentLevel;
        }
    }
}

[System.Serializable]
public class InstanceBrushInsert : InstanceBrush
{
    public InstanceBrushInsert()
        : base("Insert", Color.green)
    {
        radius = 1.0f;
    }

    public InstanceBrushInsertObject objectToPaint = new InstanceBrushInsertObject();
    public int objectID = 0;

    public override void UpdatePressed(RaycastHit hit)
    {
        if (painter.group.objectNames.Count == 0)
            return;

        objectToPaint.PlaceObject(painter.group, objectID, hit);
    }

    public override void UpdateInspector()
    {
        if (GUILayout.Button("Clear All Instances") && EditorUtility.DisplayDialog("Clear All Instances", "Are you sure you want to remove all instances?", "Yes", "No"))
        {
            Undo.RecordObject(painter.group, "Edit InstanceGroup");
            painter.group.ClearInstances();
            EditorUtility.SetDirty(painter.group);
        }

        objectID = EditorGUILayout.Popup("Object", objectID, painter.group.objectNames.ToArray());
        objectToPaint.UpdateInspector(painter.group, true);
    }
}

[System.Serializable]
public class InstanceBrushMultiInsert : InstanceBrush
{
    public InstanceBrushMultiInsert() : base("MultiInsert", Color.green) { }

    public int insertAmount = 1;
    public float distanceBetweenInserts = 5.0f;
    Vector3? _lastPosition;
    bool _minimumDistancesFoldout = false;

    public InstanceBrushInsertObject[] objectsToPaint = { };

    // Gives the index of an object based on the weights in objectsToPaint.
    // This will return -1 if there are no objects.
    public int randomObjectFromWeight
    {
        get
        {
            float v = Random.Range(0.0f, 1.0f);
            for (int i = 0; i < objectsToPaint.Length; ++i)
            {
                v -= objectsToPaint[i].distributionWeight;
                if (v <= 0.0f)
                    return i;
            }
            return objectsToPaint.Length - 1;
        }
    }

    public override void UpdateDragged(RaycastHit hit)
    {
        if (painter.group.objectNames.Count == 0)
            return;

        // can we paint again?
        if (_lastPosition.HasValue && (hit.point - _lastPosition.Value).magnitude < distanceBetweenInserts)
            return;
        _lastPosition = hit.point;

        List<RaycastHit> hits = painter.GetRandomPointsNearCursor(hit, insertAmount, radius);
        for (int i = 0; i < hits.Count; ++i)
        {
            int objectid = randomObjectFromWeight;
            objectsToPaint[objectid].PlaceObject(painter.group, objectid, hits[i]);
        }
    }

    // Makes the total of all the objects weights equal 1.
    void NormalizeDistributionWeights(int index)
    {
        // if only one object exists, the weight must be 1 for that object
        if (objectsToPaint.Length == 1)
        {
            objectsToPaint[0].distributionWeight = 1.0f;
            return;
        }

        // calculate the new total
        float total = 0.0f;
        for (int i = 0; i < objectsToPaint.Length; ++i)
            total += objectsToPaint[i].distributionWeight;

        // if nothing has changed
        if (total == 1.0f)
            return;

        // avoid divide by zero and other weird numbers
        if (total <= 0.000001f)
        {
            for (int i = 0; i < objectsToPaint.Length; ++i)
                objectsToPaint[i].distributionWeight = 1.0f / objectsToPaint.Length;
            return;
        }

        // calculate amount to distribute fairly to other objects
        float distribution = (1.0f - total) / (objectsToPaint.Length - 1);

        for (int i = 0; i < objectsToPaint.Length; ++i)
        {
            if (i != index)
                objectsToPaint[i].distributionWeight += distribution;
        }
    }

    public override void UpdateInspector()
    {
        if (GUILayout.Button("Clear All Instances") && EditorUtility.DisplayDialog("Clear All Instances", "Are you sure you want to remove all instances?", "Yes", "No"))
        {
            Undo.RecordObject(painter.group, "Edit InstanceGroup");
            painter.group.ClearInstances();
            EditorUtility.SetDirty(painter.group);
        }

        radius = Mathf.Max(EditorGUILayout.FloatField("Radius", radius), 0.0001f);
        insertAmount = Mathf.Max(EditorGUILayout.IntField("Insert Amount", insertAmount), 1);
        distanceBetweenInserts = Mathf.Max(EditorGUILayout.FloatField("Distance Between Inserts", distanceBetweenInserts), 0.0f);

        // resize the distribution weight list
        if (objectsToPaint.Length != painter.group.objectNames.Count)
        {
            System.Array.Resize(ref objectsToPaint, painter.group.objectNames.Count);
            for (int i = 0; i < objectsToPaint.Length; ++i)
            {
                if (objectsToPaint[i] == null)
                    objectsToPaint[i] = new InstanceBrushInsertObject();
                objectsToPaint[i].distributionWeight = 1.0f / objectsToPaint.Length;
            }
        }

        EditorGUILayout.Separator();
        for (int i = 0; i < objectsToPaint.Length; ++i)
        {
            // the weights must be renormalized for each individual object if a weight was changed
            objectsToPaint[i].UpdateInspectorInFoldout(painter.group, i, false);
            NormalizeDistributionWeights(i);
        }
        //--EditorGUI.indentLevel;

        _minimumDistancesFoldout = EditorGUILayout.Foldout(_minimumDistancesFoldout, "Minimum Distances");
        if (_minimumDistancesFoldout)
        {
            string[] labels = painter.group.objectNames.ToArray();
            System.Array.Reverse(labels);
            HorizontalRect.HeaderLabels(labels);

            int objcount = painter.group.objectNames.Count;

            // make sure all objects have their arrays at the right size
            for (int i = 0; i < objcount; ++i)
            {
                if (objectsToPaint[i].minObjectDistances.Length != objcount)
                    System.Array.Resize(ref objectsToPaint[i].minObjectDistances, objcount);
            }

            // for each row
            for (int i = 0; i < objcount; ++i)
            {
                HorizontalRect hr = new HorizontalRect(painter.group.objectNames[i], objcount);

                // for each cell
                for (int v = objcount - 1; v >= i; --v)
                {
                    float value = objectsToPaint[i].minObjectDistances[v];
                    value = Mathf.Max(EditorGUI.FloatField(hr.Next(), value), 0.0f);
                    objectsToPaint[i].minObjectDistances[v] = value;
                    objectsToPaint[v].minObjectDistances[i] = value;
                }
            }
        }
    }
}

[System.Serializable]
public class InstanceBrushErase : InstanceBrush
{
    public InstanceBrushErase() : base("Erase", Color.red) { }

    public override void UpdateDragged(RaycastHit hit)
    {
        List<InstanceObject> objs = GetSelectedObjects(hit.point);
        for (int i = 0; i < objs.Count; ++i)
            painter.group.RemoveInstance(objs[i]);
    }

    public override void UpdateInspector()
    {
        if (GUILayout.Button("Clear All Instances") && EditorUtility.DisplayDialog("Clear All Instances", "Are you sure you want to remove all instances?", "Yes", "No"))
            painter.group.ClearInstances();
        radius = Mathf.Max(EditorGUILayout.FloatField("Radius", radius), 0.0001f);
    }
}

[System.Serializable]
public class InstanceBrushColorize : InstanceBrushDistribution
{
    public InstanceBrushColorize() : base("Colorize", Color.yellow) { }

    public enum Action
    {
        Set,
        SetRange,
        SetNoiseRange,
        Add
    }

    public Action action = Action.Set;
    public Color minColor = Color.white;
    public Color maxColor = Color.white;

    public override void UpdateDragged(RaycastHit hit)
    {
        if (action == Action.Set)
            ModifySelectedObjects(hit.point, (i, s) => i.color = minColor);
        else if (action == Action.SetRange)
            ModifySelectedObjects(hit.point, (i, s) => i.color = Color.Lerp(minColor, maxColor, Random.Range(0.0f, 1.0f)));
        else if (action == Action.SetNoiseRange)
            ModifySelectedObjects(hit.point, (i, s) => i.color = new Color(Random.Range(minColor.r, maxColor.r),
                                                                           Random.Range(minColor.g, maxColor.g),
                                                                           Random.Range(minColor.b, maxColor.b),
                                                                           Random.Range(minColor.a, maxColor.a)));
        else if (action == Action.Add)
            ModifySelectedObjects(hit.point, (i, s) => i.color = Color.Lerp(i.color, minColor, s));
    }

    public override void UpdateInspector()
    {
        action = (Action)EditorGUILayout.EnumPopup("Action", action);

        if (action == Action.Set)
        {
            radius = Mathf.Max(EditorGUILayout.FloatField("Radius", radius), 0.0001f);
            minColor = EditorGUILayout.ColorField("Color", minColor);
        }
        else if (action == Action.Add)
        {
            BrushDistributionInspector();
            minColor = EditorGUILayout.ColorField("Color", minColor);
        }
        else
        {
            radius = Mathf.Max(EditorGUILayout.FloatField("Radius", radius), 0.0001f);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Color");
            minColor = EditorGUILayout.ColorField(minColor);
            maxColor = EditorGUILayout.ColorField(maxColor);
            EditorGUILayout.EndHorizontal();
        }
    }
}

[System.Serializable]
public class InstanceBrushScale : InstanceBrushDistribution
{
    public InstanceBrushScale() : base("Scale", Color.blue) { }

    public enum Action
    {
        Set,
        SetRange,
        Shrink,
        Grow
    }

    public Action action = Action.Set;
    public float minScale = 1.0f;
    public float maxScale = 1.0f;
    public float multiplier = 1.0f;

    public override void UpdateDragged(RaycastHit hit)
    {
        if (action == Action.Set)
            ModifySelectedObjects(hit.point, (i, s) => i.scale = Vector3.one * minScale);
        else if (action == Action.SetRange)
            ModifySelectedObjects(hit.point, (i, s) => i.scale = Vector3.one * Random.Range(minScale, maxScale));
        else if (action == Action.Shrink)
            ModifySelectedObjects(hit.point, (i, s) => i.scale *= 1.0f - (s / 2));
        else if (action == Action.Grow)
            ModifySelectedObjects(hit.point, (i, s) => i.scale *= 1.0f + (s / 2));
    }

    public override void UpdateInspector()
    {
        BrushDistributionInspector();
        action = (Action)EditorGUILayout.EnumPopup("Action", action);
        if (action == Action.Set)
            minScale = EditorGUILayout.FloatField("Scale", minScale);
        else if (action == Action.SetRange)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Scale");
            minScale = EditorGUILayout.FloatField(minScale);
            maxScale = EditorGUILayout.FloatField(maxScale);
            EditorGUILayout.EndHorizontal();
        }
    }
}

[System.Serializable]
public class InstanceBrushScatter : InstanceBrushDistribution
{
    public InstanceBrushScatter() : base("Scatter", Color.magenta) { }

    float amount = 1.0f;

    public override void UpdateDragged(RaycastHit hit)
    {
        List<InstanceObject> objs = GetSelectedObjects(hit.point);
        if (objs.Count <= 1)
            return;

        for (int i = 0; i < objs.Count; ++i)
        {
            // find nearest obj
            int nearest = 0;
            float nearestdist = Mathf.Infinity;
            for (int a = 0; a < objs.Count; ++a)
            {
                float dist = (objs[i].position - objs[a].position).magnitude;
                if (a != i && dist < nearestdist)
                {
                    nearest = a;
                    nearestdist = dist;
                }
            }

            // move in the opposite direction
            Vector3 direction = objs[i].position - objs[nearest].position;
            objs[i].position += direction.normalized * (GetStrength(hit.point, objs[i].position) * amount);
        }
    }

    public override void UpdateInspector()
    {
        BrushDistributionInspector();
        amount = EditorGUILayout.FloatField("Amount", amount);
    }
}

#endif