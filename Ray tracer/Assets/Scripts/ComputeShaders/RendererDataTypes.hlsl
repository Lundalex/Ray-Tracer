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
    int MaxDepthBVH;
    float3 min;
    float3 max;
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
    bool didHit;
    float dst;
    float3 hitPoint;
    float3 normal;
    int materialKey;
};
struct TriangleHitInfo
{
    bool didHit;
    float dst;
    float3 hitPoint;
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
};
struct DebugData
{
    int triChecks;
    int bvChecks;
};

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
    hitInfo.didHit = false;
    hitInfo.dst = 1.#INF;
    hitInfo.hitPoint = 0;
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