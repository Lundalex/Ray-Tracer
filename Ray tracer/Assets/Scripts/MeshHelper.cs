using Unity.Mathematics;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

// Import utils from Resources.cs
using Resources;
// Usage: Utils.(functionName)()

public class MeshHelper : MonoBehaviour
{
    public GameObject[] sceneObjects;
    public int MaxDepthBVH;
    public int SplitResolution; // ex. 10 -> Each BV split will test 10 increments for each component x,y,z (30 tests total)
    public int TriMaxPerOBJ;
    private SceneObjectData[] sceneObjectsData;
    private BoundingVolume[] loadedBoundingVolumes = new BoundingVolume[0];
    private Tri[] loadedTris = new Tri[0];
    private List<(Mesh mesh, int triStartIndex, int bvStartIndex)> LoadedMeshes = new();
    public Tri2[] LoadOBJ(Mesh mesh)
    {
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        int triNum = triangles.Length / 3;

        // Set tris data
        Tri2[] tris = new Tri2[TriMaxPerOBJ == -1 ? triNum : Mathf.Min(triNum, TriMaxPerOBJ)];
        for (int triCount = 0; triCount < (TriMaxPerOBJ == -1 ? triNum : Mathf.Min(triNum, TriMaxPerOBJ)); triCount++)
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
            tris[triCount].min = GetTri2Min(tris[triCount]);
            tris[triCount].max = GetTri2Max(tris[triCount]);
            tris[triCount].mid = Func.Avg(tris[triCount].min, tris[triCount].max);
        }

