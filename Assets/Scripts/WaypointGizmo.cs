using UnityEngine;

public class WaypointGizmo : MonoBehaviour

{


    // Draws a yellow sphere to mark the waypoint in the Scene view.
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position, 0.5f);
    }
}
