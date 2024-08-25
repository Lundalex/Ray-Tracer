using Unity.Mathematics;
[System.Serializable]
public struct RenderBV
{
    public float3 min;
    public float3 max;
    public int indexA; // childIndexA / componentsStart, a < 0 <= b
    public int indexB; // childIndexB / totComponents, a < 0 <= b
};