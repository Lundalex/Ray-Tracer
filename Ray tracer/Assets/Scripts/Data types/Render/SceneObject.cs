using Matrix4x4 = UnityEngine.Matrix4x4;
public struct SceneObjectData
{
    public Matrix4x4 worldToLocalMatrix;
    public Matrix4x4 localToWorldMatrix;
    public int materialKey;
    public int triStartIndex;
    public int bvStartIndex;
};