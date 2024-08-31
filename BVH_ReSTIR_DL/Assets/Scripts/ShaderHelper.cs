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
    public void SetTriBuffer(ComputeBuffer renderTriangleBuffer)
    {
        m.rtShader.SetBuffer(0, "Triangles", renderTriangleBuffer);
        m.rtShader.SetBuffer(4, "Triangles", renderTriangleBuffer);
        m.pcShader.SetBuffer(0, "Triangles", renderTriangleBuffer);

        m.pcShader.SetInt("TrianglesNum", m.RenderTriangles.Length);
        m.rtShader.SetInt("TrianglesNum", m.RenderTriangles.Length);
    }
    public void SetVertexBuffer(ComputeBuffer vertexBuffer)
    {
        m.rtShader.SetBuffer(0, "Vertices", vertexBuffer);
        m.rtShader.SetBuffer(4, "Vertices", vertexBuffer);
        m.pcShader.SetBuffer(0, "Vertices", vertexBuffer);
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
        m.rtShader.SetBuffer(4, "TemporalFrameBuffer", temporalFrameBuffer);
    }

    public void SetHitInfoBuffer(ComputeBuffer hitInfoBuffer)
    {
        m.rtShader.SetBuffer(0, "HitInfos", hitInfoBuffer);
        m.rtShader.SetBuffer(4, "HitInfos", hitInfoBuffer);
    }
}