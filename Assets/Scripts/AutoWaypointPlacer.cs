using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[RequireComponent(typeof(BoxCollider))]
public class AutoWaypointGenerator : MonoBehaviour
{
    [Header("Waypoint Settings")]
    [Tooltip("Prefab for the waypoint marker (should be a visible object in Edit mode).")]
    public GameObject waypointPrefab;
    [Tooltip("Distance between waypoints along the track's centerline (in world units).")]
    public float spacing = 3f;
    [Tooltip("If true, clear existing waypoints (children under waypointParent) before generating.")]
    public bool clearExisting = true;
    [Tooltip("Parent transform to store generated waypoints. If null, waypoints are created as children of this GameObject.")]
    public Transform waypointParent;

    [Header("Generation Mode")]
    [Tooltip("If true, use manual start and end points instead of the BoxCollider.")]
    public bool useManualPoints = false;
    [Tooltip("Transform marking the start of the track.")]
    public Transform manualStartPoint;
    [Tooltip("Transform marking the end of the track.")]
    public Transform manualEndPoint;

    [Header("Auto Generation Options")]
    [Tooltip("Automatically generate waypoints when in Edit mode or at Start.")]
    public bool generateOnStart = true;

    // Context menu button for manual generation.
    [ContextMenu("Generate Waypoints")]
    public void GenerateWaypoints()
    {
        Transform parent = (waypointParent != null) ? waypointParent : transform;

        // Clear existing waypoints if desired.
#if UNITY_EDITOR
        if (clearExisting)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(parent.GetChild(i).gameObject);
            }
        }
#else
        if (clearExisting)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Destroy(parent.GetChild(i).gameObject);
            }
        }
#endif

        Vector3 startPoint, endPoint;
        if (useManualPoints)
        {
            if (manualStartPoint == null || manualEndPoint == null)
            {
                Debug.LogError("Manual start or end point not assigned.");
                return;
            }
            startPoint = manualStartPoint.position;
            endPoint = manualEndPoint.position;
        }
        else
        {
            BoxCollider box = GetComponent<BoxCollider>();
            if (box == null)
            {
                Debug.LogError("AutoWaypointGenerator requires a BoxCollider when not using manual points.");
                return;
            }
            // Use the BoxCollider's center and size along local Z.
            Vector3 localCenter = box.center;
            float halfLength = box.size.z * 0.5f;
            Vector3 worldCenter = transform.TransformPoint(localCenter);
            Vector3 forward = transform.forward;
            startPoint = worldCenter - forward * halfLength;
            endPoint = worldCenter + forward * halfLength;
        }

        float totalDistance = Vector3.Distance(startPoint, endPoint);
        int waypointCount = Mathf.FloorToInt(totalDistance / spacing) + 1;
        if (waypointCount < 2)
            waypointCount = 2;

        for (int i = 0; i < waypointCount; i++)
        {
            float t = i / (float)(waypointCount - 1);
            Vector3 pos = Vector3.Lerp(startPoint, endPoint, t);
            GameObject wp = Instantiate(waypointPrefab, pos, Quaternion.identity, parent);
            wp.name = "Waypoint " + i;
        }

        Debug.Log("Generated " + waypointCount + " waypoints from " + startPoint + " to " + endPoint);
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Transform parent = (waypointParent != null) ? waypointParent : transform;
        Gizmos.color = Color.green;
        foreach (Transform child in parent)
        {
            Gizmos.DrawSphere(child.position, 0.3f);
        }
    }

    void OnValidate()
    {
        if (!Application.isPlaying && generateOnStart)
        {
            EditorApplication.delayCall -= DelayedGenerate;
            EditorApplication.delayCall += DelayedGenerate;
        }
    }

    void DelayedGenerate()
    {
        if (this == null) return;
        if (!Application.isPlaying && generateOnStart)
        {
            GenerateWaypoints();
        }
    }
#endif

    void Start()
    {
        if (generateOnStart)
        {
            GenerateWaypoints();
        }
    }
}
