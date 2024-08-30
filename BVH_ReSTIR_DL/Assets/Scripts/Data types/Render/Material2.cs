using Unity.Mathematics;
public struct Material2
{
    public float brightness;

    // Color map
    public float3 col;
    public int2 colTexLoc;
    public int2 colTexDims;

    // Bump map
    public float bump;
    public int2 bumpTexLoc;
    public int2 bumpTexDims;

    // Smoothness
    public float smoothness;
    public int2 smoothnessTexLoc;
    public int2 smoothnessTexDims;
    
    // Normals map
    public int2 normalsTexLoc;
    public int2 normalsTexDims;
};