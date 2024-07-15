using Unity.Mathematics;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;

// Import utils from Resources.cs
using Resources;
// Usage: Utils.(functionName)()

public class MeshHelper : MonoBehaviour
{
    public GameObject[] sceneObjects;
    public int MaxDepthSceneBVH;
    public int SplitResolution; // ex. 10 -> Each BV split will test 10 increments for each component x,y,z (30 tests total)
    public bool doReloadSceneBVH = true;
    public Main m;

    private SceneObjectData[] sceneObjectsData;
    private BoundingVolume[] loadedBoundingVolumes = new BoundingVolume[0];
    private List<(Mesh mesh, Tri2[] meshTris, int componentStartIndex, int bvStartIndex)> LoadedMeshes = new();
    private Tri[] loadedTris = new Tri[0];
    private int[] loadedMeshesLookup;
    private int lastSceneBVHLength = 0;

    private void OnValidate()
    {
        if (m.ProgramStarted) m.DoUpdateSettings = true;
    }
    public Tri2[] LoadMesh(Mesh mesh)
    {
        Vector3[] vertices = mesh.vertices;
        Vector2[] UVs = mesh.uv;
        int[] triangles = mesh.triangles;
        int triNum = triangles.Length / 3;
        bool containsUVs = UVs.Length > 0;

        // Set tris data
        Tri2[] tris = new Tri2[triNum];
        for (int triCount = 0; triCount < triNum; triCount++)
        {
            int triCount3 = 3 * triCount;
            int indexA = triangles[triCount3];
            int indexB = triangles[triCount3 + 1];
            int indexC = triangles[triCount3 + 2];

            tris[triCount] = new Tri2
            {
                vA = vertices[indexA],
                vB = vertices[indexB],
                vC = vertices[indexC],
            };
            if (containsUVs)
            {
                tris[triCount].uvA = UVs[indexA];
                tris[triCount].uvB = UVs[indexB];
                tris[triCount].uvC = UVs[indexC];
            }
            tris[triCount].CalcMin();
            tris[triCount].CalcMax();
        }

        return tris;
    }
    private float3 GetMin<T>(T[] components) where T : BVHComponent
    {
        float3 min = new float3(float.MaxValue, float.MaxValue, float.MaxValue);

        foreach (var component in components)
        {
            min.x = Mathf.Min(min.x, component.GetMin().x);
            min.y = Mathf.Min(min.y, component.GetMin().y);
            min.z = Mathf.Min(min.z, component.GetMin().z);
        }

        return min;
    }
    private float3 GetMin<T>(List<T> components) where T : BVHComponent
    {
        float3 min = new float3(float.MaxValue, float.MaxValue, float.MaxValue);

        foreach (var component in components)
        {
            min.x = Mathf.Min(min.x, component.GetMin().x);
            min.y = Mathf.Min(min.y, component.GetMin().y);
            min.z = Mathf.Min(min.z, component.GetMin().z);
        }

        return min;
    }

    private float3 GetMax<T>(T[] components) where T : BVHComponent
    {
        float3 max = new float3(float.MinValue, float.MinValue, float.MinValue);

        foreach (var component in components)
        {
            max.x = Mathf.Max(max.x, component.GetMax().x);
            max.y = Mathf.Max(max.y, component.GetMax().y);
            max.z = Mathf.Max(max.z, component.GetMax().z);
        }

        return max;
    }
    private float3 GetMax<T>(List<T> components) where T : BVHComponent
    {
        float3 max = new float3(float.MinValue, float.MinValue, float.MinValue);

        foreach (var component in components)
        {
            max.x = Mathf.Max(max.x, component.GetMax().x);
            max.y = Mathf.Max(max.y, component.GetMax().y);
            max.z = Mathf.Max(max.z, component.GetMax().z);
        }

        return max;
    }

    private float GetBoxArea(float3 vA, float3 vB)
    {
        float length = Mathf.Abs(vA.x - vB.x);
        float width = Mathf.Abs(vA.y - vB.y);
        float height = Mathf.Abs(vA.z - vB.z);

        float area = 2 * (length * width + width * height + height * length);
        
        return area;
    }

