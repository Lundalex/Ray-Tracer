using UnityEngine;
using Unity.Mathematics;
using System;

public class Main : MonoBehaviour
{
    [Header("Render settings")]
    public float fieldOfView;
    public int2 Resolution;

    [Header("RT settings")]
    [Range(1, 10)] public int MaxBounceCount;

    [Header("Scene objects")]
    public float4[] SpheresInput; // xyz: pos; w: radii

    [Header("References")]
    public ComputeShader rayTracerShader;

    // Private variables
    private RenderTexture renderTexture;
    private int RayTracerThreadSize = 32; // /32
    private Sphere[] Spheres;
    private ComputeBuffer SphereBuffer;
    private bool ProgramStarted = false;

    public struct Sphere
    {
        public float3 position;
        public float radius;
    };

    void Start()
    {
        Spheres = new Sphere[SpheresInput.Length];
        SphereBuffer = new ComputeBuffer(Spheres.Length, sizeof(float) * 4);

        UpdateSphereData();

        rayTracerShader.SetBuffer(0, "Spheres", SphereBuffer);

        UpdatePerFrame();
        UpdateSettings();

        ProgramStarted = true;
    }

    void Update()
    {
        UpdatePerFrame();
    }

    void UpdatePerFrame()
    {
        // Camera position
        float3 worldSpaceCameraPos = transform.position;
        worldSpaceCameraPos.z = -worldSpaceCameraPos.z; // Invert z
        float[] worldSpaceCameraPosArray = new float[] { worldSpaceCameraPos.x, worldSpaceCameraPos.y, worldSpaceCameraPos.z };
        rayTracerShader.SetFloats("WorldSpaceCameraPos", worldSpaceCameraPosArray);

        // Camera orientation
        float3 cameraRot = transform.rotation.eulerAngles;
        float[] cameraRotArray = new float[] { cameraRot.x, cameraRot.y, cameraRot.z };
        rayTracerShader.SetFloats("CameraRotation", DegreesToRadians(cameraRotArray));
    }

    private void OnValidate()
    {
        if (ProgramStarted)
        {
            UpdateSettings();
        }
    }

    void UpdateSettings()
    {
        UpdateSphereData();

        rayTracerShader.SetInt("SpheresNum", Spheres.Length);

        int[] resolutionArray = new int[] { Resolution.x, Resolution.y };
        rayTracerShader.SetInts("Resolution", resolutionArray);

        rayTracerShader.SetInts("MaxBounceCount", MaxBounceCount);

        float aspectRatio = Resolution.x / Resolution.y;
        float fieldOfViewRad = fieldOfView * Mathf.PI / 180;
        float viewSpaceHeight = Mathf.Tan(fieldOfViewRad * 0.5f);
        float viewSpaceWidth = aspectRatio * viewSpaceHeight;
        rayTracerShader.SetFloat("viewSpaceWidth", viewSpaceWidth);
        rayTracerShader.SetFloat("viewSpaceHeight", viewSpaceHeight);
    }

    void UpdateSphereData()
    {
        for (int i = 0; i < Spheres.Length; i++)
        {
            Spheres[i] = new Sphere
            {
                position = new float3(SpheresInput[i].x, SpheresInput[i].y, SpheresInput[i].z),
                radius = SpheresInput[i].w
            };
        }
        SphereBuffer.SetData(Spheres);
    }

    float[] DegreesToRadians(float[] degreesArray)
    {
        float[] radiansArray = new float[degreesArray.Length];
        for (int i = 0; i < degreesArray.Length; i++)
        {
            radiansArray[i] = degreesArray[i] * Mathf.Deg2Rad;
        }
        return radiansArray;
    }

    int2 GetThreadGroupsNumsXY(int2 threadsNum, int threadSize)
    {
        int threadGroupsNumX = (int)Math.Ceiling((float)threadsNum.x / threadSize);
        int threadGroupsNumY = (int)Math.Ceiling((float)threadsNum.y / threadSize);
        return new(threadGroupsNumX, threadGroupsNumY);
    }

    void RunRenderShader()
    {
        if (renderTexture == null)
        {
            renderTexture = new RenderTexture(Resolution.x, Resolution.y, 24)
            {
                enableRandomWrite = true
            };
            renderTexture.Create();
        }

        rayTracerShader.SetTexture(0, "Result", renderTexture);
        int2 threadGroupNums = GetThreadGroupsNumsXY(Resolution, RayTracerThreadSize);
        rayTracerShader.Dispatch(0, threadGroupNums.x, threadGroupNums.y, 1);
    }

    public void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        RunRenderShader();

        Graphics.Blit(renderTexture, dest);
    }

    void OnDestroy()
    {
        SphereBuffer?.Release();
    }
}
