using UnityEngine;
using Unity.Mathematics;
using System;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using Debug = UnityEngine.Debug;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;

namespace Resources2
{
    public class Utils
    {
        public static int GetThreadGroupsNum(int threadsNum, int threadSize)
        {
            int threadGroupsNum = (int)Math.Ceiling((float)threadsNum / threadSize);
            return threadGroupsNum;
        }
        public static int2 GetThreadGroupsNum(int2 threadsNum, int threadSize)
        {
            int threadGroupsNumX = GetThreadGroupsNum(threadsNum.x, threadSize);
            int threadGroupsNumY = GetThreadGroupsNum(threadsNum.y, threadSize);
            return new(threadGroupsNumX, threadGroupsNumY);
        }
        public static int3 GetThreadGroupsNum(int3 threadsNum, int threadSize)
        {
            int threadGroupsNumX = GetThreadGroupsNum(threadsNum.x, threadSize);
            int threadGroupsNumY = GetThreadGroupsNum(threadsNum.y, threadSize);
            int threadGroupsNumZ = GetThreadGroupsNum(threadsNum.z, threadSize);
            return new(threadGroupsNumX, threadGroupsNumY, threadGroupsNumZ);
        }

        public static bool2 GetMousePressed()
        {
            bool LMousePressed = Input.GetMouseButton(0);
            bool RMousePressed = Input.GetMouseButton(1);

            bool2 MousePressed = new bool2(LMousePressed, RMousePressed);

            return MousePressed;
        }

        public static Vector2 GetMousePosNormalised()
        {
            Vector3 mousePos = Input.mousePosition;
            Vector2 mouseWorldPos = new Vector2(mousePos.x / 3840, mousePos.y / 2160);

            return mouseWorldPos;
        }

        public static float CelciusToKelvin(float celciusTemp)
        {
            return 273.15f + celciusTemp;
        }
        
        public static float3 GetParticleSpawnPosition(int pIndex, int maxIndex, int width, int height, int depth)
        {
            float posX = Func.RandFloat((float)5+width/4, (float)3*width/4-5);
            float posY = Func.RandFloat((float)5, (float)height-5);
            float posZ = Func.RandFloat((float)5, (float)depth-5);

            return new float3(posX, posY, posZ);
        }

        public static Matrix4x4 CreateWorldToLocalMatrix(Vector3 position, Vector3 rotation, Vector3 scale)
        {
            // Translation matrix
            Matrix4x4 translationMatrix = Matrix4x4.Translate(-position);

            // Rotation matrices for each axis
            Matrix4x4 rotationXMatrix = Matrix4x4.Rotate(Quaternion.Euler(-rotation.x, 0, 0));
            Matrix4x4 rotationYMatrix = Matrix4x4.Rotate(Quaternion.Euler(0, -rotation.y, 0));
            Matrix4x4 rotationZMatrix = Matrix4x4.Rotate(Quaternion.Euler(0, 0, -rotation.z));

            // Combine rotations (note: order of multiplication matters)
            Matrix4x4 rotationMatrix = rotationZMatrix * rotationYMatrix * rotationXMatrix;

            // Scale matrix
            Matrix4x4 scaleMatrix = Matrix4x4.Scale(Func.Inverse(scale));

            // Combine scale, rotation, and translation
            Matrix4x4 worldToLocalMatrix = rotationMatrix * scaleMatrix * translationMatrix;

            return worldToLocalMatrix;
        }

