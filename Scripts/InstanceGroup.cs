using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// This represents a single instance.
// A single instance can refer to only a single reference (weird behaviour will occur if there are more).
// Whenever a transformation value is changed, the matrix will be recalculated (try to avoid this wherever possible!).
[System.Serializable]
public class InstanceObject
{
    [SerializeField]
    internal InstanceGroup _group;
    public InstanceGroup group { get { return _group; } }

    public InstanceReference reference;

    Matrix4x4? _matrix;

    [SerializeField]
    Vector3 _position = Vector3.zero;

    [SerializeField]
    Quaternion _rotation = Quaternion.identity;

    [SerializeField]
    Vector3 _scale = Vector3.one;

    [SerializeField]
    Vector2 _skew = Vector2.zero;

    public Vector3 position { get { return _position; } set { _position = value; _matrix = null; } }
    public Quaternion rotation { get { return _rotation; } set { _rotation = value; _matrix = null; } }
    public Vector3 scale { get { return _scale; } set { _scale = value; _matrix = null; } }
    public Vector2 skew { get { return _skew; } set { _skew = value; _matrix = null; } }
    public Color color = Color.white;
    public int objectID = 0;
    public Matrix4x4 matrix { get { if (!_matrix.HasValue) RecalculateMatrix(); return _matrix.Value; } }

    float _boundsRadiusMultiplier = 1.0f;
    public float boundsRadiusMultiplier { get { if (!_matrix.HasValue) RecalculateMatrix();  return _boundsRadiusMultiplier; } }

    public InstanceObject(InstanceGroup parent)
    {
        _group = parent;
    }

    // Syncs the instance with the reference, only if the matrix has changed.
    // This is done automatically when call RecalculateMatrix or if you retrieve the matrix.
    public void UpdateReferenceIfChanged()
    {
        if (!_matrix.HasValue && reference)
            UpdateTransform(reference.transform);
    }

    public void RecalculateMatrix()
    {
        _boundsRadiusMultiplier = Mathf.Max(Mathf.Max(scale.x + scale.x * skew.x, scale.y), scale.z + scale.z * skew.y);

        Matrix4x4 s = Matrix4x4.Scale(scale);
        s.m01 = skew.x;
        s.m21 = skew.y;
        _matrix = Matrix4x4.TRS(position, rotation, Vector3.one) * s;

        // update the reference transform
        if (reference)
            UpdateTransform(reference.transform);
    }

    // Returns a matrix for this instance that will face the direction of the specified position.
    // The rotation will occur on the axis pointing up in this instance's local space.
    // This is non-destructive (ie will not change any values in the instance).
    public Matrix4x4 BillboardMatrix(ref Vector3 eyeposition)
    {
        Matrix4x4 s = Matrix4x4.Scale(scale);
        s.m01 = skew.x;
        s.m21 = skew.y;

        Vector3 up = rotation * Vector3.up;
        Vector3 direction = Vector3.ProjectOnPlane(eyeposition - position, up);
        Quaternion rot = Quaternion.LookRotation(direction, up);

        return Matrix4x4.TRS(position, rot, Vector3.one) * s;
    }

    // Updates a transform based on this instance.
    public void FromTransform(Transform transform)
    {
        position = transform.position;
        rotation = transform.rotation;
        scale = transform.localScale;
    }

    // Updates this instance based on a transform.
    public void UpdateTransform(Transform transform)
    {
        transform.position = position;
        transform.rotation = rotation;
        transform.localScale = scale;
    }
}

[ExecuteInEditMode]
public class InstanceGroup : MonoBehaviour
{
    [SerializeField]
    List<InstanceObject> _instances = new List<InstanceObject>();

    public List<string> objectNames = new List<string>() { "Default Object" };

    public InstanceObject this[int i]
    {
        get { return _instances[i]; }
        set { _instances[i] = value; }
    }

    public int count { get { return _instances.Count; } }

    public void AddInstance(InstanceObject instance)
    {
        instance._group = this;

        // make reference
        InstanceReferenceManager refmanager = GetComponent<InstanceReferenceManager>();
        if (refmanager != null && refmanager.useReferences)
            instance.reference = refmanager.CreateReferenceForInstance(instance);

        _instances.Add(instance);
    }

    public void RemoveInstance(InstanceObject instance, bool destroyreference = true)
    {
        InstanceReference reference = instance.reference;
        if (reference)
        {
            reference.UnlinkFromInstance();
            if (destroyreference)
                DestroyImmediate(reference.gameObject);
        }
        _instances.Remove(instance);
    }

    public void ClearInstances()
    {
        _instances.Clear();
    }

    public int IndexOfInstance(InstanceObject instance)
    {
        return _instances.IndexOf(instance);
    }
}
