using UnityEngine;

#if UNITY_EDITOR

using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public enum InstancePainterRaycastType
{
    ALL,
    IGNORE_SELF,
    ONLY_SELF
}

[ExecuteInEditMode]
[RequireComponent(typeof(InstanceGroup))]
public class InstancePainter : MonoBehaviour
{
    InstanceGroup _group;
    public InstanceGroup group { get { return _group == null ? (_group = GetComponent<InstanceGroup>()) : _group; } }

    Vector3? _cursorPosition;

    public InstanceBrush[] brushes = new InstanceBrush[]
    {
        new InstanceBrushInsert(),
        new InstanceBrushMultiInsert(),
        new InstanceBrushErase(),
        new InstanceBrushColorize(),
        new InstanceBrushScale(),
        new InstanceBrushScatter()
    };
    [System.NonSerialized]
    public int selectedBrushIndex = -1;
    public InstanceBrush selectedBrush { get { return selectedBrushIndex >= 0 ? brushes[selectedBrushIndex] : null; } }

    public RaycastHit? GetObjectAtCursor(InstancePainterRaycastType raycasttype)
    {
        Vector3 raystart = Event.current.mousePosition;
        raystart.y = Camera.current.pixelHeight - raystart.y;
        return GetHitObject(Camera.current.ScreenPointToRay(raystart), raycasttype);
    }

    public RaycastHit? GetHitObject(Ray ray, InstancePainterRaycastType raycasttype, int layermask = ~0)
    {
        RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, layermask).OrderBy(h => h.distance).ToArray();
        if (hits == null)
            return null;

        for (int i = 0; i < hits.Length; ++i)
        {
            if (raycasttype == InstancePainterRaycastType.ALL ||
                raycasttype == InstancePainterRaycastType.IGNORE_SELF && !hits[i].transform.IsChildOf(group.transform) ||
                raycasttype == InstancePainterRaycastType.ONLY_SELF && hits[i].transform.IsChildOf(group.transform))
                return hits[i];
        }
        return null;
    }

    // Returns a list of random points near the cursor in 3D space.
    // This works by recasting rays in the direction of the normal of the hit surface.
    public List<RaycastHit> GetRandomPointsNearCursor(RaycastHit cursorpoint, int amount, float radius, float height = 1.0f, int layermask = ~0)
    {
        List<RaycastHit> points = new List<RaycastHit>();
        Vector3 center = cursorpoint.point + cursorpoint.normal * height;
        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, cursorpoint.normal);

        for (int i = 0; i < amount; ++i)
        {
            Vector2 circlepoint = Random.insideUnitCircle * radius;
            Vector3 raystart = rotation * new Vector3(circlepoint.x, 0.0f, circlepoint.y) + center;
            RaycastHit? hit = GetHitObject(new Ray(raystart, -cursorpoint.normal), InstancePainterRaycastType.IGNORE_SELF, layermask);
            if (hit.HasValue)
                points.Add(hit.Value);
        }

        return points;
    }

    public void DestroyObject(RaycastHit hit)
    {
        // make sure it is the direct child of this
        Transform toptransform = hit.transform;
        while (toptransform.parent != group.transform)
            toptransform = toptransform.parent;
    }

    public void Update()
    {
        // the current event is required to check key presses and mouse positions
        if (Event.current == null || Camera.current == null)
            return;

        _cursorPosition = null;

        // we don't want the cursor displayed if the user is moving the camera
        if (selectedBrush == null || Event.current.shift || Event.current.button != 0)
            return;

        _cursorPosition = selectedBrush.Update(this);
    }

    void OnDrawGizmosSelected()
    {
        // draw cursor
        if (_cursorPosition.HasValue && selectedBrush != null)
        {
            Gizmos.color = new Color(selectedBrush.cursorColor.r, selectedBrush.cursorColor.g, selectedBrush.cursorColor.b, 0.5f);
            Gizmos.DrawSphere(_cursorPosition.Value, selectedBrush.radius);
        }
    }
}

#else

public class InstancePainter : MonoBehaviour
{
    // InstancePainter is empty when compiled for the final build.
}

#endif