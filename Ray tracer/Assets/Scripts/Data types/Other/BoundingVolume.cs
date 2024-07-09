using Unity.Mathematics;
public class BoundingVolume
{
    public float3 min;
    public float3 max;
    public int triStart;
    public int totTris;
    public int childVolumeAIndex;
    public int childVolumeBIndex;

    public BoundingVolume(float3 min, float3 max, int triStart = -1, int totTris = -1, int childVolumeAIndex = -1, int childVolumeBIndex = -1)
    {
        this.min = min;
        this.max = max;
        this.triStart = triStart;
        this.totTris = totTris;
        this.childVolumeAIndex = childVolumeAIndex;
        this.childVolumeBIndex = childVolumeBIndex;
    }

    public bool IsLeaf()
    {
        return childVolumeAIndex == -1 && childVolumeBIndex == -1;
    }
};