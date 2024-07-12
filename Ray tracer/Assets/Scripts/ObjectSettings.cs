using UnityEngine;
using Unity.Mathematics;

// Import utils from Resources.cs
using Resources;
// Usage: Utils.(functionName)()

// All position and rotation settings for scene objects
public class ObjectSettings : MonoBehaviour
{
    public float3 testPos;
    public float3 testRot;
    private void Start()
    {
        
    }

    public Matrix4x4 GetTestWorldToLocalMatrix()
    {
        Matrix4x4 testWorldToLocalMatrix = Utils.CreateWorldToLocalMatrix(testPos, testRot);
        return testWorldToLocalMatrix;
    }
}
