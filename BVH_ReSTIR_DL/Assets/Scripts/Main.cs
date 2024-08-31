using UnityEngine;
using Debug = UnityEngine.Debug;
using Unity.Mathematics;
using System;

public class Main : MonoBehaviour
{
    [Header("Camera interaction settings")]
    public float CameraMoveSpeed;
    public float CameraPanSpeed;
    [Header("Debug settings")]
    public RenderTargetSelect renderTarget;
    public bool RenderAsciiArt;
    public int DebugMaxTriChecks;
    public int DebugMaxBVChecks;
    [Header("Render settings")]
    public float fieldOfView;
    public int2 Resolution;
 
    [Header("Ray tracer settings")]
    public int MaxBounceCount;
    [Range(0.0f, 1.0f)] public float ScatterProbability;
    [Range(0.0f, 0.2f)] public float DefocusStrength;
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
    public float PixelMovementThreshold;
    public float SpatialHitPointDiffThreshold;
    public float SpatialNormalsAngleThreshold;
    public bool DoVisibilityReuse;
    public bool DoWeightRecalc;
    public float VisibilityReuseThreshold;
 
    [Header("References")]
    public ComputeShader rtShader; // BVH + ReStir (direct lighting)
    public ComputeShader ppShader;
    public ComputeShader pcShader;
    public ShaderHelper shaderHelper;
    public ObjectHelper objectHelper;
    public AsciiHelper asciiHelper;
    public Texture2D EnvironmentMapTexture;
    public Texture2D BlackTexture;
 
    // Script communication
    [NonSerialized] public bool DoUpdateSettings;
    [NonSerialized] public bool DoReloadData = false;
    [NonSerialized] public bool ProgramStarted = false;
 
    // Private variables
    private RenderTexture RTResultTexture;
    private RenderTexture AccumulatedResultTexture;
    private RenderTexture DebugOverlayTexture;
    private int RayTracerThreadSize = 8; // /32
    private int PostProcesserThreadSize = 8; // /32
    private int PreCalcThreadSize = 256;
    [NonSerialized] public Material2[] Material2s;
    [NonSerialized] public RenderTriangle[] RenderTriangles;
    [NonSerialized] public Vertex[] Vertices;
    [NonSerialized] public SceneObjectData[] SceneObjectDatas;
    [NonSerialized] public LightObject[] LightObjects;
    [NonSerialized] public RenderBV[] BVs;
    private ComputeBuffer BVBuffer;
    private ComputeBuffer RenderTriangleBuffer;
    private ComputeBuffer VertexBuffer;
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
    private Rect[] AtlasRects;
 
    // Camera data record
    private Vector3 lastCameraPosition;
    private Quaternion lastCameraRotation;
 
    // Script-specific variables
    private bool ProgramPaused = false;
    private bool FrameStep = false;
    private bool RenderThisFrame = true;
    private Vector3 lastWorldSpaceCameraPos;
    private Matrix4x4 lastCameraTransform;
    private bool[] LoggedWarnings = new bool[2];


    private void Start()
    {
        lastCameraPosition = transform.position;
        lastCameraRotation = transform.rotation;
        
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
 
            if (DoUpdateSettings) { DoUpdateSettings = false; UpdateSettings(DoReloadData); DoReloadData = false; }
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
        if (ProgramStarted) { DoUpdateSettings = true; DoReloadData = true; }
    }
 
