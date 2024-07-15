using Unity.Mathematics;
using UnityEngine;
public struct Tri : BVHComponent
{
    public float3 vA;
    public float3 vB;
    public float3 vC;
    public float2 uvA;
    public float2 uvB;
    public float2 uvC;
    public float3 normal;
    public int materialKey;
    public int parentKey;
    public float3 GetMax()
    {
        Debug.Log("Tri method GetMax() not allowed to be used!");
        return 0;
    }

    public float3 GetMin()
    {
        Debug.Log("Tri method GetMin() not allowed to be used!");
        return 0;
    }
    public void CalcMax()
    {
        Debug.Log("Tri method CalcMax() not allowed to be used!");
    }
    public void CalcMin()
    {
        Debug.Log("Tri method CalcMin() not allowed to be used!");
    }
    public float3 GetMid()
    {
        Debug.Log("Tri method GetMid() not allowed to be used!");
        return 0;
    }
};