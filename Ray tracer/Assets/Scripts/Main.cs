using UnityEngine;
using Unity.Mathematics;
using System;

// Import utils from Resources.cs
using Resources;
// Usage: Utils.(functionName)()

public class Main : MonoBehaviour
{
    [Header("Primary settings")]
    public bool RenderSpheres;
    public bool RenderBVs;
    public bool RenderTriObjects;
    public bool BVHEnable;
    public float CameraMoveSpeed;
    public float CameraPanSpeed;
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
    [Header("ReStir settings")]
    public int SceneObjectCandidatesNum;
    public int TriCandidatesNum;
    public int ChosenCandidatesNum; // A.k.a. N
    public int SceneObjectReservoirTestsNum;
    public int TriReservoirTestsNum;

    [Header("Scene objects / Material2s")]
    public float4[] SpheresInput; // xyz: pos; w: radii
    public float4[] MatTypesInput1; // xyz: emissionColor; w: emissionStrength
    public float4[] MatTypesInput2; // x: smoothness

    [Header("References")]
    public ComputeShader rtShader; // Either BVH only or BVH + ReStir
    public ComputeShader pcShader;
    public ShaderHelper shaderHelper;
    public MeshHelper meshHelper;

    // Script communication
    [NonSerialized] public bool DoUpdateSettings;
    [NonSerialized] public bool ProgramStarted = false;

    // Private variables
    private RenderTexture RTResultTexture;
    private RenderTexture DebugOverlayTexture;
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

    // ReStir
    private ComputeBuffer RayDirCandidateBuffer;
    private RenderTexture RayHitPointTexture;
    private string ReStirShaderName = "ReStir_BVH_RayTracer";
    private bool ReStirShaderEnabled;

    // Camera data record
    private Vector3 lastCameraPosition;
    private Quaternion lastCameraRotation;

    public Texture2D testTexture;

    private void Start()
    {
        lastCameraPosition = transform.position;
        lastCameraRotation = transform.rotation;

        ReStirShaderEnabled = rtShader.name == ReStirShaderName;
        if (ReStirShaderEnabled && RaysPerPixel != 1)
        {
            Debug.Log("RaysPerPixel changed from " + RaysPerPixel + " to 1 because ReStir shader is enabled!");
            RaysPerPixel = 1;
        }

        UpdatePerFrame();
        UpdateSettings();

        ProgramStarted = true;
    }

    private void Update()
    {
        UpdatePerFrame();

        if (DoUpdateSettings) { DoUpdateSettings = false; UpdateSettings(); }

        // float3[] candidates = new float3[Resolution.x * Resolution.y * RaysPerPixel * SceneObjectCandidatesNum * TriCandidatesNum];
        // RayDirCandidateBuffer.GetData(candidates);
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

        CameraMovement();
        CameraPanning();

        SetCameraOrientationAndTransform();
    }

    public void CameraMovement()
    {
        Vector3 direction = Vector3.zero;

        if (Input.GetKey(KeyCode.W)) direction += Vector3.forward;
        if (Input.GetKey(KeyCode.S)) direction -= Vector3.forward;
        if (Input.GetKey(KeyCode.A)) direction -= Vector3.right;
        if (Input.GetKey(KeyCode.D)) direction += Vector3.right;
        if (Input.GetKey(KeyCode.Space)) direction += Vector3.up;
        if (Input.GetKey(KeyCode.LeftShift)) direction -= Vector3.up;

        direction.Normalize();

        transform.position += CameraMoveSpeed * Time.deltaTime * direction;
    }

