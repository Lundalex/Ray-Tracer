using Unity.Mathematics;

[System.Serializable]
public class MeshData
{
    public Triangle[] triangles;
    public float3 localMin;
    public float3 localMax;
    public int componentStartIndex;
    public int bvStartIndex;
    public string meshKey;

    public MeshData(Triangle[] triangles, float3 localMin, float3 localMax, int componentStartIndex, int bvStartIndex, string meshKey)
    {
        this.triangles = triangles;
        this.localMin = localMin;
        this.localMax = localMax;
        this.componentStartIndex = componentStartIndex;
        this.bvStartIndex = bvStartIndex;
        this.meshKey = meshKey;
    }
}