using Unity.Mathematics;

public interface BVHComponent
{
    float3 GetMin();
    float3 GetMax();
    void CalcMin();
    void CalcMax();
    float3 GetMid();
}