    float GetCost<T>(List<T> componentsChildA, List<T> componentsChildB) where T : BVHComponent
    {
        float costA = GetBoxArea(GetMin(componentsChildA), GetMax(componentsChildA)) * componentsChildA.Count;
        float costB = GetBoxArea(GetMin(componentsChildB), GetMax(componentsChildB)) * componentsChildB.Count;
        float totCost = costA + costB;

        return totCost;
    }

    private void SwapPair<T>(ref T[] array, int indexA, int indexB) where T : BVHComponent => (array[indexB], array[indexA]) = (array[indexA], array[indexB]);

    private int DivideIntoSubGroupsRef<T>(ref T[] components, int axis, float3 splitCoord, int componentStart, int totComponents) where T : BVHComponent
    {
        int highestIndexA = componentStart - 1;
        int countA = 0;
        for (int componentIndex = componentStart; componentIndex < componentStart + totComponents; componentIndex++)
        {
            float3 pos = components[componentIndex].GetMid();
            if ((axis == 0 && pos.x < splitCoord.x) ||
                (axis == 1 && pos.y < splitCoord.y) ||
                (axis == 2 && pos.z < splitCoord.z))
            {
                highestIndexA++;
                countA++;
                if (highestIndexA != componentIndex)
                {
                    SwapPair(ref components, highestIndexA, componentIndex);
                }
            }
        }

        return countA;
    }

    private (List<T>, List<T>) DivideIntoSubGroupsCopy<T>(T[] components, int axis, float3 splitCoord, int componentStart, int totComponent) where T : BVHComponent
    {
        List<T> componentsChildA = new List<T>();
        List<T> componentsChildB = new List<T>();

        for (int componentIndex = componentStart; componentIndex < componentStart + totComponent; componentIndex++)
        {
            float3 pos = components[componentIndex].GetMid();
            if ((axis == 0 && pos.x < splitCoord.x) ||
                (axis == 1 && pos.y < splitCoord.y) ||
                (axis == 2 && pos.z < splitCoord.z))
            {
                componentsChildA.Add(components[componentIndex]);
            }
            else
            {
                componentsChildB.Add(components[componentIndex]);
            }
        }

        return (componentsChildA, componentsChildB);
    }

    private int RecursivelySplitBV<T>(ref List<BV> BVs, ref T[] components, int bvParentIndex, BV bvParent, int maxDepth, int depth = 0) where T : BVHComponent
    {
        depth += 1;
        if (depth >= maxDepth) { BVs[bvParentIndex].SetLeaf(); return bvParentIndex; }
        
        (float3 splitCoord, int axis, float cost) leastCostSplit = (0, -1, float.MaxValue);

        // Find the best split point for the parent BV
        float3 diff = bvParent.max - bvParent.min;
        for (int split = 0; split < SplitResolution; split++)
        {
            float3 splitCoord = bvParent.min + diff * (split+0.5f) / SplitResolution;

            for (int axis = 0; axis < 3; axis++)
            {
                // Test splitting the parent bounding box
                List<T> componentsChildA;
                List<T> componentsChildB;
                (componentsChildA, componentsChildB) = DivideIntoSubGroupsCopy(components, axis, splitCoord, bvParent.componentStart, bvParent.totComponents);

                // Calculate cost (total surface area) of the resulting box split
                float cost = GetCost(componentsChildA, componentsChildB);

                // Compare the resulting cost with the currently lowest split cost
                if (cost < leastCostSplit.cost) { leastCostSplit = (splitCoord, axis, cost); }
            }
        }

        // No valid split found (probably only 1 or 2 components
        if (leastCostSplit.axis == -1) { BVs[bvParentIndex].SetLeaf(); return bvParentIndex; }

        // Divide the bounding box using the best tried split
        int countA = DivideIntoSubGroupsRef(ref components, leastCostSplit.axis, leastCostSplit.splitCoord, bvParent.componentStart, bvParent.totComponents);

        // Get components for either child
        List<T> componentsBestChildA = components.Skip(bvParent.componentStart).Take(countA).ToList();
        List<T> componentsBestChildB = components.Skip(bvParent.componentStart + countA).Take(bvParent.totComponents - countA).ToList();

        // Recursively split child A
        int furthestChildIndex = bvParentIndex;
        if (componentsBestChildA.Count != 0)
        {
            int childIndexA = bvParentIndex + 1;
            BVs[bvParentIndex].childIndexA = childIndexA;
            BVs.Add(new BV(GetMin(componentsBestChildA), GetMax(componentsBestChildA), bvParent.componentStart, componentsBestChildA.Count));
            DebugUtils.ChildIndexValidation(childIndexA, BVs.Count);
            furthestChildIndex = RecursivelySplitBV(ref BVs, ref components, childIndexA, BVs[childIndexA], maxDepth, depth);
        }

        // Recursively split child B
        if (componentsBestChildB.Count != 0)
        {
            int childIndexB = furthestChildIndex + 1;
            BVs[bvParentIndex].childIndexB = childIndexB;
            BVs.Add(new BV(GetMin(componentsBestChildB), GetMax(componentsBestChildB), bvParent.componentStart + componentsBestChildA.Count, componentsBestChildB.Count));
            DebugUtils.ChildIndexValidation(childIndexB, BVs.Count);
            furthestChildIndex = RecursivelySplitBV(ref BVs, ref components, childIndexB, BVs[childIndexB], maxDepth, depth);
        }

        // Return the currently furthest child index
        return furthestChildIndex;
    }

