using UnityEngine;

public class ShaderHelper : MonoBehaviour
{
    public Main m;
    public void SetRTShaderBuffers(ComputeShader rtShader)
    {
        rtShader.SetBuffer(0, "Spheres", m.SphereBuffer);
        rtShader.SetBuffer(0, "MaterialTypes", m.MaterialTypesBuffer);
    }

    public void UpdateRTShaderVariables(ComputeShader rtShader)
    {

    }
}