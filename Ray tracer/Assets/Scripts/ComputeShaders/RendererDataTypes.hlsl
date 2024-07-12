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
    int triStart;
    int totTris;
    int childIndexA;
    int childIndexB;
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