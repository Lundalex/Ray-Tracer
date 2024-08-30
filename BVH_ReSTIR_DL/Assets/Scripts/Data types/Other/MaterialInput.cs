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

    // Smoothness
    public float smoothness;
    public Texture2D smoothnessTex;

    // Bump
    public float bump;
    public Texture2D bumpTex;
};