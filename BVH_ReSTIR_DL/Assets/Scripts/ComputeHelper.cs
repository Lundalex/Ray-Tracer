using Unity.Mathematics;
using UnityEngine;

// Import utils from Resources.cs
using Resources2;
public static class ComputeHelper
{

#region Kernel Dispatch

    /// <summary>Dispatch a shader kernel</summary>
    /// <remarks>Uses (int)threadsNum, threadSize</remarks>
    static public void DispatchKernel (ComputeShader cs, string kernelName, int threadsNum, int threadSize)
    {
        int threadGroupsNum = Utils.GetThreadGroupsNum(threadsNum, threadSize);
        cs.Dispatch(cs.FindKernel(kernelName), threadGroupsNum, 1, 1);
    }
    /// <summary>Dispatch a shader kernel</summary>
    /// <remarks>Uses (int2)threadsNum, threadSize</remarks>
    static public void DispatchKernel (ComputeShader cs, string kernelName, int2 threadsNum, int threadSize)
    {
        int2 threadGroupsNums = Utils.GetThreadGroupsNum(threadsNum, threadSize);
        cs.Dispatch(cs.FindKernel(kernelName), threadGroupsNums.x, threadGroupsNums.y, 1);
    }
    /// <summary>Dispatch a shader kernel</summary>
    /// <remarks>Uses (int3)threadsNum, threadSize</remarks>
    static public void DispatchKernel (ComputeShader cs, string kernelName, int3 threadsNum, int threadSize)
    {
        int3 threadGroupNums = Utils.GetThreadGroupsNum(threadsNum, threadSize);
        cs.Dispatch(cs.FindKernel(kernelName), threadGroupNums.x, threadGroupNums.y, threadGroupNums.z);
    }
    /// <summary>Dispatch a shader kernel</summary>
    /// <remarks>Uses (int)threadGroupsNum</remarks>
    static public void DispatchKernel (ComputeShader cs, string kernelName, int threadGroupsNum)
    {
        cs.Dispatch(cs.FindKernel(kernelName), threadGroupsNum, 1, 1);
    }
    /// <summary>Dispatch a shader kernel</summary>
    /// <remarks>Uses (int2)threadGroupsNum</remarks>
    static public void DispatchKernel (ComputeShader cs, string kernelName, int2 threadGroupsNums)
    {
        cs.Dispatch(cs.FindKernel(kernelName), threadGroupsNums.x, threadGroupsNums.y, 1);
    }
    /// <summary>Dispatch a shader kernel</summary>
    /// <remarks>Uses (int3)threadGroupsNum</remarks>
    static public void DispatchKernel (ComputeShader cs, string kernelName, int3 threadGroupsNums)
    {
        cs.Dispatch(cs.FindKernel(kernelName), threadGroupsNums.x, threadGroupsNums.y, threadGroupsNums.z);
    }
#endregion

#region Create Buffers

    /// <summary>Create an append buffer</summary>
    /// <returns>Without ref</returns>
	public static ComputeBuffer CreateAppendBuffer<T>(int capacity) // T is the buffer struct
	{
		int stride = GetStride<T>();
		ComputeBuffer buffer = new ComputeBuffer(Func.MaxInt(capacity, 1), stride, ComputeBufferType.Append);
		buffer.SetCounterValue(0);
		return buffer;
	}
    /// <summary>Create an append buffer</summary>
    /// <returns>-> ref buffer</returns>
	public static void CreateAppendBuffer<T>(ref ComputeBuffer buffer, int capacity) // T is the buffer struct
	{
		int stride = GetStride<T>();
        buffer ??= new ComputeBuffer(Func.MaxInt(capacity, 1), stride, ComputeBufferType.Append);
		buffer.SetCounterValue(0);
	}
    /// <summary>Create a structured buffer</summary>
    /// <returns>Without ref</returns>
	public static ComputeBuffer CreateStructuredBuffer<T>(T[] data) // T is the buffer struct
	{
		var buffer = new ComputeBuffer(Func.MaxInt(data.Length, 1), GetStride<T>());
		buffer.SetData(data);
		return buffer;
	}
    /// <summary>Create a structured buffer</summary>
    /// <returns>-> ref buffer</returns>
    public static void CreateStructuredBuffer<T>(ref ComputeBuffer buffer, T[] data) // T is the buffer struct
    {
            int length = Func.MaxInt(1, data.Length);
            int stride = GetStride<T>();

            if (buffer == null || !buffer.IsValid() || buffer.count != length || buffer.stride != stride)
            {
                buffer?.Release();
                buffer = new ComputeBuffer(length, stride, ComputeBufferType.Structured);
            }

            buffer.SetData(data);
    }
    /// <summary>Create a structured buffer</summary>
    /// <returns>Without ref</returns>
	public static ComputeBuffer CreateStructuredBuffer<T>(int count) // T is the buffer struct
	{
		var buffer = new ComputeBuffer(count, GetStride<T>());
		return buffer;
	}
    /// <summary>Create a structured buffer</summary>
    /// <returns>-> ref buffer</returns>
	public static void CreateStructuredBuffer<T>(ref ComputeBuffer buffer, int count) // T is the buffer struct
	{
		count = Func.MaxInt(count, 1);
		int stride = GetStride<T>();

		bool createNewBuffer = buffer == null || !buffer.IsValid() || buffer.count != count || buffer.stride != stride;
		if (createNewBuffer)
		{
			Release(buffer);
			buffer = new ComputeBuffer(count, stride, ComputeBufferType.Structured);
		}
	}
    /// <summary>Create a count buffer</summary>
    /// <returns>Without ref</returns>
    public static ComputeBuffer CreateCountBuffer()
    {
        ComputeBuffer countBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        return countBuffer;
    }
    /// <summary>Create a count buffer</summary>
    /// <returns>-> ref countBuffer</returns>
    public static void CreateCountBuffer(ref ComputeBuffer countBuffer)
    {
        countBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
    }
#endregion

#region Get Buffer Data

