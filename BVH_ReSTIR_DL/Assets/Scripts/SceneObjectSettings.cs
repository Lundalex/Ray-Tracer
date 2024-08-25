using UnityEngine;

// All position and rotation settings for scene objects
public class SceneObjectSettings : MonoBehaviour
{
    public int MaterialIndex;
    public int MaxDepthBVH;
    private Main m;
    private Vector3 lastPosition;
    private Quaternion lastRotation;

    private void Start()
    {
        lastPosition = transform.position;
        lastRotation = transform.rotation;
    }

    private void OnValidate()
    {
        if (m == null)
        {
            m = GameObject.Find("Main Camera").GetComponent<Main>();
        }

        if (m.ProgramStarted) { m.DoUpdateSettings = true;  m.DoReloadData = true; }
    }
    private void LateUpdate()
    {
        if (transform.position != lastPosition || transform.rotation != lastRotation)
        {
            m.DoUpdateSettings = true;
            m.DoReloadData = true;
            lastPosition = transform.position;
            lastRotation = transform.rotation;
        }
    }
}