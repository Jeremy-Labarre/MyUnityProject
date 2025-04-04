using UnityEngine;
using System.Collections.Generic;

public class WaypointSystem : MonoBehaviour
{
    public List<Transform> waypoints = new List<Transform>();

    void Awake()
    {
        foreach (Transform child in transform)
        {
            waypoints.Add(child);
        }
        Debug.Log("Waypoints count: " + waypoints.Count);
    }

    // This will draw a small sphere in the Scene view for each waypoint.
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        foreach (Transform child in transform)
        {
            Gizmos.DrawSphere(child.position, 0.5f);
        }
    }
}
