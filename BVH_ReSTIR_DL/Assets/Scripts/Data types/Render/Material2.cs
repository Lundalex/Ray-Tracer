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

    // Roughness
    public float roughness;
    public int2 roughnessTexLoc;
    public int2 roughnessTexDims;

    // Metallicity
    public float metallicity;
    public int2 metallicityTexLoc;
    public int2 metallicityTexDims;
    
    // Normals map
    public int2 normalsTexLoc;
    public int2 normalsTexDims;
};