        return tris;
    }

    private float3 GetTri2Min(Tri2 tri)
    {
        float3 min = new float3(float.MaxValue, float.MaxValue, float.MaxValue);

        min.x = Mathf.Min(min.x, tri.vA.x, tri.vB.x, tri.vC.x);
        min.y = Mathf.Min(min.y, tri.vA.y, tri.vB.y, tri.vC.y);
        min.z = Mathf.Min(min.z, tri.vA.z, tri.vB.z, tri.vC.z);

        return min;
    }
    private float3 GetTri2Min(Tri2[] tris)
    {
        float3 min = new float3(float.MaxValue, float.MaxValue, float.MaxValue);

        foreach (var tri in tris)
        {
            min.x = Mathf.Min(min.x, tri.min.x);
            min.y = Mathf.Min(min.y, tri.min.y);
            min.z = Mathf.Min(min.z, tri.min.z);
        }

        return min;
    }
    private float3 GetTri2Min(List<Tri2> tris)
    {
        float3 min = new float3(float.MaxValue, float.MaxValue, float.MaxValue);

        foreach (var tri in tris)
        {
            min.x = Mathf.Min(min.x, tri.min.x);
            min.y = Mathf.Min(min.y, tri.min.y);
            min.z = Mathf.Min(min.z, tri.min.z);
        }

        return min;
    }

    private float3 GetTri2Max(Tri2 tri)
    {
        float3 max = new float3(float.MinValue, float.MinValue, float.MinValue);

        max.x = Mathf.Max(max.x, tri.vA.x, tri.vB.x, tri.vC.x);
        max.y = Mathf.Max(max.y, tri.vA.y, tri.vB.y, tri.vC.y);
        max.z = Mathf.Max(max.z, tri.vA.z, tri.vB.z, tri.vC.z);

        return max;
    }
    private float3 GetTri2Max(Tri2[] tris)
    {
        float3 max = new float3(float.MinValue, float.MinValue, float.MinValue);

        foreach (var tri in tris)
        {
            max.x = Mathf.Max(max.x, tri.max.x);
            max.y = Mathf.Max(max.y, tri.max.y);
            max.z = Mathf.Max(max.z, tri.max.z);
        }

        return max;
    }
    private float3 GetTri2Max(List<Tri2> tris)
    {
        float3 max = new float3(float.MinValue, float.MinValue, float.MinValue);

        foreach (var tri in tris)
        {
            max.x = Mathf.Max(max.x, tri.max.x);
            max.y = Mathf.Max(max.y, tri.max.y);
            max.z = Mathf.Max(max.z, tri.max.z);
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

    float GetCost(List<Tri2> trisChildA, List<Tri2> trisChildB)
    {
        float costA = GetBoxArea(GetTri2Min(trisChildA), GetTri2Max(trisChildA)) * trisChildA.Count;
        float costB = GetBoxArea(GetTri2Min(trisChildB), GetTri2Max(trisChildB)) * trisChildB.Count;
        float totCost = costA + costB;

        return totCost;
    }

    private void SwapPair<T>(ref T[] array, int indexA, int indexB) => (array[indexB], array[indexA]) = (array[indexA], array[indexB]);

    private int DivideIntoSubGroupsRef(ref Tri2[] tris, int axis, float3 splitCoord, int triStart, int totTris)
    {
        int highestIndexA = triStart - 1;
        int countA = 0;
        for (int triIndex = triStart; triIndex < triStart + totTris; triIndex++)
        {
            float3 pos = tris[triIndex].vA; // vA arbitrarily chosen
            if ((axis == 0 && pos.x < splitCoord.x) ||
                (axis == 1 && pos.y < splitCoord.y) ||
                (axis == 2 && pos.z < splitCoord.z))
            {
                highestIndexA++;
                countA++;
                if (highestIndexA != triIndex)
                {
                    SwapPair(ref tris, highestIndexA, triIndex);
                }
            }
        }

        return countA;
    }

    private (List<Tri2>, List<Tri2>) DivideIntoSubGroupsCopy(Tri2[] tris, int axis, float3 splitCoord, int triStart, int totTris)
    {
        List<Tri2> trisChildA = new List<Tri2>();
        List<Tri2> trisChildB = new List<Tri2>();

        for (int triIndex = triStart; triIndex < triStart + totTris; triIndex++)
        {
            float3 pos = tris[triIndex].mid;
            if ((axis == 0 && pos.x < splitCoord.x) ||
                (axis == 1 && pos.y < splitCoord.y) ||
                (axis == 2 && pos.z < splitCoord.z))
            {
                trisChildA.Add(tris[triIndex]);
            }
            else
            {
                trisChildB.Add(tris[triIndex]);
            }
        }

        return (trisChildA, trisChildB);
    }

    private int RecursivelySplitBV(ref List<BV> BVs, ref Tri2[] tris, int bvParentIndex, BV bvParent, int depth = 0)
    {
        depth += 1;
        if (depth >= MaxDepthBVH) { BVs[bvParentIndex].SetLeaf(); return bvParentIndex; }
        
        (float3 splitCoord, int axis, float cost) leastCostSplit = (0, -1, float.MaxValue);

        // Find the best split point for the parent BV
        float3 diff = bvParent.max - bvParent.min;
        for (int split = 0; split < SplitResolution; split++)
        {
            float3 splitCoord = bvParent.min + diff * (split+0.5f) / SplitResolution;

            for (int axis = 0; axis < 3; axis++)
            {
                // Test splitting the parent bounding box
                List<Tri2> trisChildA;
                List<Tri2> trisChildB;
                (trisChildA, trisChildB) = DivideIntoSubGroupsCopy(tris, axis, splitCoord, bvParent.triStart, bvParent.totTris);

                // Calculate cost (total surface area) of the resulting box split
                float cost = GetCost(trisChildA, trisChildB);

                // Compare the resulting cost with the currently lowest split cost
                if (cost < leastCostSplit.cost) { leastCostSplit = (splitCoord, axis, cost); }
            }
        }

        // No valid split found (probably only 1 or 2 tris)
        if (leastCostSplit.axis == -1) { BVs[bvParentIndex].SetLeaf(); return bvParentIndex; }

        // Divide the bounding box using the best tried split
        int countA = DivideIntoSubGroupsRef(ref tris, leastCostSplit.axis, leastCostSplit.splitCoord, bvParent.triStart, bvParent.totTris);

        // Get tris for either child
        List<Tri2> bestTrisChildA = tris.Skip(bvParent.triStart).Take(countA).ToList();
        List<Tri2> bestTrisChildB = tris.Skip(bvParent.triStart + countA).Take(bvParent.totTris - countA).ToList();

        // Recursively split child A
        int furthestChildIndex = bvParentIndex;
        if (bestTrisChildA.Count != 0)
        {
            int childIndexA = bvParentIndex + 1;
            BVs[bvParentIndex].childIndexA = childIndexA;
            BVs.Add(new BV(GetTri2Min(bestTrisChildA), GetTri2Max(bestTrisChildA), bvParent.triStart, bestTrisChildA.Count));
            DebugUtils.ChildIndexValidation(childIndexA, BVs.Count);
            furthestChildIndex = RecursivelySplitBV(ref BVs, ref tris, childIndexA, BVs[childIndexA], depth);
        }

        // Recursively split child B
        if (bestTrisChildB.Count != 0)
        {
            int childIndexB = furthestChildIndex + 1;
            BVs[bvParentIndex].childIndexB = childIndexB;
            BVs.Add(new BV(GetTri2Min(bestTrisChildB), GetTri2Max(bestTrisChildB), bvParent.triStart + bestTrisChildA.Count, bestTrisChildB.Count));
            DebugUtils.ChildIndexValidation(childIndexB, BVs.Count);
            furthestChildIndex = RecursivelySplitBV(ref BVs, ref tris, childIndexB, BVs[childIndexB], depth);
        }

        // Return the currently furthest child index
        return furthestChildIndex;
    }

    private (int, int) ConstructBVHFromObj(ref BoundingVolume[] boundingVolumes, ref Tri[] tris, Mesh mesh)
    {
        Tri2[] newTris = LoadOBJ(mesh);

        float3 min = GetTri2Min(newTris);
        float3 max = GetTri2Max(newTris);
        List<BV> newBVs = new List<BV>
        {
            // First BV
            new BV(min, max, 0, newTris.Length, 1, 2)
        };

        // Construct the BVH
        Stopwatch stopwatch = Stopwatch.StartNew();
        RecursivelySplitBV(ref newBVs, ref newTris, 0, newBVs[0]);
        for (int i = 0; i < newBVs.Count; i++)
        {
            if (newBVs[i].childIndexA != -1) newBVs[i].childIndexA += boundingVolumes.Length;
            if (newBVs[i].childIndexB != -1) newBVs[i].childIndexB += boundingVolumes.Length;
            newBVs[i].triStart += tris.Length;
        }
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

        // Create all scene objects
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

            // Get mesh index
            int meshIndex = Utils.GetMeshIndex(LoadedMeshes, mesh);
            if (meshIndex == -1)
            {
                // Load mesh (construct it's BVH) if it has not yet been loaded
                LoadedMeshes.Add(new(mesh, loadedTris.Length, loadedBoundingVolumes.Length));
                ConstructBVHFromObj(ref loadedBoundingVolumes, ref loadedTris, mesh);
                meshIndex = LoadedMeshes.Count - 1;
            }

            // Set start index values
            sceneObjectData.triStartIndex = LoadedMeshes[meshIndex].triStartIndex;
            sceneObjectData.bvStartIndex = LoadedMeshes[meshIndex].bvStartIndex;

            // Add scene object data to the array
            sceneObjectsData[i] = sceneObjectData;
        }

        return (loadedBoundingVolumes, loadedTris, sceneObjectsData);
    }
}