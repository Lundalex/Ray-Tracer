using Unity.Mathematics;
public struct PData
{
    public float3 PredPosition;
    public float3 Position;
    public float3 Velocity;
    public float3 LastVelocity;
    public float Density;
    public float NearDensity;
    public float Temperature; // kelvin
    public float TemperatureExchangeBuffer;
    public int LastChunkKey_PType_POrder; // composed 3 int structure
    // POrder; // POrder is dynamic, 
    // LastChunkKey; // 0 <= LastChunkKey <= ChunkNum
    // PType; // 0 <= PType <= PTypeNum
}