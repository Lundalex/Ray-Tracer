using UnityEngine;
using Unity.Mathematics;
using System;
 
// Import utils from Resources.cs
using Resources;
// Usage: Utils.(functionName)()

public class Main : MonoBehaviour
{
    [Header("Camera interaction settings")]
    public float CameraMoveSpeed;
    public float CameraPanSpeed;
    [Header("Debug settings")]
    public RenderTargetSelect renderTarget;
    public int DebugMaxTriChecks;
    public int DebugMaxBVChecks;
    [Header("Render settings")]
    public float fieldOfView;
    public int2 Resolution;
 
    [Header("Ray tracer settings")]
    public int MaxBounceCount;
    public int RaysPerPixel;
    [Range(0.0f, 1.0f)] public float ScatterProbability;
    [Range(0.0f, 2.0f)] public float DefocusStrength;
    public float FocalPlaneFactor; // FocalPlaneFactor must be positive
    public int FrameCount;
    [Header("ReStir settings")]
    public int SceneObjectReservoirTestsNum;
    public int TriReservoirTestsNum;
    public int CandidateReservoirTestsNum;
    [Range(0, 5)] public int SpatialReuseIterations;
    public int TemporalCandidatesNum;
    public int SpatialReuseBlur;
    public float TemporalPrecisionThreshold;
    [Range(0.0f, 1.0f)] public float TemporalReuseWeight;
    public bool temp;
    public float PixelMovementThreshold;
    public float SpatialHitPointDiffThreshold;
    public float SpatialNormalsAngleThreshold;
    public bool DoVisibilityReuse;
 
    [Header("Material settings")]
    public float4[] MatTypesInput1; // xyz: emissionColor; w: emissionStrength
    public float4[] MatTypesInput2; // x: smoothness
 
    [Header("References")]
    public ComputeShader rtShader; // BVH + ReStir (direct lighting)
    public ComputeShader ppShader;
    public ComputeShader pcShader;
    public ShaderHelper shaderHelper;
    public MeshHelper meshHelper;
    public Texture2D EnvironmentMapTexture;
 
    // Script communication
    [NonSerialized] public bool DoUpdateSettings;
    [NonSerialized] public bool DoResetBufferData;
    [NonSerialized] public bool ProgramStarted = false;
 
    // Private variables
    private RenderTexture RTResultTexture;
    private RenderTexture AccumulatedResultTexture;
    private RenderTexture DebugOverlayTexture;
    private int RayTracerThreadSize = 8; // /32
    private int PostProcesserThreadSize = 8; // /32
    private int PreCalcThreadSize = 256;
    public Material2[] Material2s;
    public Tri[] Tris;
    public SceneObjectData[] SceneObjectDatas;
    public LightObject[] LightObjects;
    public BoundingVolume[] BVs;
    private ComputeBuffer BVBuffer;
    private ComputeBuffer TriBuffer;
    private ComputeBuffer SceneObjectDataBuffer;
    private ComputeBuffer LightObjectBuffer;
    private ComputeBuffer MaterialBuffer;
 
    // ReStir
    private ComputeBuffer CandidateBuffer;
    private ComputeBuffer CandidateReuseBuffer;
    private ComputeBuffer TemporalFrameBuffer;
    private ComputeBuffer HitInfoBuffer;
    private RenderTexture RayHitPointATexture;
    private RenderTexture RayHitPointBTexture;
    private RenderTexture DepthBufferTexture;
    private RenderTexture NormalsBufferTexture;
    private Texture2D TextureAtlas;
    private Rect[] atlasRects;
 
    // Camera data record
    private Vector3 lastCameraPosition;
    private Quaternion lastCameraRotation;
 
    // Script-specific variables
    private bool ProgramPaused = false;
    private bool FrameStep = false;
    private bool RenderThisFrame = true;
    private Vector3 lastWorldSpaceCameraPos;
    private Matrix4x4 lastCameraTransform;
 
