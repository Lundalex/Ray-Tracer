using UnityEngine;

public class ShaderHelper : MonoBehaviour
{
    public Main m;
    public void SetMaterialBuffer(ComputeBuffer materialBuffer)
    {
        m.rtShader.SetBuffer(0, "Materials", materialBuffer);
    }
    public void SetSphereBuffer(ComputeBuffer sphereBuffer)
    {
        m.rtShader.SetBuffer(0, "Spheres", sphereBuffer);
        m.rtShader.SetInt("SpheresNum", m.Spheres.Length);
    }
    public void SetTriBuffer(ComputeBuffer triBuffer)
    {
        m.rtShader.SetBuffer(0, "Tris", triBuffer);
        m.pcShader.SetBuffer(0, "Tris", triBuffer);

        m.pcShader.SetInt("NumTris", m.Tris.Length);
    }
    public void SetBoxBuffer(ComputeBuffer boxBuffer)
    {
        m.rtShader.SetBuffer(0, "Boxes", boxBuffer);
    }
}