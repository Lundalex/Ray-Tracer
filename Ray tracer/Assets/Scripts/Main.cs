using UnityEngine;
using Unity.Mathematics;

// Import utils from Resources.cs
using Resources;
// Usage: Utils.(functionName)()

public class Main : MonoBehaviour
{
    [Header("Render settings")]
    public float fieldOfView;
    public int2 Resolution;

    [Header("RT settings")]
    public int MaxBounceCount;
    public int RaysPerPixel;
    public float ScatterProbability;
    public int FrameCount = 0;

    [Header("Scene objects / materials")]
    public float4[] SpheresInput; // xyz: pos; w: radii
    public float4[] MatTypesInput1; // xyz: emissionColor; w: emissionStrength
    public float4[] MatTypesInput2; // x: smoothness

    [Header("References")]
    public ComputeShader rtShader;
    public ShaderHelper shaderHelper;

    // Private variables
    private RenderTexture renderTexture;
    private int RayTracerThreadSize = 32; // /32
    private Sphere[] Spheres;
    private MaterialType[] MaterialTypes;
    public ComputeBuffer SphereBuffer;
    public ComputeBuffer MaterialTypesBuffer;

    private bool ProgramStarted = false;

    void Start()
    {
        SphereBuffer = new ComputeBuffer(SpheresInput.Length, sizeof(float) * 4 + sizeof(int) * 1);
        MaterialTypesBuffer = new ComputeBuffer(MatTypesInput1.Length, sizeof(float) * 5 + sizeof(int) * 0);

        UpdateSetData();

        shaderHelper.SetRTShaderBuffers(rtShader);

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
        rtShader.SetInt("FrameRand", FrameRand);
        rtShader.SetInt("FrameCount", FrameCount++);

        // Camera position
        float3 worldSpaceCameraPos = transform.position;
        float[] worldSpaceCameraPosArray = new float[] { worldSpaceCameraPos.x, worldSpaceCameraPos.y, worldSpaceCameraPos.z };
        rtShader.SetFloats("WorldSpaceCameraPos", worldSpaceCameraPosArray);

        // Camera orientation
        float3 cameraRot = transform.rotation.eulerAngles;
        float[] cameraRotArray = new float[] { cameraRot.x, cameraRot.y, cameraRot.z };
        rtShader.SetFloats("CameraRotation", Func.DegreesToRadians(cameraRotArray));
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

        shaderHelper.UpdateRTShaderVariables(rtShader);

        rtShader.SetInt("SpheresNum", Spheres.Length);

        int[] resolutionArray = new int[] { Resolution.x, Resolution.y };
        rtShader.SetInts("Resolution", resolutionArray);

        rtShader.SetInt("MaxBounceCount", MaxBounceCount);
        rtShader.SetInt("RaysPerPixel", RaysPerPixel);
        rtShader.SetFloat("ScatterProbability", ScatterProbability);

        float aspectRatio = Resolution.x / Resolution.y;
        float fieldOfViewRad = fieldOfView * Mathf.PI / 180;
        float viewSpaceHeight = Mathf.Tan(fieldOfViewRad * 0.5f);
        float viewSpaceWidth = aspectRatio * viewSpaceHeight;
        rtShader.SetFloat("viewSpaceWidth", viewSpaceWidth);
        rtShader.SetFloat("viewSpaceHeight", viewSpaceHeight);
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
        MaterialTypes = new MaterialType[MatTypesInput1.Length];
        for (int i = 0; i < MaterialTypes.Length; i++)
        {
            MaterialTypes[i] = new MaterialType
            {
                color = new float3(MatTypesInput1[i].x, MatTypesInput1[i].y, MatTypesInput1[i].z),
                brightness = MatTypesInput1[i].w,
                smoothness = MatTypesInput2[i].x
            };
        }
        MaterialTypesBuffer.SetData(MaterialTypes);
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

        rtShader.SetTexture(0, "Result", renderTexture);
        int2 threadGroupNums = Utils.GetThreadGroupsNumsXY(Resolution, RayTracerThreadSize);
        rtShader.Dispatch(0, threadGroupNums.x, threadGroupNums.y, 1);
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