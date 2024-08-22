using Unity.Mathematics;
public struct HitInfo
{
    public float dst;
    public float3 hitPoint;
    public float2 uv;
    public float3 normal;
    public float3 incomingDir;
    public int materialIndex;
};