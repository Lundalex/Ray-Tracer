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

        m.pcShader.SetInt("TrisNum", m.Tris.Length);
        m.rtShader.SetInt("TrisNum", m.Tris.Length);
    }
    public void SetBVBuffer(ComputeBuffer bvBuffer)
    {
        m.rtShader.SetBuffer(0, "BVs", bvBuffer);
        m.rtShader.SetInt("BVsNum", m.BVs.Length);
    }
    public void SetTriObjectBuffer(ComputeBuffer triObjectBuffer)
    {
        m.rtShader.SetBuffer(0, "TriObjects", triObjectBuffer);
        m.rtShader.SetInt("TriObjectsNum", m.TriObjects.Length);
    }
    public void SetTestMatrix(Matrix4x4 testWorldToLocalMatrix, Matrix4x4 testLocalToWorldMatrix)
    {
        m.rtShader.SetMatrix("TestWorldToLocalMatrix", testWorldToLocalMatrix);
        m.rtShader.SetMatrix("TestWorldToLocalMatrix", testLocalToWorldMatrix);
    }
}