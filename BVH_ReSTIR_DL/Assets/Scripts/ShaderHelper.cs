using Unity.Mathematics;
using UnityEngine;

public class ShaderHelper : MonoBehaviour
{
    public Main m;
    public void SetMaterialBuffer(ComputeBuffer materialBuffer)
    {
        m.rtShader.SetBuffer(0, "Materials", materialBuffer);
        m.rtShader.SetBuffer(4, "Materials", materialBuffer);
    }
    public void SetTriBuffer(ComputeBuffer triBuffer)
    {
        m.rtShader.SetBuffer(0, "Tris", triBuffer);
        m.rtShader.SetBuffer(4, "Tris", triBuffer);
        m.pcShader.SetBuffer(0, "Tris", triBuffer);

        m.pcShader.SetInt("TrisNum", m.Tris.Length);
        m.rtShader.SetInt("TrisNum", m.Tris.Length);
    }
    public void SetBVBuffer(ComputeBuffer bvBuffer)
    {
        m.rtShader.SetBuffer(0, "BVs", bvBuffer);
        m.rtShader.SetBuffer(4, "BVs", bvBuffer);
        m.rtShader.SetInt("BVsNum", m.BVs.Length);
    }
    public void SetSceneObjectDataBuffer(ComputeBuffer sceneObjectDataBuffer)
    {
        m.rtShader.SetBuffer(0, "SceneObjects", sceneObjectDataBuffer);
        m.rtShader.SetBuffer(4, "SceneObjects", sceneObjectDataBuffer);
        m.pcShader.SetBuffer(0, "SceneObjects", sceneObjectDataBuffer);
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
        m.rtShader.SetBuffer(2, "Candidates", candidateBuffer);
        m.rtShader.SetBuffer(3, "Candidates", candidateBuffer);
        m.rtShader.SetBuffer(4, "Candidates", candidateBuffer);
    }

    public void SetCandidateReuseBuffer(ComputeBuffer candidateReuseBuffer)
    {
        m.rtShader.SetBuffer(1, "CandidatesB", candidateReuseBuffer);
        m.rtShader.SetBuffer(2, "CandidatesB", candidateReuseBuffer);
    }

    public void SetTemporalFrameBuffer(ComputeBuffer temporalFrameBuffer)
    {
        m.rtShader.SetBuffer(3, "TemporalFrameBuffer", temporalFrameBuffer);
    }

    public void SetHitInfoBuffer(ComputeBuffer hitInfoBuffer)
    {
        m.rtShader.SetBuffer(0, "HitInfos", hitInfoBuffer);
        m.rtShader.SetBuffer(4, "HitInfos", hitInfoBuffer);
    }
}