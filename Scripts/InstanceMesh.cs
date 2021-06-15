using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class InstanceMeshLOD
{
    [SerializeField]
    Mesh _mesh;
    Mesh _realMesh;
    public Material[] materials;
    public Material[] fadeMaterials;
    public bool castShadows = true;
    public bool receiveShadows = true;
    public float distance = Mathf.Infinity;
    public bool billboard = false;

    public float distanceSqr
    {
        get { return distance * distance; }
        //set { distance = Mathf.Sqrt(value); }
    }

    [SerializeField]
    Vector3 _basePosition = Vector3.zero;

    [SerializeField]
    Quaternion _baseRotation = Quaternion.identity;

    [SerializeField]
    Vector3 _baseScale = Vector3.one;

    [SerializeField]
    bool _modifyBaseMesh = false;
    public bool modifyBaseMesh { get { return _modifyBaseMesh; } set { if (_modifyBaseMesh != value) { _realMesh = null; _modifyBaseMesh = value; } } }

    public Vector3 basePosition { get { return _basePosition; } set { _realMesh = null; _basePosition = value; } }
    public Quaternion baseRotation { get { return _baseRotation; } set { _realMesh = null; _baseRotation = value; } }
    public Vector3 baseScale { get { return _baseScale; } set { _realMesh = null; _baseScale = value; } }

    float _meshBoundsRadius = 0.0f;

    public Mesh mesh
    {
        get { return _mesh; }
        set
        {
            _mesh = value;
            if (_mesh == null)
                materials = null;
            else if (materials == null)
                materials = new Material[_mesh.subMeshCount];
            else if (materials.Length != _mesh.subMeshCount)
                System.Array.Resize(ref materials, _mesh.subMeshCount);
        }
    }

    public void ResizeMaterials()
    {
        if (materials == null)
            materials = new Material[mesh.subMeshCount];
        else if (materials.Length != mesh.subMeshCount)
            System.Array.Resize(ref materials, mesh.subMeshCount);

        if (fadeMaterials == null)
            fadeMaterials = new Material[mesh.subMeshCount];
        else if (fadeMaterials.Length != mesh.subMeshCount)
            System.Array.Resize(ref fadeMaterials, mesh.subMeshCount);
    }

    public bool IsVisible(InstanceObject instance, Plane[] frustumplanes)
    {
        float radius = _meshBoundsRadius * instance.boundsRadiusMultiplier;
        for (int i = 0; i < frustumplanes.Length; ++i)
        {
            if (frustumplanes[i].GetDistanceToPoint(instance.position) <= -radius)
                return false;
        }
        return true;
    }

    public void Render(InstanceObject io, MaterialPropertyBlock mpb, float opacity, ref Vector3 eyeposition)
    {
        // must have a mesh to render
        if (!_realMesh && !GenerateRealMesh())
            return;

        Color color = io.color;
        color.a = opacity;
        mpb.SetColor("_Color", color);
        int subMeshCount = Mathf.Min(_realMesh.subMeshCount, materials.Length);
        for (int m = 0; m < subMeshCount; ++m)
            Graphics.DrawMesh(_realMesh, billboard ? io.BillboardMatrix(ref eyeposition) : io.matrix, opacity < 0.95f ? fadeMaterials[m] : materials[m], 0, null, m, mpb, castShadows, receiveShadows);
    }

    bool GenerateRealMesh()
    {
        if (!_mesh)
            return false;

        if (!_modifyBaseMesh)
        {
            _realMesh = _mesh;
            return true;
        }

        // use Mesh.CombineMeshes() to apply a matrix to the original mesh
        CombineInstance[] ci = new CombineInstance[_mesh.subMeshCount];
        Matrix4x4 matrix = Matrix4x4.TRS(basePosition, baseRotation, baseScale);
        for (int i = 0; i < _mesh.subMeshCount; ++i)
        {
            ci[i].mesh = _mesh;
            ci[i].subMeshIndex = i;
            ci[i].transform = matrix;
        }

        _realMesh = new Mesh();
        _realMesh.CombineMeshes(ci, false, true);
        _realMesh.Optimize();
        _realMesh.hideFlags = HideFlags.HideAndDontSave; // this magically stops a memory leak error appearing everytime you save the project

        // recalculate mesh radius
        _meshBoundsRadius = _realMesh.bounds.extents.magnitude;

        return true;
    }
}

[System.Serializable]
public class InstanceMeshObject
{
    public List<InstanceMeshLOD> lods = new List<InstanceMeshLOD>();
    public bool hide = false;

    // Ensures the LODs are in the correct order.
    public void UpdateLODs()
    {
        lods = lods.OrderBy(x => x.distance).ToList();
    }