    private (int, int) ConstructBVH(ref BoundingVolume[] boundingVolumes, Tri2[] newTris, ref Tri[] tris, int maxDepth)
    {
        float3 objectMin = GetMin(newTris);
        float3 objectMax = GetMax(newTris);
        List<BV> newBVs = new List<BV> { new BV(objectMin, objectMax, 0, newTris.Length, 1, 2) };

        // Construct the BVH
        Stopwatch stopwatch = Stopwatch.StartNew();
        RecursivelySplitBV(ref newBVs, ref newTris, 0, newBVs[0], maxDepth);

        int bvLength = boundingVolumes.Length;
        int trisLength = tris.Length;
        Parallel.For(0, newBVs.Count, i =>
        {
            if (newBVs[i].childIndexA != -1) newBVs[i].childIndexA += bvLength;
            if (newBVs[i].childIndexB != -1) newBVs[i].childIndexB += bvLength;
            newBVs[i].componentStart += trisLength;
        });
        DebugUtils.LogStopWatch("BVH construction", ref stopwatch);

        // Convert to bounding volume struct variant for shader buffer transfer
        BoundingVolume[] newBoundingVolumes = BV.ClassToStruct(newBVs);

        // Add new bounding volumes & tris to existing arrays
        tris = tris.Concat(Utils.TrisFromTri2s(newTris)).ToArray();
        boundingVolumes = boundingVolumes.Concat(newBoundingVolumes).ToArray();

        return (newBoundingVolumes.Length, newTris.Length);
    }

