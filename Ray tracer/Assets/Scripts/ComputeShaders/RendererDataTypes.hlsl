// --- Rendered Object Types ---

struct TriObject
{
    int rootIndexBVH;
    float4x4 worldToLocal;
    float4x4 localToWorld;
    int materialKey;
};
struct Tri
{
    float3 vA;
    float3 vB;
    float3 vC;
    float2 uvA;
    float2 uvB;
    float2 uvC;
    float3 normal;
    int materialKey;
    int parentKey;
};
struct BoundingVolume
{
    float3 min;
    float3 max;
    int componentStart;
    int totComponents;
    int childIndexA;
    int childIndexB;
};
struct SceneObject
{
    float4x4 worldToLocalMatrix;
    float4x4 localToWorldMatrix;
    int materialKey;
    int bvStartIndex;
    int maxDepthBVH;
    float areaApprox;
    float3 min;
    float3 max;
};
struct LightObject
{
    float4x4 localToWorldMatrix;
    float areaApprox;
    float brightness;
    int triStart;
    int totTris;
};
struct Sphere
{
    float3 pos;
    float radius;
    int materialKey;
};
struct Material2
{
    float3 color;
    float3 specularColor;
    float brightness;
    float smoothness;
};

// --- Ray Tracer Structs ---

struct Ray
{
    float3 pos;
    float3 dir;
};
struct HitInfo
{
    float dst;
    float3 hitPoint;
    float2 uv;
    float3 normal;
    int materialKey;
};
struct TriHitInfo
{
    bool didHit;
    float dst;
    float3 hitPoint;
    float2 uv;
    int triIndex;
};
struct BVHitInfo
{
    bool didHit;
    float dst;
};
struct TraceInfo
{
    float3 rayColor;
    float3 incomingLight;
    float3 firstHitPoint;
};
struct Reservoir
{
    int chosenIndex;
    float chosenWeight;
    float totWeights;
};
struct DebugData
{
    int triChecks;
    int bvChecks;
};

Reservoir InitReservoir(int firstElementIndex, float firstElementWeight)
{
    Reservoir reservoir;
    reservoir.chosenIndex = firstElementIndex;
    reservoir.chosenWeight = firstElementWeight;
    reservoir.totWeights = firstElementWeight;

    return reservoir;
}

Ray InitRay()
{
    Ray ray;
    ray.pos = 0;
    ray.dir = 0;

    return ray;
}

HitInfo InitHitInfo()
{
    HitInfo hitInfo;
    hitInfo.dst = 1.#INF;
    hitInfo.hitPoint = -1;
    hitInfo.uv = 0;
    hitInfo.normal = 0;
    hitInfo.materialKey = 0;

    return hitInfo;
}

DebugData InitDebugData()
{
    DebugData debugData;
    debugData.triChecks = 0;
    debugData.bvChecks = 0;

    return debugData;
}