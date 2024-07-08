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

float2 triUV(float3 a, float3 b, float3 c, float3 p, float scale)
{
    // Calculate barycentric coordinates of point p with respect to triangle ABC
    float3 v0 = b - a;
    float3 v1 = c - a;
    float3 v2 = p - a;

    float dot00 = dot(v0, v0);
    float dot01 = dot(v0, v1);
    float dot02 = dot(v0, v2);
    float dot11 = dot(v1, v1);
    float dot12 = dot(v1, v2);

    float invDenom = 1.0 / (dot00 * dot11 - dot01 * dot01);
    float u = (dot11 * dot02 - dot01 * dot12) * invDenom;
    float v = (dot00 * dot12 - dot01 * dot02) * invDenom;
    float w = 1.0 - u - v;

    // UV coordinates for vertex a, b, and c
    float2 uvA = float2(0.0, 0.0);
    float2 uvB = float2(1.0, 0.0);
    float2 uvC = float2(0.0, 1.0);

    // Interpolate UV coordinates of point p based on UV coordinates of vertices a, b, and c
    float2 uv = (u * uvA + v * uvB + w * uvC) * scale % 1.0;

    return uv;
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