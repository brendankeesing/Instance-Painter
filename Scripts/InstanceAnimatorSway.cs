using UnityEngine;
using System.Collections;

struct InstanceAnimatorSwayInstance
{
    public Quaternion original;
    public Vector3 target;
    public Vector3 current;
}

[System.Serializable]
public class InstanceAnimatorSwayObject
{
    public float amount = 0.3f;
    public float speed = 0.3f;

    internal void Move(InstanceObject instance, ref InstanceAnimatorSwayInstance swayinstance)
    {
        // skip this if the object isn't moving
        if (amount <= 0.00001f || speed <= 0.00001f)
            return;

        // move towards the target
        Vector3 desiredvelocity = swayinstance.target - swayinstance.current;
        float distance = desiredvelocity.magnitude;

        // if the target is reached
        if (distance < speed * 0.01f)
        {
            swayinstance.target = Random.onUnitSphere * amount;
            return;
        }

        // normalize
        desiredvelocity /= distance;

        // add to the current position
        swayinstance.current += desiredvelocity * (Time.deltaTime * speed);

        // calculate the new rotation
        instance.rotation = swayinstance.original * Quaternion.Euler(swayinstance.current);
    }
}

[RequireComponent(typeof(InstanceGroup))]
public class InstanceAnimatorSway : MonoBehaviour
{
    public InstanceAnimatorSwayObject[] swayObjects = { };
    public float maxDistanceSqr = Mathf.Infinity;
    public float maxDistance
    {
        get { return Mathf.Sqrt(maxDistanceSqr); }
        set { maxDistanceSqr = value * value; }
    }

    InstanceGroup _group;
    public InstanceGroup group { get { if (_group == null) _group = GetComponent<InstanceGroup>(); return _group; } }
    InstanceAnimatorSwayInstance[] _instances;

    void Start()
    {
        _group = GetComponent<InstanceGroup>();
    }

    void ResetTargets()
    {
        _instances = new InstanceAnimatorSwayInstance[group.count];
        for (int i = 0; i < _group.count; ++i)
        {
            _instances[i].original = _group[i].rotation;
            _instances[i].target = Vector3.zero;
            _instances[i].current = Vector3.zero;
        }
    }

    public void ResetObjects()
    {
        System.Array.Resize(ref swayObjects, group.objectNames.Count);
        for (int i = 0; i < swayObjects.Length; ++i)
        {
            if (swayObjects[i] == null)
                swayObjects[i] = new InstanceAnimatorSwayObject();
        }
    }

    void Update()
    {
        if (!group)
            return;

        if (_instances == null || _instances.Length != _group.count)
            ResetTargets();

        if (swayObjects.Length != _group.objectNames.Count)
            ResetObjects();

        Vector3 cameraposition = Camera.main.transform.position;

        for (int i = 0; i < _group.count; ++i)
        {
            InstanceObject instance = _group[i];

            // make sure there is a sway object
            if (instance.objectID >= swayObjects.Length)
                continue;

            // make sure the object is not too far away
            if ((cameraposition - instance.position).sqrMagnitude > maxDistanceSqr)
                continue;

            swayObjects[instance.objectID].Move(instance, ref _instances[i]);
        }
    }
}