    // Resizes and sorts LODs.
    public void UpdateLODs(int lodcount)
    {
        int difference = lodcount - lods.Count;
        if (difference > 0)
        {
            for (int i = 0; i < difference; ++i)
                lods.Add(new InstanceMeshLOD());
        }
        else if (difference < 0)
        {
            for (int i = 0; i < -difference; ++i)
                lods.RemoveAt(lods.Count - 1);
        }
        lods.Sort();
    }

    // Returns -1 if the LOD does not exist.
    public int GetLOD(float distancesqr)
    {
        for (int i = 0; i < lods.Count; ++i)
        {
            if (distancesqr <= lods[i].distanceSqr)
                return i;
        }
        return -1;
    }
}

[ExecuteInEditMode]
[RequireComponent(typeof(InstanceGroup))]
public class InstanceMesh : InstanceReferenceManager
{
    public List<InstanceMeshObject> objects = new List<InstanceMeshObject>();
    public bool performCulling = false;
    public float fadeDistance = 1.0f;

    // Resizes object list
    public void UpdateObjects()
    {
        int difference = group.objectNames.Count - objects.Count;
        if (difference > 0)
        {
            for (int i = 0; i < difference; ++i)
                objects.Add(new InstanceMeshObject());
        }
        else if (difference < 0)
        {
            for (int i = 0; i < -difference; ++i)
                objects.RemoveAt(objects.Count - 1);
        }
    }

    void Update()
    {
        // prepare camera information
        Camera cam = Camera.main;
        Vector3 eyeposition = cam.transform.position;
        Plane[] frustumplanes = performCulling ? GeometryUtility.CalculateFrustumPlanes(cam) : null;

        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        mpb.Clear();
        for (int i = 0; i < group.count; ++i)
        {
            InstanceObject io = group[i];

            // get object (stop here if the object is hidden)
            if (io.objectID < 0 || io.objectID >= objects.Count || objects[io.objectID].hide)
                continue;
            InstanceMeshObject obj = objects[io.objectID];

            // get the LOD
            float distancesqr = (io.position - eyeposition).sqrMagnitude;
            int lodindex = obj.GetLOD(distancesqr);
            if (lodindex == -1)
                continue;

            // make sure the LOD is visible to the camera
            InstanceMeshLOD lod = obj.lods[lodindex];
            if (performCulling && !lod.IsVisible(io, frustumplanes))
                continue;

            // if no fading, just render it how it is
            if (fadeDistance <= 0.001f)
                lod.Render(io, mpb, 1.0f, ref eyeposition);
            else
            {
                float opacity = 1.0f;

                // get next LOD
                int nextlodindex = lodindex + 1;
                if (nextlodindex < obj.lods.Count)
                {
                    // is it within fade distance?
                    float fadeedgedistance = lod.distance - fadeDistance;
                    if (distancesqr > fadeedgedistance * fadeedgedistance)
                    {
                        opacity = (Mathf.Sqrt(distancesqr) - lod.distance) / -fadeDistance;
                        obj.lods[nextlodindex].Render(io, mpb, 1.0f - opacity, ref eyeposition);
                    }
                }

                // render LOD
                lod.Render(io, mpb, opacity, ref eyeposition);
            }
        }
    }

    // This was used in an earlier idea to combine smaller meshes into another single mesh.
    // Due to LODs, this is no longer feasible, but may be used in the future, so I'll keep it here.
    /*
    bool GenerateCombineMesh()
    {
        if (!_realMesh && !GenerateRealMesh())
            return false;

        // The instances must be joined into a few meshes.
        // The reason why they can't all go into a single mesh is because Unity only allows for 2^16 vertices per mesh.
        int maxvertices = 65535;
        int instancevertexcount = mesh.triangles.Length;
        int instancespermesh = maxvertices / instancevertexcount;
        int nummeshes = group.count / instancespermesh + 1;

        _combineMeshes = new Mesh[nummeshes];

        for (int m = 0; m < nummeshes; ++m)
        {
            int firstinstance = m * instancespermesh;
            int totalinstances = m == nummeshes - 1 ? group.count % instancespermesh : instancespermesh;
            CombineInstance[] combineinstances = new CombineInstance[totalinstances];

            for (int i = 0; i < totalinstances; ++i)
            {
                combineinstances[i].transform = group[firstinstance + i].matrix;
                combineinstances[i].mesh = _realMesh;
                combineinstances[i].subMeshIndex = 0;
            }

            _combineMeshes[m] = new Mesh();
            _combineMeshes[m].CombineMeshes(combineinstances, true, true);
            _combineMeshes[m].Optimize();
        }

        return true;
    }*/
}