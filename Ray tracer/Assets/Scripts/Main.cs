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
    [Range(0.0f, 1.0f)] public float ScatterProbability;
    [Range(0.0f, 2.0f)] public float defocusStrength;
    public float focalPlaneFactor; // focalPlaneFactor must be positive
    public int FrameCount;

    [Header("Scene objects / Material2s")]
    public float4[] SpheresInput; // xyz: pos; w: radii
    public float4[] MatTypesInput1; // xyz: emissionColor; w: emissionStrength
    public float4[] MatTypesInput2; // x: smoothness

    [Header("References")]
    public ComputeShader rtShader;
    public ShaderHelper shaderHelper;

    // Private variables
    private RenderTexture renderTexture;
    private int RayTracerThreadSize = 16; // /32
    private Sphere[] Spheres;
    private Material2[] Material2s;
    public ComputeBuffer SphereBuffer;
    public ComputeBuffer MaterialsBuffer;

    private bool ProgramStarted = false;
    private Vector3 lastCameraPosition;
    private Quaternion lastCameraRotation;

    void Start()
    {
        lastCameraPosition = transform.position;

        SphereBuffer = new ComputeBuffer(SpheresInput.Length, sizeof(float) * 4 + sizeof(int) * 1);
        MaterialsBuffer = new ComputeBuffer(MatTypesInput1.Length, sizeof(float) * 8 + sizeof(int) * 0);

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

    void LateUpdate()
    {
        if (transform.position != lastCameraPosition || transform.rotation != lastCameraRotation)
        {
            UpdateSettings();
            lastCameraPosition = transform.position;
            lastCameraRotation = transform.rotation;
        }
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
            UpdateSettings();
        }
    }

    void UpdateSettings()
    {
        FrameCount = 0;
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

        rtShader.SetFloat("defocusStrength", defocusStrength);
        rtShader.SetFloat("focalPlaneFactor", focalPlaneFactor);
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
                materialFlag = i == 0 ? 1 : 0
            };
        }
        SphereBuffer.SetData(Spheres);

        // Set Material2 types data
        Material2s = new Material2[MatTypesInput1.Length];
        for (int i = 0; i < Material2s.Length; i++)
        {
            Material2s[i] = new Material2
            {
                color = new float3(MatTypesInput1[i].x, MatTypesInput1[i].y, MatTypesInput1[i].z),
                specularColor = new float3(1, 1, 1), // Specular color is currently set to white for all Material2 types
                brightness = MatTypesInput1[i].w,
                smoothness = MatTypesInput2[i].x
            };
        }
        MaterialsBuffer.SetData(Material2s);
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
        MaterialsBuffer?.Release();
    }
}