using Unity.Mathematics;
public struct TriObject
{
    public int rootIndexBVH;
    public float4x4 worldToLocal;
    public float4x4 localToWorld;
    public int materialKey;
};