using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class EnemyAIController : MonoBehaviour
{
    [Header("Waypoints & Movement")]
    [Tooltip("Parent transform whose children are the waypoints.")]
    public Transform waypointParent;

    [Tooltip("Movement speed (forward force).")]
    public float moveSpeed = 10f;

    [Tooltip("Turning speed for horizontal rotation.")]
    public float turnSpeed = 5f;

    [Tooltip("Distance to consider a waypoint reached.")]
    public float closeEnoughDistance = 1f;

    [Header("Slope Alignment")]
    [Tooltip("How quickly the enemy car aligns with the slope.")]
    public float slopeAlignSpeed = 5f;

    [Tooltip("Distance to raycast downward for ground detection.")]
    public float slopeRaycastDistance = 2f;

    [Tooltip("Layers considered as ground.")]
    public LayerMask groundLayer;

    [Header("Respawn Settings")]
    [Tooltip("If the enemy's Y position falls below this value, it respawns.")]
    public float voidThreshold = -10f;

    [Tooltip("Where to respawn the enemy if it falls. If null, respawns at the first waypoint.")]
    public Transform respawnPoint;

    [Header("Push Settings")]
    [Tooltip("Force applied to push the enemy off the track when colliding with the player's car.")]
    public float pushForce = 500f;

    [Tooltip("Duration (in seconds) over which the push is applied.")]
    public float pushDuration = 0.5f;

    // Internals
    private Rigidbody rb;
    private List<Transform> waypoints = new List<Transform>();
    private int currentWaypointIndex = 0;
    private bool finished = false;
    private bool isBeingPushed = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        CollectWaypoints();

        // Start at the first waypoint if available.
        if (waypoints.Count > 0)
        {
            currentWaypointIndex = 0;
            transform.position = waypoints[0].position + Vector3.up;
        }
    }

    void FixedUpdate()
    {
        // Respawn if enemy falls below threshold.
        if (transform.position.y < voidThreshold)
        {
            RespawnEnemy();
            return;
        }

        // Wait until the race starts.
        if (!RaceStartManager.raceStarted)
            return;

        // Stop movement if finished following waypoints.
        if (finished)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            return;
        }

        AlignToSlope();
        MoveTowardsCurrentWaypoint();
    }

    // Collects all children from the waypoint parent.
    private void CollectWaypoints()
    {
        waypoints.Clear();
        if (waypointParent == null)
        {
            Debug.LogWarning("EnemyAIController: No waypoint parent assigned.");
            return;
        }
        foreach (Transform child in waypointParent)
        {
            if (child != null)
                waypoints.Add(child);
        }
        if (waypoints.Count > 0)
            currentWaypointIndex = 0;
    }

    // Moves the enemy toward the current waypoint.
    private void MoveTowardsCurrentWaypoint()
    {
        if (waypoints.Count == 0)
            return;

        if (waypoints[currentWaypointIndex] == null)
        {
            Debug.Log("Current waypoint is missing. Recollecting waypoints.");
            CollectWaypoints();
            if (waypoints.Count == 0)
                return;
            if (currentWaypointIndex >= waypoints.Count)
                currentWaypointIndex = 0;
        }

        Transform target = waypoints[currentWaypointIndex];
        Vector3 toTarget = target.position - transform.position;
        Vector3 flatToTarget = Vector3.ProjectOnPlane(toTarget, Vector3.up);

        // If close enough, move to next waypoint.
        if (flatToTarget.magnitude < closeEnoughDistance)
        {
            currentWaypointIndex++;
            if (currentWaypointIndex >= waypoints.Count)
            {
                finished = true;
                return;
            }
            target = waypoints[currentWaypointIndex];
            toTarget = target.position - transform.position;
            flatToTarget = Vector3.ProjectOnPlane(toTarget, Vector3.up);
        }

        // Rotate towards the target.
        if (flatToTarget.sqrMagnitude > 0.001f)
        {
            Quaternion desiredRotation = Quaternion.LookRotation(flatToTarget, Vector3.up);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, desiredRotation, turnSpeed * Time.fixedDeltaTime));
        }

        // Apply forward force.
        rb.AddForce(transform.forward * moveSpeed, ForceMode.Force);
    }

    // Aligns the enemy's rotation to the slope of the track using a downward raycast.
    private void AlignToSlope()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, -Vector3.up, out hit, slopeRaycastDistance, groundLayer))
        {
            Vector3 groundNormal = hit.normal;
            Vector3 currentForward = transform.forward;
            Vector3 forwardOnSlope = Vector3.ProjectOnPlane(currentForward, groundNormal).normalized;
            if (forwardOnSlope.sqrMagnitude > 0.01f)
            {
                Quaternion slopeRotation = Quaternion.LookRotation(forwardOnSlope, groundNormal);
                rb.MoveRotation(Quaternion.Slerp(rb.rotation, slopeRotation, slopeAlignSpeed * Time.fixedDeltaTime));
            }
        }
    }

    // Respawns the enemy at the respawn point or at the first waypoint.
    private void RespawnEnemy()
    {
        Debug.Log("Respawning enemy...");
        if (respawnPoint == null && waypoints.Count > 0)
        {
            transform.position = waypoints[0].position + Vector3.up;
            transform.rotation = Quaternion.identity;
        }
        else if (respawnPoint != null)
        {
            transform.position = respawnPoint.position;
            transform.rotation = respawnPoint.rotation;
        }
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        finished = false;
        currentWaypointIndex = 0;
    }

    // When colliding with the player's car, start a gradual push.
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player") && !isBeingPushed)
        {
            // Get the contact point and determine the push direction.
            ContactPoint contact = collision.contacts[0];
            Vector3 pushDirection = contact.normal;

            // Apply an initial impulse to kick things off.
            rb.AddForce(pushDirection * (pushForce * 0.3f), ForceMode.Impulse);
            StartCoroutine(GradualPush(pushDirection));
        }
    }

    // Coroutine to gradually apply push force over pushDuration seconds.
    private IEnumerator GradualPush(Vector3 pushDirection)
    {
        isBeingPushed = true;
        float elapsedTime = 0f;
        while (elapsedTime < pushDuration)
        {
            // Apply continuous force each frame.
            rb.AddForce(pushDirection * (pushForce * Time.deltaTime), ForceMode.Impulse);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        isBeingPushed = false;
    }
}
