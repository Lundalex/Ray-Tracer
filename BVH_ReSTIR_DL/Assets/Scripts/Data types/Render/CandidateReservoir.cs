using Unity.Mathematics;

public struct CandidateReservoir
{
    public float3 dir;
    public float3 hitPoint;
    public float3 normal;
    public float brdf;
    public float chosenWeight;
    public float totWeights;
    public float totCandidates;
};