        // Hash key generation from Mesh
        public static string GetMeshKey(Mesh mesh)
        {
            if (mesh == null) return string.Empty;

            // Convert mesh data to strings or byte arrays
            StringBuilder sb = new StringBuilder();

            int i = 0;
            foreach (var vertex in mesh.vertices)
            {
                sb.Append(vertex.x).Append(vertex.y).Append(vertex.z);
                if (i++ >= 10) break;
            }

            i = 0;
            foreach (var normal in mesh.normals)
            {
                sb.Append(normal.x).Append(normal.y).Append(normal.z);
                if (i++ >= 10) break;
            }

            i = 0;
            foreach (var uv in mesh.uv)
            {
                sb.Append(uv.x).Append(uv.y);
                if (i++ >= 10) break;
            }

            i = 0;
            foreach (var triangle in mesh.triangles)
            {
                sb.Append(triangle);
                if (i++ >= 10) break;
            }

            // Convert the string builder content to a byte array
            byte[] meshDataBytes = Encoding.UTF8.GetBytes(sb.ToString());

            // Hash the byte array
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(meshDataBytes);
                StringBuilder hashString = new StringBuilder();

                // Convert hash bytes to a hexadecimal string
                for (int j = 0; j < hashBytes.Length; j++)
                {
                    hashString.Append(hashBytes[j].ToString("x2"));
                }

                // Return the hexadecimal string as the mesh key
                return hashString.ToString();
            }
        }

