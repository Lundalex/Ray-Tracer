using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
[System.Serializable]
public class KeyedInt
{
    public string Key;
    public int Value;
}
[System.Serializable]
public class KeyedFloat
{
    public string Key;
    public float Value;
}
[System.Serializable]
public class MultiArrayContainer
{
    public Triangle[] loadedTriangles;
    public SceneObjectData[] sceneObjectDatas;
    public LightObject[] lightObjects;
    public int[] loadedMeshesLookup;
    public int2[] loadedComponentDatas;
    public List<MeshData> loadedMeshes;
    public RenderBV[] loadedBVs;
    public Vertex[] loadedVertices;
    public KeyedInt[] integers;
    public KeyedFloat[] floats;
    public RenderBV[] renderBVs;
    public RenderTriangle[] renderTriangles;

    // Function to fetch integer value by key
    public int GetIntByKey(string key)
    {
        return integers.FirstOrDefault(x => x.Key == key)?.Value ?? throw new KeyNotFoundException($"Key '{key}' not found in integers.");
    }

    // Function to fetch float value by key
    public float GetFloatByKey(string key)
    {
        return floats.FirstOrDefault(x => x.Key == key)?.Value ?? throw new KeyNotFoundException($"Key '{key}' not found in floats.");
    }
}