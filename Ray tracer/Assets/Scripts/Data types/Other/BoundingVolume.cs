using System.Collections.Generic;
using Unity.Mathematics;
public class BV
{
    public float3 min;
    public float3 max;
    public int componentStart;
    public int totComponents;
    public int childIndexA;
    public int childIndexB;
    public BV(float3 min, float3 max, int componentStart = -1, int totComponents = -1, int childIndexA = -1, int childIndexB = -1)
    {
        this.min = min;
        this.max = max;
        this.componentStart = componentStart;
        this.totComponents = totComponents;
        this.childIndexA = childIndexA;
        this.childIndexB = childIndexB;
    }
    private BoundingVolume ThisToStruct()
    {
        BoundingVolume boundingVolume = new BoundingVolume
        {
            min = min,
            max = max,
            componentStart = componentStart,
            totComponents = totComponents,
            childIndexA = childIndexA,
            childIndexB = childIndexB
        };

        return boundingVolume;
    }
    public static BoundingVolume[] ClassToStruct(List<BV> BVs)
    {
        BoundingVolume[] boundingVolumes = new BoundingVolume[BVs.Count];
        for (int i = 0; i < BVs.Count; i++)
        {
            boundingVolumes[i] = BVs[i].ThisToStruct();
        }

        return boundingVolumes;
    }
    public bool IsLeaf()
    {
        return childIndexA == -1 && childIndexB == -1;
    }
    public void SetLeaf()
    {
        childIndexA = -1;
        childIndexB = -1;
    }
};
public struct BoundingVolume
{
    public float3 min;
    public float3 max;
    public int componentStart;
    public int totComponents;
    public int childIndexA;
    public int childIndexB;
};