    public void CameraPanning()
    {
        if (Input.GetMouseButton(0))
        {
            float mouseY = Input.GetAxis("Mouse X") * CameraPanSpeed;
            float mouseX = Input.GetAxis("Mouse Y") * CameraPanSpeed;

            transform.rotation *= Quaternion.Euler(-mouseY, -mouseX, 0.0f);
        }
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
        if (ProgramStarted) DoUpdateSettings = true;
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

        // Debug overlay
        int[] DebugDataMaxValues = new int[] { DebugMaxTriChecks, DebugMaxBVChecks };
        rtShader.SetInts("DebugDataMaxValues", DebugDataMaxValues);

        // ReStir
        if (ReStirShaderEnabled)
        {
            rtShader.SetInt("SceneObjectCandidatesNum", SceneObjectCandidatesNum);
            rtShader.SetInt("TriCandidatesNum", TriCandidatesNum);
            rtShader.SetInt("TotCandidatesNum", SceneObjectCandidatesNum * TriCandidatesNum);
            rtShader.SetInt("ChosenCandidatesNum", ChosenCandidatesNum);
            rtShader.SetInt("SceneObjectReservoirTestsNum", SceneObjectReservoirTestsNum);
            rtShader.SetInt("TriReservoirTestsNum", TriReservoirTestsNum);

        }

        // Object Textures
        int[] texDims = new int[] { testTexture.width, testTexture.height };
        rtShader.SetInts("TexDims", texDims);
        rtShader.SetTexture(0, "TestTexture", testTexture);
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
                specularColor = new float3(1, 1, 1), // Specular color is currently set to white for all materials
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

        // Set Tris data
        TriBuffer = ComputeHelper.CreateStructuredBuffer<Tri>(Tris);
        shaderHelper.SetTriBuffer(TriBuffer);
        RunPreCalcShader();

        // Set SceneObjects data
        SceneObjectDataBuffer = ComputeHelper.CreateStructuredBuffer<SceneObjectData>(SceneObjectDatas);
        shaderHelper.SetSceneObjectDataBuffer(SceneObjectDataBuffer);

        // ReStir
        RayDirCandidateBuffer = ComputeHelper.CreateStructuredBuffer<float3>(Resolution.x * Resolution.y * RaysPerPixel * SceneObjectCandidatesNum * TriCandidatesNum);
        shaderHelper.SetDirCandidateBuffer(RayDirCandidateBuffer);
    }

    private void RunPreCalcShader()
    {
        ComputeHelper.DispatchKernel(pcShader, "CalcTriNormals", Tris.Length, PreCalcThreadSize);
    }

    private void RunRenderShader()
    {
        // Ray tracer result texture
        if (RTResultTexture == null)
        {
            RTResultTexture = new RenderTexture(Resolution.x, Resolution.y, 24)
            {
                enableRandomWrite = true
            };
            RTResultTexture.Create();
            rtShader.SetTexture(0, "Result", RTResultTexture);
        }

        // Debug overlay texture
        if (DebugOverlayTexture == null)
        {
            DebugOverlayTexture = new RenderTexture(Resolution.x, Resolution.y, 24)
            {
                enableRandomWrite = true
            };
            DebugOverlayTexture.Create();
            rtShader.SetTexture(0, "DebugOverlay", DebugOverlayTexture);
        }

        // ReStir
        if (ReStirShaderEnabled)
        {
            if (RayHitPointTexture == null)
            {
                RayHitPointTexture = new RenderTexture(Resolution.x, Resolution.y, 24)
                {
                    enableRandomWrite = true
                };
                RayHitPointTexture.Create();
                rtShader.SetTexture(0, "RayHitPoints", RayHitPointTexture);
            }
        }

        ComputeHelper.DispatchKernel(rtShader, "GenerateCandidates", Resolution, RayTracerThreadSize);
    }

    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        RunRenderShader();

        Graphics.Blit(DebugViewEnable ? RayHitPointTexture : RTResultTexture, dest); // DebugOverlayTexture 
    }

    private ComputeBuffer[] AllBuffers() => new ComputeBuffer[] { SphereBuffer, BVBuffer, TriBuffer, SceneObjectDataBuffer, MaterialBuffer, RayDirCandidateBuffer };

    private void OnDestroy()
    {
        ComputeHelper.Release(AllBuffers());
    }

    // --- Test ---

    bool WeightedRand(float weightA, float weightB)
    {
        float totalWeight = weightA + weightB;
        float randValue = UnityEngine.Random.value * totalWeight;

        return randValue < weightA;
    }
    void ReservoirSamplingTest()
    {
        float[] inputArr = new float[] { 3.2f, 9.5f, 8.4f, 5.3f, 233.0f, 7.7f, 5.1f };
        (int chosenIndex, float chosenWeight, float totWeights) reservoir = new(-1, 0, 0);

        for (int i = 0; i < inputArr.Length; i++)
        {
            float candidateWeight = inputArr[i];

            bool doReplace = WeightedRand(candidateWeight, reservoir.totWeights);
            if (doReplace)
            {
                reservoir.chosenIndex = i;
                reservoir.chosenWeight = candidateWeight;
            }
            reservoir.totWeights += candidateWeight;
        }
        Debug.Log(reservoir);
    }

    void GetPixelsFromTexture()
    {
        // Create a new Texture2D with the same dimensions and format as the RenderTexture
        Texture2D tex = new Texture2D(RayHitPointTexture.width, RayHitPointTexture.height, TextureFormat.RGBA32, false);

        // Set the RenderTexture as the active render target
        RenderTexture.active = RayHitPointTexture;

        // Read the RenderTexture into the Texture2D
        tex.ReadPixels(new Rect(0, 0, RayHitPointTexture.width, RayHitPointTexture.height), 0, 0);
        tex.Apply();

        Color[] pixels = tex.GetPixels();
    }
}