    /// <summary>Get append buffer count</summary>
    /// <remarks>Uses an countBuffer</remarks>
	public static int GetAppendBufferCount(ComputeBuffer buffer, ComputeBuffer countBuffer)
	{
        ComputeBuffer.CopyCount(buffer, countBuffer, 0);
        int[] countArr = new int[1];
        countBuffer.GetData(countArr);
        int count = countArr[0];
        return count;
	}
    /// <summary>Get append buffer count</summary>
    /// <remarks>Does not use an countBuffer</remarks>
	public static int GetAppendBufferCount(ComputeBuffer buffer)
	{
        ComputeBuffer countBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        ComputeBuffer.CopyCount(buffer, countBuffer, 0);
        int[] countArr = new int[1];
        countBuffer.GetData(countArr);
        int count = countArr[0];
        return count;
	}

    /// <summary>Get data from a compute buffer into a c# array</summary>
	public static T[] GetStructuredBufferData<T>(ComputeBuffer buffer)
	{
        T[] temp = new T[buffer.count];
        buffer.GetData(temp);
        return temp;
	}
#endregion

#region Buffer Sorting

    /// <summary>Sorts a given spatial lookup / start indices pair of buffers</summary>
    /// <remarks>Sorts the spatial lookup in ascending order, prioritising the x-component over the y-component. Sorts the buffers associated with the given sorting shader</remarks>
    public static void SpatialSort(ComputeShader sortShader, int sortLength, int threadSize)
    {
        sortShader.SetInt("SortLength", sortLength);
        int sortLengthNextPow2 = Func.NextPow2(sortLength);
        sortShader.SetInt("SortLengthNextPow2", Func.NextPow2(sortLength));

        int threadGroupsNum = Utils.GetThreadGroupsNum(sortLengthNextPow2, threadSize);

        DispatchKernel(sortShader, "PopulateSpatialLookup", threadGroupsNum);

        // Primary sorting loop
        int basebBlockLen = 2;
        while (basebBlockLen != 2*sortLengthNextPow2) // basebBlockLen == sortLengthNextPow2 is the last outer iteration
        {
            int blockLen = basebBlockLen;
            while (blockLen != 1) // blockLen == 2 is the last inner iteration
            {
                bool brownPinkSort = blockLen == basebBlockLen;

                sortShader.SetBool("BrownPinkSort", brownPinkSort);
                sortShader.SetInt("BlockLen", blockLen);

                DispatchKernel(sortShader, "SortIteration", Mathf.CeilToInt(threadGroupsNum * 0.5f));

                blockLen /= 2;
            }
            basebBlockLen *= 2;
        }

        DispatchKernel(sortShader, "PopulateStartIndices", threadGroupsNum);
    }
#endregion

#region Release Buffers / Textures

    /// <summary>Releases a single compute buffer</summary>
	public static void Release(ComputeBuffer buffer)
	{
		buffer?.Release(); // ComputeBuffer class passed by reference automatically
	}
    /// <summary>Releases multiple compute buffer</summary>
    public static void Release(params ComputeBuffer[] buffers)
	{
        for (int i = 0; i < buffers.Length; i++)
        {
            Release(buffers[i]);
        }
	}
    /// <summary>Releases a single render texture</summary>
	public static void Release(RenderTexture texture)
	{
		if (texture != null)
		{
			texture.Release(); // RenderTexture class passed by reference automatically
		}
	}
#endregion

#region Class

    /// <returns>The combined stride (size in bytes) of a struct/datatype</returns>
    public static int GetStride<T>() => System.Runtime.InteropServices.Marshal.SizeOf(typeof(T));
#endregion
}