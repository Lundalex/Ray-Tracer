// --- Rendered object types ---

struct Tri
{
    float3 vA;
    float3 vB;
    float3 vC;
    float2 uvA;
    float2 uvB;
    float2 uvC;
    float3 worldNormal;
    int parentIndex;
};
struct BoundingVolume
{
    float3 min;
    float3 max;
    int indexA; // -childIndexA / componentsStart, a < 0 <= b
    int indexB; // -childIndexB / totComponents, a < 0 <= b
};
struct SceneObject
{
    float4x4 worldToLocalMatrix;
    float4x4 localToWorldMatrix;
    int materialIndex;
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
struct Material2
{
    float3 color;
    float3 specularColor;
    float brightness;
    float smoothness;
};

// --- Ray tracer structs ---

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
    int materialIndex;
};
struct TriHitInfo
{
    bool didHit;
    float dst;
    float2 uv;
    int triIndex;
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

// --- Init functions ---

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
    hitInfo.materialIndex = 0;

    return hitInfo;
}
DebugData InitDebugData()
{
    DebugData debugData;
    debugData.triChecks = 0;
    debugData.bvChecks = 0;

    return debugData;
}