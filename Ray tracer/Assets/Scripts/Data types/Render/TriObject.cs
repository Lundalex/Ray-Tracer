using Unity.Mathematics;
public struct TriObject
{
    public float3 pos;
    public float3 rot;
    public float3 lastRot;
    public float containedRadius;
    public int triStart;
    public int triEnd;
};