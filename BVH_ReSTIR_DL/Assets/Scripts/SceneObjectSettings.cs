using UnityEngine;
using Unity.Mathematics;

// Import utils from Resources.cs
using Resources;
// Usage: Utils.(functionName)()

// All position and rotation settings for scene objects
public class SceneObjectSettings : MonoBehaviour
{
    public int MaterialIndex;
    public int MaxDepthBVH;
    private Main m;
    private Vector3 lastCameraPosition;
    private Quaternion lastCameraRotation;

    private void Start()
    {
        lastCameraPosition = transform.position;
        lastCameraRotation = transform.rotation;
    }

    private void OnValidate()
    {
        m = m != null ? m : GameObject.Find("Main Camera").GetComponent<Main>();
        if (m.ProgramStarted) m.DoUpdateSettings = true;
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