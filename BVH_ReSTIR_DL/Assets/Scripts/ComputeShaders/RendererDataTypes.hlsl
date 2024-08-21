// --- Rendered object types ---

struct Tri // Replace with vertex lookup & vertex buffers !!!
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
    float3 directLight;
    float3 indirectLight;
    float3 firstHitPoint;
};
struct Reservoir
{
    int chosenIndex;
    float chosenWeight;
    float totWeights;
};
struct CandidateReservoir
{
    float3 dir;
    float3 hitPoint;
    float3 normal;
    float chosenWeight;
    float totWeights;
    float totCandidates; // "float" since temporal reuse averages the values from sequential frames
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
Ray InitRay(float3 pos, float3 dir)
{
    Ray ray;
    ray.pos = pos;
    ray.dir = dir;

    return ray;
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
Material2 InitMaterial()
{
    Material2 material;
    material.color = 0;
    material.specularColor = 0;
    material.brightness = 0;
    material.smoothness = 0;

    return material;
}
CandidateReservoir InitCandidateReservoir(float3 dir, float3 hitPoint, float3 normal, float chosenWeight, float totWeights, int totCandidates)
{
    CandidateReservoir candidateReservoir;
    candidateReservoir.dir = dir;
    candidateReservoir.hitPoint = hitPoint;
    candidateReservoir.normal = normal;
    candidateReservoir.chosenWeight = chosenWeight;
    candidateReservoir.totWeights = totWeights;
    candidateReservoir.totCandidates = totCandidates;

    return candidateReservoir;
}
CandidateReservoir InitCandidateReservoir()
{
    CandidateReservoir candidateReservoir;
    candidateReservoir.dir = 0;
    candidateReservoir.hitPoint = 0;
    candidateReservoir.normal = 0;
    candidateReservoir.chosenWeight = 0;
    candidateReservoir.totWeights = 0;
    candidateReservoir.totCandidates = 0;

    return candidateReservoir;
}