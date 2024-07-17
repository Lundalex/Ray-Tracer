using Unity.Mathematics;
using UnityEngine;

public class ShaderHelper : MonoBehaviour
{
    public Main m;
    public void SetMaterialBuffer(ComputeBuffer materialBuffer)
    {
        m.rtShader.SetBuffer(0, "Materials", materialBuffer);
        m.rtShader.SetBuffer(1, "Materials", materialBuffer);
    }
    public void SetSphereBuffer(ComputeBuffer sphereBuffer)
    {
        m.rtShader.SetBuffer(0, "Spheres", sphereBuffer);
        m.rtShader.SetBuffer(1, "Spheres", sphereBuffer);
        m.rtShader.SetInt("SpheresNum", m.Spheres.Length);
    }
    public void SetTriBuffer(ComputeBuffer triBuffer)
    {
        m.rtShader.SetBuffer(0, "Tris", triBuffer);
        m.rtShader.SetBuffer(1, "Tris", triBuffer);
        m.pcShader.SetBuffer(0, "Tris", triBuffer);

        m.pcShader.SetInt("TrisNum", m.Tris.Length);
        m.rtShader.SetInt("TrisNum", m.Tris.Length);
    }
    public void SetBVBuffer(ComputeBuffer bvBuffer)
    {
        m.rtShader.SetBuffer(0, "BVs", bvBuffer);
        m.rtShader.SetBuffer(1, "BVs", bvBuffer);
        m.rtShader.SetInt("BVsNum", m.BVs.Length);
    }
    public void SetSceneObjectDataBuffer(ComputeBuffer sceneObjectDataBuffer)
    {
        m.rtShader.SetBuffer(0, "SceneObjects", sceneObjectDataBuffer);
        m.rtShader.SetBuffer(1, "SceneObjects", sceneObjectDataBuffer);
        m.rtShader.SetInt("SceneObjectsNum", m.SceneObjectDatas.Length);
    }

    public void SetLightObjectBuffer(ComputeBuffer lightObjectBuffer)
    {
        m.rtShader.SetBuffer(0, "LightObjects", lightObjectBuffer);
    }

    // --- ReStir ---

    public void SetCandidateBuffer(ComputeBuffer candidateBuffer)
    {
        m.rtShader.SetBuffer(0, "Candidates", candidateBuffer);
        m.rtShader.SetBuffer(1, "Candidates", candidateBuffer);
    }

    public void SetHitInfoBuffer(ComputeBuffer hitInfoBuffer)
    {
        m.rtShader.SetBuffer(0, "HitInfos", hitInfoBuffer);
        m.rtShader.SetBuffer(1, "HitInfos", hitInfoBuffer);
    }

    // // ReStir
    // public void SetDirCandidateBuffer(ComputeBuffer candidateDirectionBuffer)
    // {
    //     m.rtShader.SetBuffer(0, "CandidateDirections", candidateDirectionBuffer);
    // }
}