float3 FresnelSchlick(float cosTheta, float3 F0)
{
    return F0 + (1.0 - F0) * pow(1.0 - cosTheta, 5.0);
}

float GeometrySmith(float NdotL, float NdotV, float roughness)
{
    float r = roughness + 1.0;
    float k = (r * r) / 8.0;
    float G1 = NdotL / (NdotL * (1.0 - k) + k);
    float G2 = NdotV / (NdotV * (1.0 - k) + k);
    return G1 * G2;
}

float DistributionGGX(float NdotH, float roughness)
{
    float a = roughness * roughness;
    float a2 = a * a;
    float NdotH2 = NdotH * NdotH;

    float denom = (NdotH2 * (a2 - 1.0) + 1.0);
    return a2 / (3.14 * denom * denom);
}

float GetBRDF(float3 incomingDir, float3 outgoingDir, float3 hitNormal, float metallicity, float roughness)
{
    metallicity = 1; // METALLICITY NOT USED

    incomingDir = -incomingDir;
    float3 hitAlbedo = float3(1, 0, 1);

    // Calculate necessary vectors and terms
    float3 H = normalize(incomingDir + outgoingDir);  // Halfway vector

    // Dot products for the BRDF components
    float NdotL = saturate(dot(hitNormal, incomingDir));   // Light direction dot normal
    float NdotV = saturate(dot(hitNormal, outgoingDir));   // View direction dot normal
    float NdotH = saturate(dot(hitNormal, H));             // Normal dot halfway vector
    float VdotH = saturate(dot(outgoingDir, H));           // View direction dot halfway vector

    // Fresnel
    float3 F0 = lerp(float3(0.04, 0.04, 0.04), hitAlbedo, metallicity); // Base reflectivity
    float3 F = FresnelSchlick(VdotH, F0);

    // Distribution
    float D = DistributionGGX(NdotH, roughness);

    // Geometry
    float G = GeometrySmith(NdotL, NdotV, roughness);

    // BRDF result
    float3 numerator = D * G * F;
    float denominator = 4.0 * NdotL * NdotV + 0.001; // Add a small value to prevent division by zero
    float3 specular = numerator / denominator;

    // Lambertian diffuse term
    float3 kS = F;
    float3 kD = 1.0 - kS;
    kD *= 1.0 - metallicity; // Metal has no diffuse component

    float3 diffuse = kD * hitAlbedo / 3.14;

    // Final BRDF value
    return (diffuse + specular) * NdotL * 10;
}