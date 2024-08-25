using Matrix4x4 = UnityEngine.Matrix4x4;
using Unity.Mathematics;
using Resources;
using UnityEngine;
[System.Serializable]
public struct SceneObjectData : IBVHComponent
{
    public float4x4 worldToLocalMatrix;
    public float4x4 localToWorldMatrix;
    public int materialIndex;
    public int bvStartIndex;
    public int maxDepthBVH;
    public float areaApprox;
    public float3 min;
    public float3 max;
    public readonly float3 GetMin() => min;
    public readonly float3 GetMax() => max;
    public readonly void CalcMax(Vertex[] vertices, int vertexIndexOffset = 0)
    {
        Debug.Log("SceneObjectData method CalcMax() not allowed to be used!");
    }

    public readonly void CalcMin(Vertex[] vertices, int vertexIndexOffset = 0)
    {
        Debug.Log("SceneObjectData method CalcMin() not allowed to be used!");
    }
    public readonly float3 GetMid() => Func.Avg(GetMin(), GetMax());
};