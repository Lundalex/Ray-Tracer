#include "Constants.hlsl"
#include "RendererDataTypes.hlsl"

float MSDensityKernel(float dst, float radius)
{
	if (dst < radius)
	{
        float dstR = dst / radius;
        return sqrt(1 - dstR);
	}
	return 0;
}

void ApplyTransformTriVertices(float3 rot, inout float3 a, inout float3 b, inout float3 c)
{
    float cosX = cos(rot.x);
    float sinX = sin(rot.x);
    float cosY = cos(rot.y);
    float sinY = sin(rot.y);
    float cosZ = cos(rot.z);
    float sinZ = sin(rot.z);

    // Combine rotation matrices into a single matrix
    float3x3 rotationMatrix = float3x3(
        cosY * cosZ,                             cosY * sinZ,                           -sinY,
        sinX * sinY * cosZ - cosX * sinZ,   sinX * sinY * sinZ + cosX * cosZ,  sinX * cosY,
        cosX * sinY * cosZ + sinX * sinZ,   cosX * sinY * sinZ - sinX * cosZ,  cosX * cosY
    );

    // Apply the combined rotation matrix to each vertex
    a = mul(rotationMatrix, a);
    b = mul(rotationMatrix, b);
    c = mul(rotationMatrix, c);
}

float sqr(float a)
{
	return a * a;
}

float avg(float a, float b) // float version
{
    return .5 * (a + b);
}
float2 avg(float2 a, float2 b) // float2 version
{
    return .5 * (a + b);
}
float3 avg(float3 a, float3 b) // float3 version
{
    return .5 * (a + b);
}

float dot2(float3 a) // float3 version
{
    return dot(a, a);
}
float dot2(float2 a) // float2 version
{
    return dot(a, a);
}

uint NextRandom(inout uint state)
{
    state = state * 747796405 + 2891336453;
    uint result = ((state >> ((state >> 28) + 4)) ^ state) * 277803737;
    result = (result >> 22) ^ result;
    return result;
}

float randNormalized(inout uint state)
{
    return NextRandom(state) / 4294967295.0; // 2^32 - 1
}

int randIntSpan(int a, int b, inout uint state)
{
    float randNorm = randNormalized(state);
    int diff = b - a;
    int offset = (int)(diff * randNorm);
    int result = a + offset;
    return result;
}

bool weightedRand(float a, float b, inout uint state)
{
    float randNorm = randNormalized(state);
    
    float totalWeight = a + b;
    float relRand = randNorm * totalWeight;
    return relRand < a;
}

float randValueNormalDistribution(inout uint state)
{
    float theta = 2 * PI * randNormalized(state);
    float rho = sqrt(-2 * log(randNormalized(state)));
    return rho * cos(theta);
}

// Expensive!
float3 randPointOnUnitSphere(inout uint state)
{
    float x = randValueNormalDistribution(state);
    float y = randValueNormalDistribution(state);
    float z = randValueNormalDistribution(state);
    return normalize(float3(x, y, z));
}

float2 randPointInCircle(inout uint state)
{
    float angle = randNormalized(state) * 2 * PI;
    float2 pointOnCircle = float2(cos(angle), sin(angle));
    return pointOnCircle * sqrt(randNormalized(state));
}

float GetTriArea(Tri tri)
{
    float3 ab = tri.vB - tri.vA;
    float3 ac = tri.vC - tri.vA;
    float3 crossProduct = cross(ab, ac);
    float area = length(crossProduct) * 0.5;
    return area;
}

float3 GetRandWorldPointTri(Tri tri, float4x4 localToWorldMatrix, inout uint state)
{
    float r1 = randNormalized(state);
    float r2 = randNormalized(state);

    // Ensure the point lies within the triangle
    if (r1 + r2 > 1.0)
    {
        r1 = 1.0 - r1;
        r2 = 1.0 - r2;
    }

    float3 localPoint = tri.vA * (1.0 - r1 - r2) + tri.vB * r1 + tri.vC * r2;

    float3 worldPoint = mul(localToWorldMatrix, float4(localPoint, 1.0)).xyz;

    return worldPoint;
}