        // Helper method to compare arrays
        private static bool AreArraysEqual<T>(T[] arrayA, T[] arrayB)
        {
            if (arrayA.Length != arrayB.Length)
            {
                return false;
            }

            for (int i = 0; i < arrayA.Length; i++)
            {
                if (!EqualityComparer<T>.Default.Equals(arrayA[i], arrayB[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public static int GetMeshIndex(List<MeshData> meshArray, string meshKeyToCheck)
        {
            for (int i = 0; i < meshArray.Count; i++)
            {
                if (meshArray[i].meshKey == meshKeyToCheck) return i;
            }
            return -1;
        }

        public static void RemoveFromEndOfArray<T>(ref T[] originalArray, int x)
        {
            Array.Resize(ref originalArray, originalArray.Length - x);
        }
    }

    public class Func
    {
        public static void Log2(ref int a, bool doCeil = false)
        {
            double logValue = Math.Log(a, 2);
            a = doCeil ? (int)Math.Ceiling(logValue) : (int)logValue;
        }
        
        public static int Log2(int a, bool doCeil = false)
        {
            double logValue = Math.Log(a, 2);
            return doCeil ? (int)Math.Ceiling(logValue) : (int)logValue;
        }
        public static int Log2(float a, bool doCeil = false)
        {
            double logValue = Math.Log(a, 2);
            return doCeil ? (int)Math.Ceiling(logValue) : (int)logValue;
        }
        public static int Log2(ref float a, bool doCeil = false)
        {
            double logValue = Math.Log(a, 2);
            return doCeil ? (int)Math.Ceiling(logValue) : (int)logValue;
        }

        public static int Pow2(int a)
        {
            double powValue = Mathf.Pow(2, a);
            return (int)powValue;
        }

        /// <returns>returns a random integer between a min value (INCLUSIVE) and a max value (INCLUSIVE)</returns>
        public static int RandInt(int min, int max)
        {
            return UnityEngine.Random.Range(min, max+1);
        }

        /// <returns>returns a random float between a min value (INCLUSIVE) and a max value (INCLUSIVE)</returns>
        public static float RandFloat(float min, float max)
        {
            return UnityEngine.Random.Range(min, max+1);
        }

        public static int NextPow2(int a)
        {
            int nextPow2 = 1;
            while (nextPow2 < a)
            {
                nextPow2 *= 2;
            }
            return nextPow2;
        }

        public static void NextPow2(ref int a)
        {
            int nextPow2 = 1;
            while (nextPow2 < a)
            {
                nextPow2 *= 2;
            }
            a = nextPow2;
        }

        /// <summary>Calculates the logarithm (base 2) of the next power of 2</summary>
        public static int NextLog2(int a)
        {
            return Log2(NextPow2(a));
        }

        /// <summary>Calculates the logarithm (base 2) of the next power of 2</summary>
        public static void NextLog2(ref int a)
        {
            a = Log2(NextPow2(a));
        }

        /// <summary>Calculates the next integer divisible by a divisor</summary>
        public static void NextDivisible(ref int a, int divisor)
        {
            a = Mathf.CeilToInt(a / divisor) * divisor;
        }
        /// <summary>Calculates the next integer divisible by a divisor</summary>
        public static int NextDivisible(int a, int divisor)
        {
            return Mathf.CeilToInt(a / divisor) * divisor;
        }

        /// <summary>Calculates the uv coord for a point projected onto a triangle, with respect to the scale</summary>
        public static Vector2 TriUV(Vector3 a, Vector3 b, Vector3 c, Vector3 p, float scale)
        {
            // Calculate barycentric coordinates of point p with respect to triangle ABC
            Vector3 v0 = b - a;
            Vector3 v1 = c - a;
            Vector3 v2 = p - a;

            float dot00 = Vector3.Dot(v0, v0);
            float dot01 = Vector3.Dot(v0, v1);
            float dot02 = Vector3.Dot(v0, v2);
            float dot11 = Vector3.Dot(v1, v1);
            float dot12 = Vector3.Dot(v1, v2);

            float invDenom = 1 / (dot00 * dot11 - dot01 * dot01);
            float u = (dot11 * dot02 - dot01 * dot12) * invDenom;
            float v = (dot00 * dot12 - dot01 * dot02) * invDenom;
            float w = 1 - u - v;

            Vector2 uvA = new Vector2(0, 0); // UV coordinates for vertex a
            Vector2 uvB = new Vector2(1, 0); // UV coordinates for vertex b
            Vector2 uvC = new Vector2(0, 1); // UV coordinates for vertex c

            Vector2 uv = (u * uvA + v * uvB + w * uvC) * scale;
            uv.x = uv.x % 1.0f;
            uv.y = uv.y % 1.0f;

            return uv;
        }

        public static float[] DegreesToRadians(float[] degreesArray)
        {
            float[] radiansArray = new float[degreesArray.Length];
            for (int i = 0; i < degreesArray.Length; i++)
            {
                radiansArray[i] = degreesArray[i] * Mathf.Deg2Rad;
            }
            return radiansArray;
        }

        public static int MaxInt(params int[] inputArray)
        {
            int maxVal = inputArray[0];
            for (int i = 1; i < inputArray.Length; i++)
            {
                maxVal = Mathf.Max(maxVal, inputArray[i]);
            }

            return maxVal;
        }

        public static void SetToMaxInt(ref int input, params int[] inputArray)
        {
            int maxVal = inputArray[0];
            for (int i = 1; i < inputArray.Length; i++)
            {
                maxVal = Mathf.Max(maxVal, inputArray[i]);
            }

            input = maxVal;
        }

        public static float3 Avg(params float3[] inputArray)
        {
            float3 tot = 0;
            foreach (var input in inputArray)
            {
                tot += input;
            }
            float3 avg = tot / inputArray.Length;

            return avg;
        }

        public static Vector3 Inverse(Vector3 a)
        {
            return new Vector3(
                a.x != 0 ? 1.0f / a.x : 0,
                a.y != 0 ? 1.0f / a.y : 0,
                a.z != 0 ? 1.0f / a.z : 0
            );
        }

        public static float3 Mul(Matrix4x4 matrix, float3 a)
        {
            float4 transformedA = math.mul(matrix, new float4(a, 1.0f));
            return new float3(transformedA.x, transformedA.y, transformedA.z);
        }
    }

    public class DebugUtils
    {
        public static void ChildIndexValidation(int childIndex, int bvCount)
        {
            if (childIndex + 1 != bvCount) { Debug.Log("Faulty child index. BVs count: " + bvCount + ". Child index: " + childIndex); }
        }
        public static void LogStopWatch(string taskName, ref Stopwatch stopwatch)
        {
            stopwatch.Stop();
            Debug.Log(taskName + $" completed in {stopwatch.ElapsedMilliseconds} ms");
        }
    }
}