    public (BoundingVolume[], Tri[], SceneObjectData[]) CreateSceneObjects()
    {
        sceneObjectsData ??= new SceneObjectData[sceneObjects.Length];
        loadedMeshesLookup ??= new int[sceneObjects.Length];
        int[] BVHDepths = new int[sceneObjects.Length + 1];
        BVHDepths[sceneObjects.Length] = MaxDepthSceneBVH;

        // Create all scene objects & triangle BVHs
        for (int i = 0; i < sceneObjects.Length; i++)
        {
            // Retrieve relevant game object data
            GameObject sceneObject = sceneObjects[i];
            Transform transform = sceneObject.transform;
            SceneObjectSettings sceneObjectSettings = sceneObject.GetComponentInChildren<SceneObjectSettings>();
            Mesh mesh = sceneObject.GetComponentInChildren<MeshFilter>().mesh;

            SceneObjectData sceneObjectData = new SceneObjectData();

            // Set transformation matrices
            sceneObjectData.worldToLocalMatrix = Utils.CreateWorldToLocalMatrix(transform.position, transform.rotation.eulerAngles, transform.localScale);
            sceneObjectData.localToWorldMatrix = sceneObjectData.worldToLocalMatrix.inverse;

            // Set material key
            sceneObjectData.materialKey = sceneObjectSettings.MaterialKey;
            sceneObjectData.MaxDepthBVH = sceneObjectSettings.MaxDepthBVH;
            BVHDepths[i] = sceneObjectData.MaxDepthBVH;

            // Get mesh index
            int meshIndex = Utils.GetMeshIndex(LoadedMeshes, mesh);
            if (meshIndex == -1)
            {
                // Load mesh (construct it's BVH) if it has not yet been loaded
                LoadedMeshes.Add(new(mesh, LoadMesh(mesh), loadedTris.Length, loadedBoundingVolumes.Length));
                ConstructBVH(ref loadedBoundingVolumes, LoadMesh(mesh), ref loadedTris, sceneObjectData.MaxDepthBVH);
                meshIndex = LoadedMeshes.Count - 1;
            }
            loadedMeshesLookup[i] = meshIndex;

            // Set start index values
            sceneObjectData.bvStartIndex = LoadedMeshes[meshIndex].bvStartIndex;

            // Add scene object data to the array;
            sceneObjectsData[i] = sceneObjectData;
        }

        m.rtShader.SetInt("MaxBVHDepth", Func.MaxInt(MaxDepthSceneBVH));

        // --- Scene object BVH ---

        Stopwatch stopwatch = Stopwatch.StartNew();

        Utils.RemoveFromEndOfArray(ref loadedBoundingVolumes, lastSceneBVHLength);

        // Transform tri mesh to global space
        for (int i = 0; i < sceneObjects.Length; i++)
        {
            SceneObjectData sceneObjectData = sceneObjectsData[i];
            Tri2[] sceneObjectTris = LoadedMeshes[loadedMeshesLookup[i]].meshTris;
            float3 min = new float3(float.MaxValue, float.MaxValue, float.MaxValue);
            float3 max = new float3(float.MinValue, float.MinValue, float.MinValue);
            for (int j = 0; j < sceneObjectTris.Length; j++)
            {
                sceneObjectTris[j].CalcMinMaxTransformed(sceneObjectData.localToWorldMatrix, min, max);
            }

            sceneObjectsData[i].min = GetMin(sceneObjectTris);
            sceneObjectsData[i].max = GetMax(sceneObjectTris);
        }

        float3 sceneMin = GetMin(sceneObjectsData);
        float3 sceneMax = GetMax(sceneObjectsData);
        List<BV> newBVs = new List<BV> { new BV(sceneMin, sceneMax, 0, sceneObjectsData.Length, 1, 2) };

        // Construct the BVH
        RecursivelySplitBV(ref newBVs, ref sceneObjectsData, 0, newBVs[0], MaxDepthSceneBVH);
        m.rtShader.SetInt("SceneBVHStartIndex", loadedBoundingVolumes.Length);
        Parallel.For(0, newBVs.Count, i =>
        {
            if (newBVs[i].childIndexA != -1) newBVs[i].childIndexA += loadedBoundingVolumes.Length;
            if (newBVs[i].childIndexB != -1) newBVs[i].childIndexB += loadedBoundingVolumes.Length;
        });

        DebugUtils.LogStopWatch("BVH construction (scene objects)", ref stopwatch);

        // Replace existing scene BVH with new BVH data
        BoundingVolume[] newBoundingVolumes = BV.ClassToStruct(newBVs);
        loadedBoundingVolumes = loadedBoundingVolumes.Concat(newBoundingVolumes).ToArray();
        lastSceneBVHLength = newBoundingVolumes.Length;

        return (loadedBoundingVolumes, loadedTris, sceneObjectsData);
    }
}