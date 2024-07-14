using Matrix4x4 = UnityEngine.Matrix4x4;
using Vector4 = UnityEngine.Vector4;
using Unity.Mathematics;
using Resources;
using UnityEngine;
public struct SceneObjectData : BVHComponent
{
    public Matrix4x4 worldToLocalMatrix;
    public Matrix4x4 localToWorldMatrix;
    public int materialKey;
    public int bvStartIndex;
    public float3 min;
    public float3 max;
    public float3 GetMin() => min;
    public float3 GetMax() => max;
    public void CalcMax()
    {
        Debug.Log("SceneObjectData method CalcMax() not allowed to be used!");
    }

    public void CalcMin()
    {
        Debug.Log("SceneObjectData method CalcMin() not allowed to be used!");
    }
    public float3 GetMid() => Func.Avg(GetMin(), GetMax());
};