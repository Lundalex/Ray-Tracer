using Matrix4x4 = UnityEngine.Matrix4x4;
using Unity.Mathematics;
using Resources2;
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
    public readonly void CalcMax(float3 v0Pos = new float3(), float3 v1Pos = new float3(), float3 v2Pos = new float3())
    {
        Debug.Log("SceneObjectData method CalcMax() not allowed to be used!");
    }

    public readonly void CalcMin(float3 v0Pos = new float3(), float3 v1Pos = new float3(), float3 v2Pos = new float3())
    {
        Debug.Log("SceneObjectData method CalcMin() not allowed to be used!");
    }
    public readonly float3 GetMid() => Func.Avg(GetMin(), GetMax());
};