using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[ExecuteInEditMode]
public class InstanceReference : MonoBehaviour
{
    [SerializeField]
    internal InstanceObject _instance;
    public InstanceObject instance { get { return _instance; } }

    Transform _realTransform;
    Transform _transform { get { if (!_realTransform) _realTransform = transform; return _realTransform; } }

    // Makes this reference independant from the instance
    public void UnlinkFromInstance()
    {
        if (_instance == null)
            return;
        instance.reference = null;
        _instance = null;
    }

    // Permanently removes this instance from the InstanceGroup.
    // This InstanceReference should be deleted after calling this, as it no longer serves a purpose.
    public void UnlinkFromInstanceGroup()
    {
        if (_instance == null)
            return;

        instance.reference = null;
        _instance.group.RemoveInstance(_instance, false);
        _instance = null;
    }

    void OnDestroy()
    {
        if (_instance != null)
            _instance.group.RemoveInstance(_instance);
    }

    void Update()
    {
        // make sure the instance is in sync with our transform
        if (_instance != null && _transform.hasChanged)
            _instance.FromTransform(_transform);
    }
}

// This can be used to generate and maintain a list of all of the references in the scene.
// This will usually be used by the renderer of the instances.
// Only one should be used on a single GameObject (if there are more, it will mess up the references).
public abstract class InstanceReferenceManager : MonoBehaviour
{
    InstanceGroup _group;
    public InstanceGroup group { get { return _group == null ? (_group = GetComponent<InstanceGroup>()) : _group; } }

    [SerializeField]
    bool _useReferences = false;
    public bool useReferences
    {
        get
        {
            return _useReferences;
        }
        set
        {
            if (value != _useReferences)
            {
                _useReferences = value;
                if (_useReferences)
                    CreateReferences();
                else
                    RemoveReferences();
            }
        }
    }

    // Creates and returns the game object based on the instance.
    // The reference will be added after this.
    protected virtual GameObject CreateReferenceGameObject(InstanceObject instance)
    {
        return new GameObject("InstanceReference");
    }

    // Creates a gameobject with a reference to the instance.
    public InstanceReference CreateReferenceForInstance(InstanceObject instance)
    {
        // create reference object
        GameObject obj = CreateReferenceGameObject(instance);
        if (instance.objectID >= 0 && instance.objectID < group.objectNames.Count)
            obj.name = group.objectNames[instance.objectID];
        InstanceReference reference = obj.AddComponent<InstanceReference>();
        reference._instance = instance;
        instance.reference = reference;

        // sync transform to the instance
        Transform objtransform = obj.transform;
        objtransform.transform.parent = transform;
        instance.UpdateTransform(objtransform);

        return reference;
    }

    // Builds all references
    void CreateReferences()
    {
        for (int i = 0; i < group.count; ++i)
        {
            InstanceObject instance = group[i];

            if (instance.reference != null)
                Debug.LogError("Instance has more than one reference assigned to it.");

            CreateReferenceForInstance(instance);
        }
    }

    void RemoveReferences()
    {
        for (int i = 0; i < group.count; ++i)
        {
            InstanceReference reference = group[i].reference;
            if (reference == null)
                continue;
            reference.UnlinkFromInstance();
            DestroyImmediate(reference.gameObject);
        }
    }

    // Makes reference game objects independant from the instances
    public void UnlinkReferences()
    {
        for (int i = 0; i < group.count; ++i)
        {
            InstanceReference reference = group[i].reference;
            if (reference == null)
                continue;
            reference.UnlinkFromInstance();
            DestroyImmediate(reference);
        }
        _useReferences = false;
    }

    void OnDestroy()
    {
        RemoveReferences();
    }
}