    private void UpdateSettings(bool resetBufferData)
    {
        FrameCount = 0;
        if (resetBufferData) SetData();
 
        int[] resolutionArray = new int[] { Resolution.x, Resolution.y };
        rtShader.SetInts("Resolution", resolutionArray);

        if (MaxBounceCount != 1 && !LoggedWarnings[0]) { LoggedWarnings[0] = true; Debug.LogWarning("ReSTIR DL not designed for multi-bounce rays. Additional ray bounces may lead to unexpected results"); }
        rtShader.SetInt("MaxBounceCount", MaxBounceCount);
        rtShader.SetFloat("ScatterProbability", ScatterProbability);
 
        float aspectRatio = Resolution.x / Resolution.y;
        float fieldOfViewRad = fieldOfView * Mathf.PI / 180;
        float viewSpaceHeight = Mathf.Tan(fieldOfViewRad * 0.5f);
        float viewSpaceWidth = aspectRatio * viewSpaceHeight;
 
        rtShader.SetVector("ViewSpaceDims", new Vector2(viewSpaceWidth, viewSpaceHeight));
 
        if (DefocusStrength != 0 && !LoggedWarnings[1]) { LoggedWarnings[1] = true; Debug.LogWarning("ReSTIR not designed for focus ray modifiers. Enabling this will decrease the effectiveness of spatiotemporal reuse"); }
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
        rtShader.SetInt("TemporalCandidatesNum", TemporalCandidatesNum);
        rtShader.SetFloat("TemporalPrecisionThreshold", TemporalPrecisionThreshold);
        rtShader.SetFloat("VisibilityReuseThreshold", VisibilityReuseThreshold);

        // Multi-compilation
        if (DoVisibilityReuse) rtShader.EnableKeyword("VISIBILITY_REUSE");
        else rtShader.DisableKeyword("VISIBILITY_REUSE");
        if (DoWeightRecalc) rtShader.EnableKeyword("WEIGHT_RECALC");
        else rtShader.DisableKeyword("WEIGHT_RECALC");

        // Object Textures
        int[] textureAtlasDims = new int[] { TextureAtlas.width, TextureAtlas.height };
        rtShader.SetInts("TextureAtlasDims", textureAtlasDims);
        rtShader.SetTexture(0, "TextureAtlas", TextureAtlas);
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

        // Construct BVH
        (BVs, Vertices, RenderTriangles, SceneObjectDatas, LightObjects, TextureAtlas, Material2s) = objectHelper.ConstructScene();

        MaterialBuffer = ComputeHelper.CreateStructuredBuffer<Material2>(Material2s);
        shaderHelper.SetMaterialBuffer(MaterialBuffer);
        
        // Set BVH data
        BVBuffer = ComputeHelper.CreateStructuredBuffer<RenderBV>(BVs);
        shaderHelper.SetBVBuffer(BVBuffer);
 
        // Set SceneObjects & Tris data
        SceneObjectDataBuffer = ComputeHelper.CreateStructuredBuffer<SceneObjectData>(SceneObjectDatas);
        shaderHelper.SetSceneObjectDataBuffer(SceneObjectDataBuffer);
        RenderTriangleBuffer = ComputeHelper.CreateStructuredBuffer<RenderTriangle>(RenderTriangles);
        shaderHelper.SetTriBuffer(RenderTriangleBuffer);
        VertexBuffer = ComputeHelper.CreateStructuredBuffer<Vertex>(Vertices);
        shaderHelper.SetVertexBuffer(VertexBuffer);
        RunPreCalcShader();
 
        // Set LightObjects data
        LightObjectBuffer = ComputeHelper.CreateStructuredBuffer<LightObject>(LightObjects);
        shaderHelper.SetLightObjectBuffer(LightObjectBuffer);
 
        // ReStir
        CandidateBuffer = ComputeHelper.CreateStructuredBuffer<CandidateReservoir>(Resolution.x * Resolution.y);
        shaderHelper.SetCandidateBuffer(CandidateBuffer);
        CandidateReuseBuffer = ComputeHelper.CreateStructuredBuffer<CandidateReservoir>(Resolution.x * Resolution.y);
        shaderHelper.SetCandidateReuseBuffer(CandidateReuseBuffer);
        TemporalFrameBuffer = ComputeHelper.CreateStructuredBuffer<CandidateReservoir>(Resolution.x * Resolution.y);
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
        ComputeHelper.DispatchKernel(pcShader, "CalcTriNormals", RenderTriangles.Length, PreCalcThreadSize);
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
        if (renderTarget == RenderTargetSelect.None || RenderAsciiArt) Graphics.Blit(BlackTexture, dest);
        else switch (renderTarget)
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
            case RenderTargetSelect.None:
                Graphics.Blit(BlackTexture, dest);
                break;
        }

        if (RenderAsciiArt)
        {
            Texture2D tex;
            switch (renderTarget)
            {
                case RenderTargetSelect.RTResultTexture:
                    tex = asciiHelper.RenderTextureToTexture2D(RTResultTexture);
                    break;
                case RenderTargetSelect.AccumulatedResultTexture:
                    tex = asciiHelper.RenderTextureToTexture2D(AccumulatedResultTexture);
                    break;
                case RenderTargetSelect.DebugOverlayTexture:
                    tex = asciiHelper.RenderTextureToTexture2D(DebugOverlayTexture);
                    break;
                case RenderTargetSelect.DepthBufferTexture:
                    tex = asciiHelper.RenderTextureToTexture2D(DepthBufferTexture);
                    break;
                case RenderTargetSelect.NormalsBufferTexture:
                    tex = asciiHelper.RenderTextureToTexture2D(NormalsBufferTexture);
                    break;
                case RenderTargetSelect.RayHitPointATexture:
                    tex = asciiHelper.RenderTextureToTexture2D(RayHitPointATexture);
                    break;
                case RenderTargetSelect.RayHitPointBTexture:
                    tex = asciiHelper.RenderTextureToTexture2D(RayHitPointBTexture);
                    break;
                default:
                    asciiHelper.Asciitext.enabled = false;
                    return;
            }

            string asciiArt = asciiHelper.ConvertTextureToASCII(tex);

            asciiHelper.Asciitext.enabled = true;
            asciiHelper.Asciitext.text = asciiArt;

            Destroy(tex);
        }
        else asciiHelper.Asciitext.enabled = false;
    }
 
    private ComputeBuffer[] AllBuffers() => new ComputeBuffer[] { BVBuffer, RenderTriangleBuffer, VertexBuffer, SceneObjectDataBuffer, LightObjectBuffer, MaterialBuffer, CandidateBuffer, CandidateReuseBuffer, TemporalFrameBuffer, HitInfoBuffer };
 
    private void OnDestroy()
    {
        ComputeHelper.Release(AllBuffers());
        DepthBufferTexture.Release(); // Test release. 9undisposed -> works
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