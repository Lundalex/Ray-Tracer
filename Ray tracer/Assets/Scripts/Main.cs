using UnityEngine;
using Unity.Mathematics;
using System;

public class Main : MonoBehaviour
{
    [Header("Render settings")]
    public float fieldOfView;
    public int2 Resolution;

    [Header("RT settings")]
    public int MaxBounceCount;
    public int RaysPerPixel;
    public int FrameCount = 0;

    [Header("Scene objects / materials")]
    public float4[] SpheresInput; // xyz: pos; w: radii
    public float4[] MatTypesInput; // xyz: emissionColor; w: emissionStrength

    [Header("References")]
    public ComputeShader rayTracerShader;

    // Private variables
    private RenderTexture renderTexture;
    private int RayTracerThreadSize = 32; // /32
    private Sphere[] Spheres;
    private MaterialType[] MaterialTypes;
    private ComputeBuffer SphereBuffer;
    private ComputeBuffer MaterialTypesBuffer;

    private bool ProgramStarted = false;

    public struct Sphere
    {
        public float3 position;
        public float radius;
        public int materialTypeFlag;
    };
    struct MaterialType
    {
        public float3 color;
        public float brightness;
    };

    void Start()
    {
        SphereBuffer = new ComputeBuffer(SpheresInput.Length, sizeof(float) * 4 + sizeof(int) * 1);
        MaterialTypesBuffer = new ComputeBuffer(MatTypesInput.Length, sizeof(float) * 4 + sizeof(int) * 0);

        UpdateSetData();

        rayTracerShader.SetBuffer(0, "Spheres", SphereBuffer);
        rayTracerShader.SetBuffer(0, "MaterialTypes", MaterialTypesBuffer);

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
        // Frame set variables
        int FrameRand = UnityEngine.Random.Range(0, 999999);
        rayTracerShader.SetInt("FrameRand", FrameRand);
        rayTracerShader.SetInt("FrameCount", FrameCount++);

        // Camera position
        float3 worldSpaceCameraPos = transform.position;
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
            FrameCount = 0;
            UpdateSettings();
        }
    }

    void UpdateSettings()
    {
        UpdateSetData();

        rayTracerShader.SetInt("SpheresNum", Spheres.Length);

        int[] resolutionArray = new int[] { Resolution.x, Resolution.y };
        rayTracerShader.SetInts("Resolution", resolutionArray);

        rayTracerShader.SetInt("MaxBounceCount", MaxBounceCount);
        rayTracerShader.SetInt("RaysPerPixel", RaysPerPixel);

        float aspectRatio = Resolution.x / Resolution.y;
        float fieldOfViewRad = fieldOfView * Mathf.PI / 180;
        float viewSpaceHeight = Mathf.Tan(fieldOfViewRad * 0.5f);
        float viewSpaceWidth = aspectRatio * viewSpaceHeight;
        rayTracerShader.SetFloat("viewSpaceWidth", viewSpaceWidth);
        rayTracerShader.SetFloat("viewSpaceHeight", viewSpaceHeight);
    }

    void UpdateSetData()
    {
        // Set spheres data
        Spheres = new Sphere[SpheresInput.Length];
        for (int i = 0; i < Spheres.Length; i++)
        {
            Spheres[i] = new Sphere
            {
                position = new float3(SpheresInput[i].x, SpheresInput[i].y, SpheresInput[i].z),
                radius = SpheresInput[i].w,
                materialTypeFlag = i == 0 ? 1 : 0
            };
        }
        SphereBuffer.SetData(Spheres);

        // Set material types data
        MaterialTypes = new MaterialType[MatTypesInput.Length];
        for (int i = 0; i < MaterialTypes.Length; i++)
        {
            MaterialTypes[i] = new MaterialType
            {
                color = new float3(MatTypesInput[i].x, MatTypesInput[i].y, MatTypesInput[i].z),
                brightness = MatTypesInput[i].w
            };
        }
        MaterialTypesBuffer.SetData(MaterialTypes);
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
        MaterialTypesBuffer?.Release();
    }
}
