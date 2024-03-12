static const float PI = 3.1415926;

float sqr(float a)
{
	return a * a;
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

float3 randPointOnUnitSphere(inout uint state)
{
    float x = randValueNormalDistribution(state);
    float y = randValueNormalDistribution(state);
    float z = randValueNormalDistribution(state);
    return normalize(float3(x, y, z));
}