using Unity.Mathematics;
using UnityEngine;
using System.Collections.Generic;

// Import utils from Resources.cs
using Resources;
// Usage: Utils.(functionName)()

public class MeshHelper : MonoBehaviour
{
    public Mesh[] Meshes;
    public int MaxBVHDepth;
    public int SplitResolution; // ex. 10 -> Each BV split will test 10 increments for each component x,y,z (1000 tests total)
    public Tri2[] LoadOBJ(int meshIndex, float scale)
    {
        Mesh mesh = Meshes[meshIndex];
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        int triNum = triangles.Length / 3;

        // Set tris data
        Tri2[] tri2s = new Tri2[triNum];
        for (int triCount = 0; triCount < triNum; triCount++)
        {
            int triCount3 = 3 * triCount;
            int indexA = triangles[triCount3];
            int indexB = triangles[triCount3 + 1];
            int indexC = triangles[triCount3 + 2];

            tri2s[triCount] = new Tri2
            {
                vA = vertices[indexA] * scale,
                vB = vertices[indexB] * scale,
                vC = vertices[indexC] * scale,
            };
        }

        return tri2s;
    }

    private float3 GetTri2Min(params Tri2[] tris)
    {
        float3 min = new float3(float.MaxValue, float.MaxValue, float.MaxValue);

        foreach (var tri in tris)
        {
            min.x = Mathf.Min(min.x, tri.vA.x, tri.vB.x, tri.vC.x);
            min.y = Mathf.Min(min.y, tri.vA.y, tri.vB.y, tri.vC.y);
            min.z = Mathf.Min(min.z, tri.vA.z, tri.vB.z, tri.vC.z);
        }

        return min;
    }

    private float3 GetTri2Max(params Tri2[] tris)
    {
        float3 max = new float3(float.MinValue, float.MinValue, float.MinValue);

        foreach (var tri in tris)
        {
            max.x = Mathf.Max(max.x, tri.vA.x, tri.vB.x, tri.vC.x);
            max.y = Mathf.Max(max.y, tri.vA.y, tri.vB.y, tri.vC.y);
            max.z = Mathf.Max(max.z, tri.vA.z, tri.vB.z, tri.vC.z);
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

    private void SwapPair<T>(ref T[] array, int indexA, int indexB)
    {
        (array[indexB], array[indexA]) = (array[indexA], array[indexB]);
    }

    // Should be implemented directly in "ConstructBVHFromObj"
    // void SplitBoundingBox()
    // {
    //     return;
    // }

    private void SplitBoundingVolume(ref List<BoundingVolume> BVs, ref Tri2[] tris, int bvParentIndex, float3 bvParentMin, float3 bvParentMax, int depth)
    {
        if (depth < MaxBVHDepth)
        {
            // Test dividing the parent BV in a grid formation, incrementing between bvParentMin and bvParentMax
            float3 diff = bvParentMax - bvParentMin;
            for (float splitX = bvParentMax.x; splitX < bvParentMax.x; splitX += diff.x/SplitResolution)
            {
                for (float splitY = bvParentMax.y; splitY < bvParentMax.y; splitY += diff.y/SplitResolution)
                {
                    for (float splitZ = bvParentMax.z; splitZ < bvParentMax.z; splitZ += diff.z/SplitResolution)
                    {
                        
                    }   
                }
            }
        }
    }

    // Should also return the bounding volumes list in some way, since that data is needed for the BVH
    public (Box[], Tri[]) ConstructBVHFromObj(int meshIndex, float scale)
    {
        Tri2[] tris = LoadOBJ(meshIndex, scale);

        float3 min = GetTri2Min(tris);
        float3 max = GetTri2Max(tris);
        List<BoundingVolume> BVs = new List<BoundingVolume>
        {
            // First depth BV
            new BoundingVolume(min, max, 0, tris.Length, 1, 2)
        };

        SplitBoundingVolume(ref BVs, ref tris, 0, min, max, 0);

        // --- other ---



        // --- other ---

        Box[] boxes = new Box[2];
        boxes[0] = new Box { vA = 1, vB = 5, materialKey = 0 };
        return (boxes, Utils.TrisFromTri2s(tris));
    }
}