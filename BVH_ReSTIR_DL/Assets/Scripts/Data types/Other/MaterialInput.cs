using Unity.Mathematics;
using UnityEngine;
[System.Serializable]
public struct MaterialInput
{
    public float brightness;

    // Col
    public float3 col;
    public Texture2D colTex;

    // Normals
    public Texture2D normalsTex;

    // Roughness
    public float roughness;
    public Texture2D roughnessTex;

    // Metallicity
    public float metallicity;
    public Texture2D metallicityTex;

    // Bump
    public float bump;
    public Texture2D bumpTex;
};