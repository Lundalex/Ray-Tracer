static const float PI = 3.1415926;

float sqr(float a)
{
	return a * a;
}

float randNormalized(float n)
{
    return frac(sin(n) * 43758.5453);
}

float randValueNormalDistribution(inout uint state)
{
    // Thanks to https://stackoverflow.com/a/6178290
    float theta = 2 * PI * randNormalized(state);
    float rho = sqrt(-2 * log(randNormalized(state)));
    return rho * cos(theta);
}

float3 randPointOnUnitCircle(inout uint state)
{
    // Thanks to https://math.stackexchange.com/a/1585996
    float x = randValueNormalDistribution(state);
    float y = randValueNormalDistribution(state);
    float z = randValueNormalDistribution(state);
    return normalize(float3(x, y, z));
}