    private void Start()
    {
        lastCameraPosition = transform.position;
        lastCameraRotation = transform.rotation;
 
        if (RaysPerPixel != 1)
        {
            Debug.Log("RaysPerPixel changed from " + RaysPerPixel + " to 1 because no other values are supported currently!");
            RaysPerPixel = 1;
        }
        
        UpdatePerFrame();
        UpdateSettings(true);
        SetCameraData();
 
        ProgramStarted = true;
    }
 
    private void Update()
    {
        PauseControls();
 
        if (ProgramPaused && FrameStep) Debug.Log("Stepped forward 1 frame");
        if (!ProgramPaused || (ProgramPaused && FrameStep))
        {
            RenderThisFrame = true;
            FrameStep = false;
 
            UpdatePerFrame();
            rtShader.SetBool("TEMP", temp);
 
            if (DoUpdateSettings) { DoUpdateSettings = false; UpdateSettings(DoResetBufferData); DoResetBufferData = false; }
        }
        else RenderThisFrame = false;
    }
 
    private void UpdatePerFrame()
    {
        // Frame set variables
        int FrameRand = UnityEngine.Random.Range(0, 999999);
        rtShader.SetInt("FrameRand", FrameRand);
        rtShader.SetInt("FrameCount", FrameCount);
        ppShader.SetInt("FrameCount", FrameCount);
        FrameCount++;
        CameraMovement();
        // CameraPanning();
        SetCameraData();
    }
 
    private void CameraMovement()
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
 
