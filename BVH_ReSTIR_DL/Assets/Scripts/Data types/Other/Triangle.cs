using Resources;
using Unity.Mathematics;
using UnityEngine;
[System.Serializable]
public struct Triangle : IBVHComponent
{
    public int vertex0Index;
    public int vertex1Index;
    public int vertex2Index;
    public int parentIndex;
    public float3 min;
    public float3 max;
    public readonly float3 GetMin() => min;
    public readonly float3 GetMax() => max;
    public void Initialise(Vertex[] vertices, int vertexIndexOffset = 0)
    {
        CalcMin(vertices, vertexIndexOffset);
        CalcMax(vertices, vertexIndexOffset);
    }
    public void CalcMin(Vertex[] vertices, int vertexIndexOffset = 0)
    {
        float3 min = new float3(float.MaxValue, float.MaxValue, float.MaxValue);

        float3 v0Pos = vertices[vertex0Index - vertexIndexOffset].pos;
        float3 v1Pos = vertices[vertex1Index - vertexIndexOffset].pos;
        float3 v2Pos = vertices[vertex2Index - vertexIndexOffset].pos;

        min.x = Mathf.Min(min.x, v0Pos.x, v1Pos.x, v2Pos.x);
        min.y = Mathf.Min(min.y, v0Pos.y, v1Pos.y, v2Pos.y);
        min.z = Mathf.Min(min.z, v0Pos.z, v1Pos.z, v2Pos.z);

        this.min = min;
    }
    public void CalcMax(Vertex[] vertices, int vertexIndexOffset = 0)
    {
        float3 max = new float3(float.MinValue, float.MinValue, float.MinValue);

        float3 v0Pos = vertices[vertex0Index - vertexIndexOffset].pos;
        float3 v1Pos = vertices[vertex1Index - vertexIndexOffset].pos;
        float3 v2Pos = vertices[vertex2Index - vertexIndexOffset].pos;

        max.x = Mathf.Max(max.x, v0Pos.x, v1Pos.x, v2Pos.x);
        max.y = Mathf.Max(max.y, v0Pos.y, v1Pos.y, v2Pos.y);
        max.z = Mathf.Max(max.z, v0Pos.z, v1Pos.z, v2Pos.z);

        this.max = max;
    }

    public void CalcMinMaxTransformed(Vertex[] vertices, Matrix4x4 matrix)
    {
        float3 min = new float3(float.MaxValue, float.MaxValue, float.MaxValue);
        float3 max = new float3(float.MinValue, float.MinValue, float.MinValue);

        float3 v0Pos = vertices[vertex0Index].pos;
        float3 v1Pos = vertices[vertex1Index].pos;
        float3 v2Pos = vertices[vertex2Index].pos;

        float3 transformedV0Pos = Func.Mul(matrix, v0Pos);
        float3 transformedV1Pos = Func.Mul(matrix, v1Pos);
        float3 transformedV2Pos = Func.Mul(matrix, v2Pos);

        min.x = Mathf.Min(min.x, transformedV0Pos.x, transformedV1Pos.x, transformedV2Pos.x);
        min.y = Mathf.Min(min.y, transformedV0Pos.y, transformedV1Pos.y, transformedV2Pos.y);
        min.z = Mathf.Min(min.z, transformedV0Pos.z, transformedV1Pos.z, transformedV2Pos.z);

        max.x = Mathf.Max(max.x, transformedV0Pos.x, transformedV1Pos.x, transformedV2Pos.x);
        max.y = Mathf.Max(max.y, transformedV0Pos.y, transformedV1Pos.y, transformedV2Pos.y);
        max.z = Mathf.Max(max.z, transformedV0Pos.z, transformedV1Pos.z, transformedV2Pos.z);

        this.min = min;
        this.max = max;
    }
    public readonly float3 GetMid() => Func.Avg(GetMin(), GetMax());
};