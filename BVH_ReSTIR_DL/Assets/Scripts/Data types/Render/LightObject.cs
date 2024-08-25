using Unity.Mathematics;
using UnityEngine;
[System.Serializable]
public struct LightObject
{
    public float4x4 localToWorldMatrix;
    public float areaApprox;
    public float brightness;
    public int triStart;
    public int totTris;
}