    private void PauseControls()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            ProgramPaused = !ProgramPaused;
            Debug.Log("Program paused");
        }
        if (Input.GetKeyDown(KeyCode.F)) FrameStep = !FrameStep;
    }
 
    private void CameraPanning()
    {
        if (Input.GetMouseButton(0))
        {
            float mouseY = Input.GetAxis("Mouse Y") * CameraPanSpeed;
            float mouseX = Input.GetAxis("Mouse X") * CameraPanSpeed;
 
            transform.rotation *= Quaternion.Euler(mouseY, -mouseX, 0.0f);
        }
    }
 
    private void SetCameraData()
    {
        // Camera position
        Vector3 worldSpaceCameraPos = transform.position;
        if (lastWorldSpaceCameraPos == null) lastWorldSpaceCameraPos = worldSpaceCameraPos;

        rtShader.SetVector("WorldSpaceCameraPos", worldSpaceCameraPos);
        rtShader.SetVector("LastWorldSpaceCameraPos", lastWorldSpaceCameraPos);
    
        // Camera transform matrix
        Matrix4x4 cameraTransform = transform.localToWorldMatrix;

        if (lastCameraTransform == Matrix4x4.zero) lastCameraTransform = cameraTransform;

        rtShader.SetMatrix("CameraTransform", cameraTransform);
        rtShader.SetMatrix("LastCameraTransformInverse", lastCameraTransform.inverse);

        // Update last frame's camera data
        lastWorldSpaceCameraPos = worldSpaceCameraPos;
        lastCameraTransform = cameraTransform;
    }
 
    private void OnValidate()
    {
        if (ProgramStarted) { DoUpdateSettings = true; DoResetBufferData = true; }
    }
 
    private void UpdateSettings(bool resetBufferData)
    {
        FrameCount = 0;
        if (resetBufferData) SetData();
 
        int[] resolutionArray = new int[] { Resolution.x, Resolution.y };
        rtShader.SetInts("Resolution", resolutionArray);
 
        rtShader.SetInt("MaxBounceCount", MaxBounceCount);
        rtShader.SetInt("RaysPerPixel", RaysPerPixel);
        rtShader.SetFloat("ScatterProbability", ScatterProbability);
 
        float aspectRatio = Resolution.x / Resolution.y;
        float fieldOfViewRad = fieldOfView * Mathf.PI / 180;
        float viewSpaceHeight = Mathf.Tan(fieldOfViewRad * 0.5f);
        float viewSpaceWidth = aspectRatio * viewSpaceHeight;
 
        rtShader.SetVector("ViewSpaceDims", new Vector2(viewSpaceWidth, viewSpaceHeight));
 
        rtShader.SetFloat("DefocusStrength", DefocusStrength);
        rtShader.SetFloat("FocalPlaneFactor", FocalPlaneFactor);
 
        // Debug overlay
        int[] DebugDataMaxValues = new int[] { DebugMaxTriChecks, DebugMaxBVChecks };
        rtShader.SetInts("DebugDataMaxValues", DebugDataMaxValues);
 
        // ReStir
        rtShader.SetInt("SceneObjectReservoirTestsNum", SceneObjectReservoirTestsNum);
        rtShader.SetInt("TriReservoirTestsNum", TriReservoirTestsNum);
        rtShader.SetInt("CandidateReservoirTestsNum", CandidateReservoirTestsNum);
        rtShader.SetFloat("TemporalReuseWeight", TemporalReuseWeight);
        rtShader.SetFloat("PixelMovementThreshold", PixelMovementThreshold);
        rtShader.SetFloat("SpatialHitPointDiffThreshold", SpatialHitPointDiffThreshold);
        rtShader.SetFloat("SpatialNormalsAngleThreshold", SpatialNormalsAngleThreshold);
        rtShader.SetBool("DoVisibilityReuse", DoVisibilityReuse);
        rtShader.SetInt("TemporalCandidatesNum", TemporalCandidatesNum);
        rtShader.SetFloat("TemporalPrecisionThreshold", TemporalPrecisionThreshold);
 
        // Object Textures
        int[] textureAtlasDims = new int[] { TextureAtlas.width, TextureAtlas.height };
        rtShader.SetInts("TextureAtlasDims", textureAtlasDims);
        rtShader.SetTexture(4, "TextureAtlas", TextureAtlas);
 
        // Environment Map Texture
        int[] environmentMapTexDims = new int[] { EnvironmentMapTexture.width, EnvironmentMapTexture.height };
        rtShader.SetInts("EnvironmentMapTexDims", environmentMapTexDims);
        rtShader.SetTexture(4, "EnvironmentMap", EnvironmentMapTexture);
 
        Debug.Log("Internal program settings updated");
    }
 
    private void SetData()
    {
        ComputeHelper.Release(AllBuffers());
 
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

        // Construct BVH
        (BVs, Tris, SceneObjectDatas, LightObjects, TextureAtlas, atlasRects) = meshHelper.CreateSceneObjects();

        for (int i = 0; i < Material2s.Length && MatTypesInput1.Length != 0 && MatTypesInput1.Length == MatTypesInput2.Length && i < atlasRects.Length; i++)
        {
            Material2s[i].texLoc = new int2((int)(atlasRects[i].x * TextureAtlas.width), (int)(atlasRects[i].y * TextureAtlas.height));
            Material2s[i].texDims = new int2((int)(atlasRects[i].width * TextureAtlas.width), (int)(atlasRects[i].height * TextureAtlas.height));
        }

        MaterialBuffer = ComputeHelper.CreateStructuredBuffer<Material2>(Material2s);
        shaderHelper.SetMaterialBuffer(MaterialBuffer);
        
        // Set BVH data
        BVBuffer = ComputeHelper.CreateStructuredBuffer<BoundingVolume>(BVs);
        shaderHelper.SetBVBuffer(BVBuffer);
 
        // Set SceneObjects & Tris data
        SceneObjectDataBuffer = ComputeHelper.CreateStructuredBuffer<SceneObjectData>(SceneObjectDatas);
        shaderHelper.SetSceneObjectDataBuffer(SceneObjectDataBuffer);
        TriBuffer = ComputeHelper.CreateStructuredBuffer<Tri>(Tris);
        shaderHelper.SetTriBuffer(TriBuffer);
        RunPreCalcShader();
 
        // Set LightObjects data
        LightObjectBuffer = ComputeHelper.CreateStructuredBuffer<LightObject>(LightObjects);
        shaderHelper.SetLightObjectBuffer(LightObjectBuffer);
 
        // ReStir
        CandidateBuffer = ComputeHelper.CreateStructuredBuffer<CandidateReservoir>(Resolution.x * Resolution.y * RaysPerPixel);
        shaderHelper.SetCandidateBuffer(CandidateBuffer);
        CandidateReuseBuffer = ComputeHelper.CreateStructuredBuffer<CandidateReservoir>(Resolution.x * Resolution.y * RaysPerPixel);
        shaderHelper.SetCandidateReuseBuffer(CandidateReuseBuffer);
        TemporalFrameBuffer = ComputeHelper.CreateStructuredBuffer<CandidateReservoir>(Resolution.x * Resolution.y * RaysPerPixel);
        shaderHelper.SetTemporalFrameBuffer(TemporalFrameBuffer);
 
        HitInfoBuffer = ComputeHelper.CreateStructuredBuffer<HitInfo>(Resolution.x * Resolution.y);
        shaderHelper.SetHitInfoBuffer(HitInfoBuffer);
    }
 
    private void CreateTextures()
    {
        // Ray tracer result texture
        if (RTResultTexture == null)
        {
            RTResultTexture = TextureHelper.CreateTexture(Resolution, 4);
            RTResultTexture.Create();
            rtShader.SetTexture(4, "Result", RTResultTexture);
            ppShader.SetTexture(0, "Result", RTResultTexture);
        }
 
        // Accumulated result texture
        if (AccumulatedResultTexture == null)
        {
            AccumulatedResultTexture = TextureHelper.CreateTexture(Resolution, 4);
            AccumulatedResultTexture.Create();
            ppShader.SetTexture(0, "AccumulatedResult", AccumulatedResultTexture);
        }
 
        // Debug overlay texture
        if (DebugOverlayTexture == null)
        {
            DebugOverlayTexture = TextureHelper.CreateTexture(Resolution, 4);
            DebugOverlayTexture.Create();
            rtShader.SetTexture(0, "DebugOverlay", DebugOverlayTexture);
            rtShader.SetTexture(3, "DebugOverlay", DebugOverlayTexture);
        }
 
        // Ray hit point double buffer textures
        if (RayHitPointATexture == null)
        {
            RayHitPointATexture = TextureHelper.CreateTexture(Resolution, 3);
            RayHitPointATexture.Create();
            rtShader.SetTexture(0, "RayHitPointsA", RayHitPointATexture);
            rtShader.SetTexture(3, "RayHitPointsA", RayHitPointATexture);
        }
        if (RayHitPointBTexture == null)
        {
            RayHitPointBTexture = TextureHelper.CreateTexture(Resolution, 3);
            RayHitPointBTexture.Create();
            rtShader.SetTexture(0, "RayHitPointsB", RayHitPointBTexture);
            rtShader.SetTexture(3, "RayHitPointsB", RayHitPointBTexture);
        }
 
        // Depth buffer texture
        if (DepthBufferTexture == null)
        {
            DepthBufferTexture = new RenderTexture(Resolution.x, Resolution.y, 0, RenderTextureFormat.RFloat)
            {
                dimension = UnityEngine.Rendering.TextureDimension.Tex2D,
                enableRandomWrite = true,
                filterMode = FilterMode.Bilinear
            };
            DepthBufferTexture.Create();
            rtShader.SetTexture(0, "DepthBuffer", DepthBufferTexture);
            rtShader.SetTexture(1, "DepthBuffer", DepthBufferTexture);
            rtShader.SetTexture(2, "DepthBuffer", DepthBufferTexture);
            rtShader.SetTexture(3, "DepthBuffer", DepthBufferTexture);
        }
 
        // Normals buffer texture
        if (NormalsBufferTexture == null)
        {
            NormalsBufferTexture = TextureHelper.CreateTexture(Resolution, 3);
            NormalsBufferTexture.Create();
            rtShader.SetTexture(0, "NormalsBuffer", NormalsBufferTexture);
            rtShader.SetTexture(1, "NormalsBuffer", NormalsBufferTexture);
        }
    }
 
    private void RunPreCalcShader()
    {
        ComputeHelper.DispatchKernel(pcShader, "CalcTriNormals", Tris.Length, PreCalcThreadSize);
    }
 
    private void SpatialReuse()
    {
        bool reuseBufferCycle = false;
        for (int i = 0; i < SpatialReuseIterations; i++)
        {
            reuseBufferCycle = !reuseBufferCycle;
 
            int maxOffset = (int)Mathf.Pow(3, i) * SpatialReuseBlur;
            rtShader.SetInt("MaxOffset", maxOffset);
            rtShader.SetBool("ReuseBufferCycle", reuseBufferCycle);
 
            ComputeHelper.DispatchKernel(rtShader, "SpatialReusePass", Resolution, RayTracerThreadSize);
        }
 
        if (reuseBufferCycle == true) ComputeHelper.DispatchKernel(rtShader, "TransferToOriginal", Resolution, RayTracerThreadSize);
    }
 
    private void RunReSTIRShader()
    {
        CreateTextures();
 
        ComputeHelper.DispatchKernel(rtShader, "InitialTrace", Resolution, RayTracerThreadSize);
 
        if (TemporalReuseWeight > 0) ComputeHelper.DispatchKernel(rtShader, "TemporalReuse", Resolution, RayTracerThreadSize);
 
        if (SpatialReuseIterations > 0) SpatialReuse();
 
        ComputeHelper.DispatchKernel(rtShader, "TraceRays", Resolution, RayTracerThreadSize);
    }
 
    private void RunPostProcessingShader()
    {
        ComputeHelper.DispatchKernel(ppShader, "AccumulateFrames", Resolution, PostProcesserThreadSize);
    }
 
    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (RenderThisFrame)
        {
            RunReSTIRShader();
            RunPostProcessingShader();
        }

        // Render relected render target to the camera output
        switch (renderTarget)
        {
            case RenderTargetSelect.RTResultTexture:
                Graphics.Blit(RTResultTexture, dest);
                break;
            case RenderTargetSelect.AccumulatedResultTexture:
                Graphics.Blit(AccumulatedResultTexture, dest);
                break;
            case RenderTargetSelect.DebugOverlayTexture:
                Graphics.Blit(DebugOverlayTexture, dest);
                break;
            case RenderTargetSelect.DepthBufferTexture:
                Graphics.Blit(DepthBufferTexture, dest);
                break;
            case RenderTargetSelect.NormalsBufferTexture:
                Graphics.Blit(NormalsBufferTexture, dest);
                break;
            case RenderTargetSelect.EnvironmentMapTexture:
                Graphics.Blit(EnvironmentMapTexture, dest);
                break;
            case RenderTargetSelect.TextureAtlas:
                Graphics.Blit(TextureAtlas, dest);
                break;
            case RenderTargetSelect.RayHitPointATexture:
                Graphics.Blit(RayHitPointATexture, dest);
                break;
            case RenderTargetSelect.RayHitPointBTexture:
                Graphics.Blit(RayHitPointBTexture, dest);
                break;
        }
    }
 
    private ComputeBuffer[] AllBuffers() => new ComputeBuffer[] { BVBuffer, TriBuffer, SceneObjectDataBuffer, LightObjectBuffer, MaterialBuffer, CandidateBuffer, CandidateReuseBuffer, TemporalFrameBuffer, HitInfoBuffer };
 
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
        Texture2D tex = new Texture2D(RayHitPointATexture.width, RayHitPointATexture.height, TextureFormat.RGBA32, false);
 
        // Set the RenderTexture as the active render target
        RenderTexture.active = RayHitPointATexture;
 
        // Read the RenderTexture into the Texture2D
        tex.ReadPixels(new Rect(0, 0, RayHitPointATexture.width, RayHitPointATexture.height), 0, 0);
        tex.Apply();
 
        Color[] pixels = tex.GetPixels();
    }
}