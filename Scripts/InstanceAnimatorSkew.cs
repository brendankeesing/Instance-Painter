using UnityEngine;
using System.Collections;

struct InstanceAnimatorSkewInstance
{
    public Vector2 target;
    public Vector2 velocity;
}

[System.Serializable]
public class InstanceAnimatorSkewObject
{
    public float amount = 0.05f;
    public float speed = 0.05f;

    internal void Move(InstanceObject instance, ref InstanceAnimatorSkewInstance skewinstance)
    {
        // skip this if the object isn't moving
        if (amount <= 0.00001f || speed <= 0.00001f)
            return;

        // move towards the target
        Vector2 desiredvelocity = skewinstance.target - instance.skew;
        float distance = desiredvelocity.magnitude;

        // if the target is reached
        if (distance < speed * 0.01f)
        {
            skewinstance.target = Random.insideUnitCircle;
            return;
        }

        // normalize
        desiredvelocity /= distance;

        skewinstance.velocity += (desiredvelocity - skewinstance.velocity) * (Time.deltaTime * speed);
        instance.skew += skewinstance.velocity * Time.deltaTime;
    }
}

[RequireComponent(typeof(InstanceGroup))]
public class InstanceAnimatorSkew : MonoBehaviour
{
    public InstanceAnimatorSkewObject[] skewObjects = { };
    public float maxDistanceSqr = Mathf.Infinity;
    public float maxDistance
    {
        get { return Mathf.Sqrt(maxDistanceSqr); }
        set { maxDistanceSqr = value * value; }
    }

    InstanceGroup _group;
    public InstanceGroup group { get { if (_group == null) _group = GetComponent<InstanceGroup>(); return _group; } }
    InstanceAnimatorSkewInstance[] _instances;

    void Start()
    {
        _group = GetComponent<InstanceGroup>();
    }

    void ResetTargets()
    {
        _instances = new InstanceAnimatorSkewInstance[group.count];
        for (int i = 0; i < _group.count; ++i)
        {
            _instances[i].target = _group[i].skew;
            _instances[i].velocity = Vector2.zero;
        }
    }

    public void ResetObjects()
    {
        System.Array.Resize(ref skewObjects, group.objectNames.Count);
        for (int i = 0; i < skewObjects.Length; ++i)
        {
            if (skewObjects[i] == null)
                skewObjects[i] = new InstanceAnimatorSkewObject();
        }
    }

    void Update()
    {
        if (!group)
            return;

        if (_instances == null || _instances.Length != _group.count)
            ResetTargets();

        if (skewObjects.Length != _group.objectNames.Count)
            ResetObjects();

        Vector3 cameraposition = Camera.main.transform.position;

        for (int i = 0; i < _group.count; ++i)
        {
            InstanceObject instance = _group[i];

            // make sure there is a skew object
            if (instance.objectID >= skewObjects.Length)
                continue;

            // make sure the object is not too far away
            if ((cameraposition - instance.position).sqrMagnitude > maxDistanceSqr)
                continue;

            skewObjects[instance.objectID].Move(instance, ref _instances[i]);
        }
    }
}