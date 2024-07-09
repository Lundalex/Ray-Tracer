using Unity.Mathematics;
using UnityEngine;

// Import utils from Resources.cs
using Resources;
// Usage: Utils.(functionName)()

public class MeshHelper : MonoBehaviour
{
    public Mesh[] Meshes;
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

    public (Box[], Tri[]) ConstructBVHFromObj(int meshIndex, float scale)
    {
        Tri2[] meshTris = LoadOBJ(meshIndex, scale);

        // --- Construct BVH ---










        Box[] boxes = new Box[2];
        boxes[0] = new Box { vA = 1, vB = 5, materialKey = 0 };
        return (boxes, Utils.TrisFromTri2s(meshTris));
    }
}