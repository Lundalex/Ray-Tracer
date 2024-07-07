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
    [Range(0.0f, 2.0f)] public float DefocusStrength;
    public float focalPlaneFactor; // focalPlaneFactor must be positive
    public int FrameCount;

    [Header("Scene objects / Material2s")]
    public float4[] SpheresInput; // xyz: pos; w: radii
    public float4[] MatTypesInput1; // xyz: emissionColor; w: emissionStrength
    public float4[] MatTypesInput2; // x: smoothness

    [Header("References")]
    public ComputeShader rtShader;
    public ShaderHelper shaderHelper;
    public MeshHelper meshHelper;

    // Private variables
    private RenderTexture renderTexture;
    private int RayTracerThreadSize = 16; // /32
    private Sphere[] Spheres;
    private Material2[] Material2s;
    private Box[] Boxes;
    private Tri[] Tris;
    private ComputeBuffer SphereBuffer;
    private ComputeBuffer BoxBuffer;
    private ComputeBuffer TriBuffer;
    private ComputeBuffer MaterialBuffer;

    private bool ProgramStarted = false;
    private Vector3 lastCameraPosition;
    private Quaternion lastCameraRotation;

    private void Start()
    {
        lastCameraPosition = transform.position;

        SetData(); // Set data of render object compute buffers

        UpdatePerFrame();
        UpdateSettings();

        ProgramStarted = true;
    }

    private void Update()
    {
        UpdatePerFrame();
    }

    private void LateUpdate()
    {
        if (transform.position != lastCameraPosition || transform.rotation != lastCameraRotation)
        {
            UpdateSettings();
            lastCameraPosition = transform.position;
            lastCameraRotation = transform.rotation;
        }
    }

    private void UpdatePerFrame()
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

    private void UpdateSettings()
    {
        FrameCount = 0;
        SetData();

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

        rtShader.SetFloat("defocusStrength", DefocusStrength);
        rtShader.SetFloat("focalPlaneFactor", focalPlaneFactor);
    }

    private void SetData()
    {
        ComputeHelper.Release(AllBuffers());

        // Set spheres data
        Spheres = new Sphere[Func.MaxInt(SpheresInput.Length, 1)];
        for (int i = 0; i < Spheres.Length && SpheresInput.Length != 0; i++)
        {
            Spheres[i] = new Sphere
            {
                pos = new float3(SpheresInput[i].x, SpheresInput[i].y, SpheresInput[i].z),
                radius = SpheresInput[i].w,
                materialKey = i == 0 ? 1 : 0
            };
        }
        SphereBuffer = ComputeHelper.CreateStructuredBuffer<Sphere>(Spheres);
        rtShader.SetBuffer(0, "Spheres", SphereBuffer);

        // Set Material2 types data
        Material2s = new Material2[Func.MaxInt(MatTypesInput1.Length, 1)];
        for (int i = 0; i < Material2s.Length && MatTypesInput1.Length != 0 && MatTypesInput1.Length == MatTypesInput2.Length; i++)
        {
            Material2s[i] = new Material2
            {
                color = new float3(MatTypesInput1[i].x, MatTypesInput1[i].y, MatTypesInput1[i].z),
                specularColor = new float3(1, 1, 1), // Specular color is currently set to white for all Material2 types
                brightness = MatTypesInput1[i].w,
                smoothness = MatTypesInput2[i].x
            };
        }
        MaterialBuffer = ComputeHelper.CreateStructuredBuffer<Material2>(Material2s);
        rtShader.SetBuffer(0, "Materials", MaterialBuffer);

        (Boxes, Tris) = meshHelper.ConstructBVHFromObj(0, 1f);

        TriBuffer = ComputeHelper.CreateStructuredBuffer<Tri>(Tris);
        rtShader.SetBuffer(0, "Tris", TriBuffer);

        BoxBuffer = ComputeHelper.CreateStructuredBuffer<Box>(Boxes);
        rtShader.SetBuffer(0, "Boxes", BoxBuffer);
    }

    private void RunRenderShader()
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
        int2 threadGroupNums = Utils.GetThreadGroupsNum(Resolution, RayTracerThreadSize);
        rtShader.Dispatch(0, threadGroupNums.x, threadGroupNums.y, 1);
    }

    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        RunRenderShader();

        Graphics.Blit(renderTexture, dest);
    }

    private ComputeBuffer[] AllBuffers() => new ComputeBuffer[] { SphereBuffer, BoxBuffer, TriBuffer, MaterialBuffer };

    private void OnDestroy()
    {
        ComputeHelper.Release(AllBuffers());
    }
}