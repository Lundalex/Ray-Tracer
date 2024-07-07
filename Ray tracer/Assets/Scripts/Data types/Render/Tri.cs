using Unity.Mathematics;
public struct Tri // Triangle
{
    public float3 vA;
    public float3 vB;
    public float3 vC;
    public float3 normal;
    public int materialKey;
    public int parentKey;
};