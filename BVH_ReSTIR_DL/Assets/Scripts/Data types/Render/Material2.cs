using Unity.Mathematics;
public struct Material2
{
    public float brightness;
    // Color map
    public float3 col;
    public int2 colTexLoc;
    public int2 colTexDims;
    // Specular color map
    public float3 specCol;
    public int2 specColTexLoc;
    public int2 specColTexDims;
    // Smoothness (r), Bump map (g)
    public float smoothness;
    public float bump;
    public int2 compressedTexLoc;
    public int2 compressedTexDims;
    // Normals map
    public int2 normalsTexLoc;
    public int2 normalsTexDims;
};