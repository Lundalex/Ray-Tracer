using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Unity.Mathematics;
using UnityEngine;

public static class FileLoader
{
    // --- .json (multi-file structure) ---

    public static void SaveArrayToJsonFile<T>(T[] array, string folderPath, string fileName)
    {
        string path = Path.Combine(folderPath, fileName + ".json");

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string json = JsonUtility.ToJson(new Wrapper<T>(array), true);
        File.WriteAllText(path, json);
        Debug.Log("Array saved to " + path);
    }

    public static void SaveMultiArrayContainerToJsonFiles(string baseFileName, MultiArrayContainer container)
    {
        string folderPath = Path.Combine(Application.persistentDataPath, baseFileName);

        SaveArrayToJsonFile(container.loadedTriangles, folderPath, "loadedTriangles");
        SaveArrayToJsonFile(container.sceneObjectDatas, folderPath, "sceneObjectDatas");
        SaveArrayToJsonFile(container.lightObjects, folderPath, "lightObjects");
        SaveArrayToJsonFile(container.loadedMeshesLookup, folderPath, "loadedMeshesLookup");
        SaveArrayToJsonFile(container.loadedComponentDatas, folderPath, "loadedComponentDatas");
        SaveArrayToJsonFile(container.loadedMeshes.ToArray(), folderPath, "loadedMeshes");
        SaveArrayToJsonFile(container.loadedBVs, folderPath, "loadedBVs");
        SaveArrayToJsonFile(container.loadedVertices, folderPath, "loadedVertices");
        SaveArrayToJsonFile(container.integers, folderPath, "integers");
        SaveArrayToJsonFile(container.floats, folderPath, "floats");
        SaveArrayToJsonFile(container.renderBVs, folderPath, "renderBVs");
        SaveArrayToJsonFile(container.renderTriangles, folderPath, "renderTriangles");

        Debug.Log("MultiArrayContainer saved to folder " + folderPath);
    }

    public static T[] LoadArrayFromJsonFile<T>(string folderPath, string fileName)
    {
        string path = Path.Combine(folderPath, fileName + ".json");

        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(json);
            return wrapper.array;
        }
        else
        {
            Debug.LogError("File not found: " + path);
            return null;
        }
    }

    public static MultiArrayContainer LoadMultiArrayContainerFromJsonFiles(string baseFileName)
    {
        string folderPath = Path.Combine(Application.persistentDataPath, baseFileName);

        if (!Directory.Exists(folderPath))
        {
            Debug.LogError("Directory not found: " + folderPath);
            return null;
        }

        var container = new MultiArrayContainer
        {
            loadedTriangles = LoadArrayFromJsonFile<Triangle>(folderPath, "loadedTriangles"),
            sceneObjectDatas = LoadArrayFromJsonFile<SceneObjectData>(folderPath, "sceneObjectDatas"),
            lightObjects = LoadArrayFromJsonFile<LightObject>(folderPath, "lightObjects"),
            loadedMeshesLookup = LoadArrayFromJsonFile<int>(folderPath, "loadedMeshesLookup"),
            loadedComponentDatas = LoadArrayFromJsonFile<int2>(folderPath, "loadedComponentDatas"),
            loadedMeshes = new List<MeshData>(LoadArrayFromJsonFile<MeshData>(folderPath, "loadedMeshes")),
            loadedBVs = LoadArrayFromJsonFile<RenderBV>(folderPath, "loadedBVs"),
            loadedVertices = LoadArrayFromJsonFile<Vertex>(folderPath, "loadedVertices"),
            integers = LoadArrayFromJsonFile<KeyedInt>(folderPath, "integers"),
            floats = LoadArrayFromJsonFile<KeyedFloat>(folderPath, "floats"),
            renderBVs = LoadArrayFromJsonFile<RenderBV>(folderPath, "renderBVs"),
            renderTriangles = LoadArrayFromJsonFile<RenderTriangle>(folderPath, "renderTriangles")
        };

        Debug.Log("MultiArrayContainer loaded from folder " + folderPath);
        return container;
    }

    // --- .bin (single-file structure) ---

    public static void SaveArrayToBinFile<T>(T[] array, string fileName)
    {
        string path = Path.Combine(Application.persistentDataPath, fileName + ".bin");

        try
        {
            using (FileStream fileStream = new FileStream(path, FileMode.Create))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(fileStream, array);
            }

            Debug.Log("Array saved to " + path);
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to save array: " + e.Message);
        }
    }

    // Load array from a binary file
    public static T[] LoadArrayFromBinFile<T>(string fileName)
    {
        string path = Path.Combine(Application.persistentDataPath, fileName + ".bin");

        if (File.Exists(path))
        {
            try
            {
                using (FileStream fileStream = new FileStream(path, FileMode.Open))
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    return (T[])formatter.Deserialize(fileStream);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to load array: " + e.Message);
                return null;
            }
        }
        else
        {
            Debug.LogError("File not found: " + path);
            return null;
        }
    }

    // Save a container of multiple arrays to a binary file
    public static void SaveArraysToBinFile(string fileName, MultiArrayContainer container)
    {
        string path = Path.Combine(Application.persistentDataPath, fileName + ".bin");

        try
        {
            using (FileStream fileStream = new FileStream(path, FileMode.Create))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(fileStream, container);
            }

            Debug.Log("Arrays saved to " + path);
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to save arrays: " + e.Message);
        }
    }

    // Load a container of multiple arrays from a binary file
    public static MultiArrayContainer LoadArraysFromBinFile(string fileName)
    {
        string path = Path.Combine(Application.persistentDataPath, fileName + ".bin");

        if (File.Exists(path))
        {
            try
            {
                using (FileStream fileStream = new FileStream(path, FileMode.Open))
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    return (MultiArrayContainer)formatter.Deserialize(fileStream);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to load arrays: " + e.Message);
                return null;
            }
        }
        else
        {
            Debug.LogError("File not found: " + path);
            return null;
        }
    }
}
