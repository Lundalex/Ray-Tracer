// --- Rendered Object Types ---

struct TriObject
{
    float3 pos;
    float3 rot;
};
struct Tri // Triangle
{
    float3 vA;
    float3 vB;
    float3 vC;
    float3 normal;
    int materialKey;
    int parentKey;
};
struct Tri2 // Triangle (variant)
{
    float3 vA;
    float3 vB;
    float3 vC;
};
struct Sphere
{
    float3 pos;
    float radius;
    int materialKey;
};
struct Box
{
    float3 cornerA;
    float3 cornerB;
    int materialKey;
};
struct Material2
{
    float3 color;
    float3 specularColor;
    float brightness;
    float smoothness;
};

// Ray Tracer Structss

struct Ray
{
    float3 origin;
    float3 pos;
    float3 dir;
};
struct HitInfo
{
    bool didHit;
    float dst;
    float3 hitPoint;
    float3 normal;
    Material2 material2;
};
struct TraceInfo
{
    float3 rayColor;
    float3 incomingLight;
};