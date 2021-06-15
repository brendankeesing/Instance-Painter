using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
    using UnityEditor;
#endif

[ExecuteInEditMode]
[RequireComponent(typeof(InstanceGroup))]
public class InstanceGameObject : InstanceReferenceManager
{
    public List<GameObject> prefabObjects = new List<GameObject>();

    public void ReloadReferences()
    {
        if (!useReferences)
            return;
        useReferences = false;
        useReferences = true;
    }

    public void UnlinkAllReferences()
    {
        if (!useReferences)
            useReferences = true;

        while (group.count > 0)
        {
            InstanceObject instance = group[group.count - 1];
            if (instance.reference)
            {
                InstanceReference reference = instance.reference;
                reference.UnlinkFromInstanceGroup();
                DestroyImmediate(reference);
            }
            else
                group.RemoveInstance(instance);
        }
    }

    // Resizes the prefab object list
    public void OnObjectChange()
    {
        int difference = group.objectNames.Count - prefabObjects.Count;

        if (difference > 0)
        {
            for (int i = 0; i < difference; ++i)
                prefabObjects.Add(null);
        }
        else if (difference < 0)
        {
            for (int i = 0; i < -difference; ++i)
                prefabObjects.RemoveAt(prefabObjects.Count - 1);
        }
    }

    protected override GameObject CreateReferenceGameObject(InstanceObject instance)
    {
        if (instance.objectID >= prefabObjects.Count || prefabObjects[instance.objectID] == null)
            return base.CreateReferenceGameObject(instance);
        
        // in the editor, we want the instances to still link to the prefab
        #if UNITY_EDITOR
            return (GameObject)PrefabUtility.InstantiatePrefab(prefabObjects[instance.objectID]);
        #else
            return (GameObject)Instantiate(prefabObjects[instance.objectID]);
        #endif
    }

    void Update()
    {
        if (useReferences)
        {
            for (int i = 0; i < group.count; ++i)
                group[i].UpdateReferenceIfChanged();
        }
    }
}
