using Unity.Mathematics;

public interface IBVHComponent
{
    float3 GetMin();
    float3 GetMax();
    void CalcMin(Vertex[] vertices, int vertexIndexOffset = 0);
    void CalcMax(Vertex[] vertices, int vertexIndexOffset = 0);
    float3 GetMid();
}