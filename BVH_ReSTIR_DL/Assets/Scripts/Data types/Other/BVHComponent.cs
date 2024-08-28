using Unity.Mathematics;

public interface IBVHComponent
{
    float3 GetMin();
    float3 GetMax();
    void CalcMin(float3 v0Pos, float3 v1Pos, float3 v2Pos);
    void CalcMax(float3 v0Pos, float3 v1Pos, float3 v2Pos);
    float3 GetMid();
}