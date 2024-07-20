using Unity.Mathematics;
using UnityEngine;
using Resources;

public struct Tri2 : BVHComponent // Triangle
{
    public float3 vA;
    public float3 vB;
    public float3 vC;
    public float2 uvA;
    public float2 uvB;
    public float2 uvC;
    public float3 min;
    public float3 max;
    public float3 GetMin() => min;
    public float3 GetMax() => max;
    // public float3 GetMin()
    // {
    //     float3 min = new float3(float.MaxValue, float.MaxValue, float.MaxValue);

    //     min.x = Mathf.Min(min.x, vA.x, vB.x, vC.x);
    //     min.y = Mathf.Min(min.y, vA.y, vB.y, vC.y);
    //     min.z = Mathf.Min(min.z, vA.z, vB.z, vC.z);
    //     bool3 tt = min != this.min;
    //     if (tt.x || tt.y || tt.z)
    //     {
    //         int a = 0;
    //     }

    //     return min;
    // }
    // public float3 GetMax()
    // {
    //     float3 max = new float3(float.MinValue, float.MinValue, float.MinValue);

    //     max.x = Mathf.Max(max.x, vA.x, vB.x, vC.x);
    //     max.y = Mathf.Max(max.y, vA.y, vB.y, vC.y);
    //     max.z = Mathf.Max(max.z, vA.z, vB.z, vC.z);

    //     return max;
    // }
    public void CalcMin()
    {
        float3 min = new float3(float.MaxValue, float.MaxValue, float.MaxValue);

        min.x = Mathf.Min(min.x, vA.x, vB.x, vC.x);
        min.y = Mathf.Min(min.y, vA.y, vB.y, vC.y);
        min.z = Mathf.Min(min.z, vA.z, vB.z, vC.z);

        this.min = min;
    }
    public void CalcMax()
    {
        float3 max = new float3(float.MinValue, float.MinValue, float.MinValue);

        max.x = Mathf.Max(max.x, vA.x, vB.x, vC.x);
        max.y = Mathf.Max(max.y, vA.y, vB.y, vC.y);
        max.z = Mathf.Max(max.z, vA.z, vB.z, vC.z);

        this.max = max;
    }

    public void CalcMinMaxTransformed(Matrix4x4 matrix, float3 min, float3 max)
    {
        float3 transformedVA = Func.Mul(matrix, vA);
        float3 transformedVB = Func.Mul(matrix, vB);
        float3 transformedVC = Func.Mul(matrix, vC);

        min.x = Mathf.Min(min.x, transformedVA.x, transformedVB.x, transformedVC.x);
        min.y = Mathf.Min(min.y, transformedVA.y, transformedVB.y, transformedVC.y);
        min.z = Mathf.Min(min.z, transformedVA.z, transformedVB.z, transformedVC.z);

        max.x = Mathf.Max(max.x, transformedVA.x, transformedVB.x, transformedVC.x);
        max.y = Mathf.Max(max.y, transformedVA.y, transformedVB.y, transformedVC.y);
        max.z = Mathf.Max(max.z, transformedVA.z, transformedVB.z, transformedVC.z);

        this.min = min;
        this.max = max;
    }
    public float3 GetMid() => Func.Avg(GetMin(), GetMax());
};