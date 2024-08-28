using Unity.Mathematics;
[System.Serializable]
public struct RenderTriangle
{
    public int vertex0Index;
    public int vertex1Index;
    public int vertex2Index;
    public float3 localNormal;
    public float area;
};