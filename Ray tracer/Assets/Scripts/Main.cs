using UnityEngine;
using Unity.Mathematics;

// Import utils from Resources.cs
using Resources;
// Usage: Utils.(functionName)()

public class Main : MonoBehaviour
{
    public bool DoUpdateSettings;
    public bool RenderSpheres;
    public bool RenderBVs;
    public bool RenderTriObjects;
    public bool BVHEnable;
    [Header("Debug settings")]
    public bool DebugViewEnable;
    public int DebugMaxTriChecks;
    public int DebugMaxBVChecks;
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
    public ComputeShader pcShader;
    public ShaderHelper shaderHelper;
    public MeshHelper meshHelper;
    public ObjectSettings objectSettings;

    // Private variables
    private RenderTexture rtResultTexture;
    private RenderTexture debugOverlayTexture;
    private int RayTracerThreadSize = 8; // /32
    private int PreCalcThreadSize = 16; // /32
    public Sphere[] Spheres;
    public Material2[] Material2s;
    public Tri[] Tris;
    public SceneObjectData[] SceneObjectDatas;
    public BoundingVolume[] BVs;
    private ComputeBuffer SphereBuffer;
    private ComputeBuffer BVBuffer;
    private ComputeBuffer TriBuffer;
    private ComputeBuffer SceneObjectDataBuffer;
    private ComputeBuffer MaterialBuffer;

    private bool ProgramStarted = false;
    private Vector3 lastCameraPosition;
    private Quaternion lastCameraRotation;

    private void Start()
    {
        lastCameraPosition = transform.position;
        lastCameraRotation = transform.rotation;

        UpdatePerFrame();
        UpdateSettings();

        ProgramStarted = true;
    }

    private void Update()
    {
        UpdatePerFrame();

        if (DoUpdateSettings) { DoUpdateSettings = false; UpdateSettings(); }
    }

    private void LateUpdate()
    {
        if (transform.position != lastCameraPosition || transform.rotation != lastCameraRotation)
        {
            DoUpdateSettings = true;
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

        SetCameraOrientationAndTransform();
    }

    public void SetCameraOrientationAndTransform()
    {
        // Camera position
        float3 worldSpaceCameraPos = transform.position;
        rtShader.SetVector("WorldSpaceCameraPos", new Vector3(worldSpaceCameraPos.x, worldSpaceCameraPos.y, worldSpaceCameraPos.z));

        // Camera orientation
        float3 cameraRot = transform.rotation.eulerAngles * Mathf.Deg2Rad;

        float temp = cameraRot.x;
        cameraRot.x = cameraRot.y;
        cameraRot.y = -temp;
        cameraRot.z = -cameraRot.z;

        // Camera transform matrix
        float cosX = Mathf.Cos(cameraRot.x);
        float sinX = Mathf.Sin(cameraRot.x);
        float cosY = Mathf.Cos(cameraRot.y);
        float sinY = Mathf.Sin(cameraRot.y);
        float cosZ = Mathf.Cos(cameraRot.z);
        float sinZ = Mathf.Sin(cameraRot.z);
        // Combined camera transform
        // Unity only allows setting 4x4 matrices (will get converted to 3x3 automatically in shader)
        float4x4 CameraTransform = new float4x4(
            cosY * cosZ,                             cosY * sinZ,                           -sinY, 0.0f,
            sinX * sinY * cosZ - cosX * sinZ,   sinX * sinY * sinZ + cosX * cosZ,  sinX * cosY, 0.0f,
            cosX * sinY * cosZ + sinX * sinZ,   cosX * sinY * sinZ - sinX * cosZ,  cosX * cosY, 0.0f,
            0.0f, 0.0f, 0.0f, 0.0f
        );

        rtShader.SetMatrix("CameraTransform", CameraTransform);
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

        int[] resolutionArray = new int[] { Resolution.x, Resolution.y };
        rtShader.SetInts("Resolution", resolutionArray);
        int[] IterationSettings = new int[] { RenderSpheres ? 1 : 0, RenderBVs ? 1 : 0, (RenderTriObjects && BVHEnable) ? 1 : 0, (RenderTriObjects && !BVHEnable) ? 1 : 0 };
        rtShader.SetInts("IterationSettings", IterationSettings);

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

        // Mesh helper
        rtShader.SetInt("MaxDepthBVH", meshHelper.MaxDepthBVH);

        // Debug overlay
        int[] DebugDataMaxValues = new int[] { DebugMaxTriChecks, DebugMaxBVChecks };
        rtShader.SetInts("DebugDataMaxValues", DebugDataMaxValues);
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
        shaderHelper.SetSphereBuffer(SphereBuffer);

        // Set Material2s data
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
        shaderHelper.SetMaterialBuffer(MaterialBuffer);

        // Construct BVHEnable(s)
        (BVs, Tris, SceneObjectDatas) = meshHelper.CreateSceneObjects();
        
        // Set BVHEnable data
        BVBuffer = ComputeHelper.CreateStructuredBuffer<BoundingVolume>(BVs);
        shaderHelper.SetBVBuffer(BVBuffer);
        rtShader.SetInt("MaxDepthBVH", meshHelper.MaxDepthBVH);

        // Set Tris data
        TriBuffer = ComputeHelper.CreateStructuredBuffer<Tri>(Tris);
        shaderHelper.SetTriBuffer(TriBuffer);
        RunPreCalcShader();

        // Set SceneObjects data
        SceneObjectDataBuffer = ComputeHelper.CreateStructuredBuffer<SceneObjectData>(SceneObjectDatas);
        shaderHelper.SetSceneObjectDataBuffer(SceneObjectDataBuffer);

    }

    private void RunPreCalcShader()
    {
        ComputeHelper.DispatchKernel(pcShader, "CalcTriNormals", Tris.Length, PreCalcThreadSize);
    }

    private void RunRenderShader()
    {
        // Ray tracer result texture
        if (rtResultTexture == null)
        {
            rtResultTexture = new RenderTexture(Resolution.x, Resolution.y, 24)
            {
                enableRandomWrite = true
            };
            rtResultTexture.Create();
            rtShader.SetTexture(0, "Result", rtResultTexture);
        }
        // Debug overlay texture
        if (debugOverlayTexture == null)
        {
            debugOverlayTexture = new RenderTexture(Resolution.x, Resolution.y, 24)
            {
                enableRandomWrite = true
            };
            debugOverlayTexture.Create();
            rtShader.SetTexture(0, "DebugOverlay", debugOverlayTexture);
        }

        ComputeHelper.DispatchKernel(rtShader, "TraceRays", Resolution, RayTracerThreadSize);
    }

    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        RunRenderShader();

        Graphics.Blit(DebugViewEnable ? debugOverlayTexture : rtResultTexture, dest);
    }

    private ComputeBuffer[] AllBuffers() => new ComputeBuffer[] { SphereBuffer, BVBuffer, TriBuffer, SceneObjectDataBuffer, MaterialBuffer };

    private void OnDestroy()
    {
        ComputeHelper.Release(AllBuffers());
    }
}