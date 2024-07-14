using UnityEngine;
using Unity.Mathematics;

// Import utils from Resources.cs
using Resources;
// Usage: Utils.(functionName)()

// All position and rotation settings for scene objects
public class SceneObjectSettings : MonoBehaviour
{
    public int MaterialKey;
    public int MaxDepthBVH; // Not yet integrated
    private bool ProgramStarted = false;
    private Main m;
    private Vector3 lastCameraPosition;
    private Quaternion lastCameraRotation;

    private void Start()
    {
        GameObject MainCameraObject = GameObject.Find("Main Camera");
        m = MainCameraObject.GetComponent<Main>();

        lastCameraPosition = transform.position;
        lastCameraRotation = transform.rotation;

        ProgramStarted = true;
    }

    private void OnValidate()
    {
        if (ProgramStarted)
        {
            m.DoUpdateSettings = true;
        }
    }
    private void LateUpdate()
    {
        if (transform.position != lastCameraPosition || transform.rotation != lastCameraRotation)
        {
            m.DoUpdateSettings = true;
            lastCameraPosition = transform.position;
            lastCameraRotation = transform.rotation;
        }
    }
}