using UnityEngine;
using UnityEngine.Splines;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class SplineWaypointGenerator : MonoBehaviour
{
    [Header("Spline Reference")]
    [Tooltip("The SplineContainer holding the track's spline.")]
    public SplineContainer splineContainer;

    [Header("Waypoint Settings")]
    [Tooltip("Prefab for the waypoint marker (should be visible in the Scene view).")]
    public GameObject waypointPrefab;
    [Tooltip("Spacing (in world units) between waypoints along the spline.")]
    public float spacing = 3f;
    [Tooltip("Parent transform to hold generated waypoints. If null, they become children of this GameObject.")]
    public Transform waypointParent;
    [Tooltip("Clear existing waypoints before generating.")]
    public bool clearExisting = true;
    [Tooltip("Automatically generate waypoints on Start and in Edit mode.")]
    public bool generateOnStart = true;

    // Context menu option to manually generate waypoints.
    [ContextMenu("Generate Waypoints")]
    public void GenerateWaypoints()
    {
        if (splineContainer == null)
        {
            Debug.LogError("SplineWaypointGenerator: No SplineContainer assigned.");
            return;
        }

        Transform parent = waypointParent != null ? waypointParent : transform;

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

        // Retrieve the spline from the SplineContainer.
        Spline spline = splineContainer.Spline;
        if (spline == null)
        {
            Debug.LogError("SplineWaypointGenerator: SplineContainer has no spline assigned.");
            return;
        }

        // Calculate the total spline length by sampling.
        int sampleCount = 100;
        float totalLength = CalculateSplineLength(spline, sampleCount);

        // Determine the number of waypoints.
        int waypointCount = Mathf.FloorToInt(totalLength / spacing) + 1;
        if (waypointCount < 2)
            waypointCount = 2;

        for (int i = 0; i < waypointCount; i++)
        {
            float distance = i * spacing;
            if (distance > totalLength)
                distance = totalLength;
            // Normalize distance to a t value in [0,1].
            float t = distance / totalLength;
            // Evaluate the spline's position in local space...
            Vector3 localPos = spline.EvaluatePosition(t);
            // ... then convert it to world space.
            Vector3 worldPos = splineContainer.transform.TransformPoint(localPos);
            GameObject wp = Instantiate(waypointPrefab, worldPos, Quaternion.identity, parent);
            wp.name = "Waypoint " + i;
        }

        Debug.Log("Generated " + waypointCount + " waypoints along the spline.");
    }

    float CalculateSplineLength(Spline spline, int sampleCount)
    {
        float length = 0f;
        Vector3 prev = spline.EvaluatePosition(0f);
        for (int i = 1; i <= sampleCount; i++)
        {
            float t = i / (float)sampleCount;
            Vector3 curr = spline.EvaluatePosition(t);
            length += Vector3.Distance(prev, curr);
            prev = curr;
        }
        return length;
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Transform parent = waypointParent != null ? waypointParent : transform;
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
        if (this == null)
            return;
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
