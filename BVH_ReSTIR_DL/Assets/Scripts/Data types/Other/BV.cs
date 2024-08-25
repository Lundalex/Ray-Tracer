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
    private (RenderBV, int2) ClassToStruct()
    {
        bool isLeaf = childIndexA == -1 && childIndexB == -1;

        int indexA = isLeaf ? componentStart : -childIndexA;
        int indexB = isLeaf ? totComponents : -childIndexB;

        int2 componentData = new int2(componentStart, totComponents);

        RenderBV boundingVolume = new RenderBV
        {
            min = min,
            max = max,
            indexA = indexA,
            indexB = indexB
        };

        return (boundingVolume, componentData);
    }
    public static (RenderBV[], int2[]) ClassToStruct(List<BV> BVs)
    {
        RenderBV[] boundingVolumes = new RenderBV[BVs.Count];
        int2[] componentDatas = new int2[BVs.Count];
        for (int i = 0; i < BVs.Count; i++)
        {
            (boundingVolumes[i], componentDatas[i]) = BVs[i].ClassToStruct();
        }

        return (boundingVolumes, componentDatas);
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