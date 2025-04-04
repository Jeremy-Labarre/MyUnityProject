using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class PlayerCarController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float acceleration = 1000f;         // Forward/backward force.
    public float steering = 30f;               // Turning speed.
    public float maxSpeed = 30f;               // Speed cap.
    public float collisionBounceFactor = 0.8f; // Speed retained after hitting walls.

    [Header("Four-Corner Slope Alignment")]
    [Tooltip("Local positions of the four corners (front-left, front-right, rear-left, rear-right).")]
    public Vector3[] cornerOffsets = new Vector3[]
    {
        new Vector3(-0.5f, 0f,  1f), // front-left.
        new Vector3( 0.5f, 0f,  1f), // front-right.
        new Vector3(-0.5f, 0f, -1f), // rear-left.
        new Vector3( 0.5f, 0f, -1f)  // rear-right.
    };
    [Tooltip("How far downward to cast rays for each corner.")]
    public float cornerRayDistance = 2f;
    [Tooltip("Layer(s) considered ground for slope alignment.")]
    public LayerMask groundLayer;
    [Tooltip("How quickly the car's rotation blends to the slope-aligned rotation.")]
    public float slopeAlignSpeed = 5f;

    [Header("Sliding Settings")]
    [Tooltip("Extra force that pushes the car down a steep slope.")]
    public float slideForceMultiplier = 5f;
    [Tooltip("Minimum slope angle (degrees) to apply sliding force.")]
    public float minSlopeAngle = 5f;
    [Tooltip("Distance to raycast from the center for sliding detection.")]
    public float slideRayDistance = 2f;

    [Header("Engine Rev Settings")]
    public bool canMove = true;         // Movement is locked until the race starts.
    public float engineRPM = 1000f;     // Starting RPM (idle).
    public float minRPM = 1000f;        // Minimum RPM.
    public float maxRPM = 5000f;        // Maximum rev limit.
    public float greenZoneMin = 3000f;  // Lower bound for the "perfect" rev zone.
    public float greenZoneMax = 4000f;  // Upper bound for the "perfect" rev zone.
    public float rpmIncreaseRate = 1200f;  // How fast RPM increases when revving.
    public float rpmDecreaseRate = 800f;   // How fast RPM decreases when not revving.

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Allow physics to handle gravity.
        rb.useGravity = true;
        // Smooth movement.
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        // Mild damping to prevent infinite sliding.
        rb.linearDamping = 0.1f;
        rb.angularDamping = 0.05f;
        // Lower center of mass for stability.
        rb.centerOfMass = new Vector3(0f, -0.5f, 0f);
        // Set collision detection mode.
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    void FixedUpdate()
    {
        // Update engine RPM regardless of movement.
        UpdateEngineRPM();

        // Only process movement if allowed.
        if (canMove)
        {
            HandleMovement();

            // Gather corner hits for slope alignment.
            List<Vector3> cornerHits = new List<Vector3>();
            for (int i = 0; i < cornerOffsets.Length; i++)
            {
                Vector3 cornerWorldPos = transform.TransformPoint(cornerOffsets[i]);
                RaycastHit hit;
                if (Physics.Raycast(cornerWorldPos, Vector3.down, out hit, cornerRayDistance, groundLayer))
                {
                    cornerHits.Add(hit.point);
                }
            }

            // Only align and slide if the car is well grounded.
            if (cornerHits.Count >= 3)
            {
                AlignToSlopeFourCorners(cornerHits);
                ApplySlidingForce();
            }
        }
    }

    void HandleMovement()
    {
        float moveInput = Input.GetAxis("Vertical");
        float steerInput = Input.GetAxis("Horizontal");

        // Steering
        if (Mathf.Abs(steerInput) > 0.01f)
        {
            float turn = steerInput * steering * Time.deltaTime;
            Quaternion turnRotation = Quaternion.Euler(0f, turn, 0f);
            rb.MoveRotation(rb.rotation * turnRotation);
        }

        // Forward/backward force (only if below max speed).
        if (Mathf.Abs(moveInput) > 0.1f && rb.linearVelocity.magnitude < maxSpeed)
        {
            rb.AddForce(transform.forward * moveInput * acceleration * Time.deltaTime);
        }
    }

    /// <summary>
    /// Aligns the car to a plane defined by three corner hits.
    /// </summary>
    void AlignToSlopeFourCorners(List<Vector3> cornerHits)
    {
        if (cornerHits.Count < 3) return;

        Plane plane = BestFitPlaneFrom3Points(cornerHits[0], cornerHits[1], cornerHits[2]);
        Vector3 planeNormal = plane.normal.normalized;
        Vector3 forwardOnPlane = Vector3.ProjectOnPlane(transform.forward, planeNormal).normalized;
        if (forwardOnPlane.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(forwardOnPlane, planeNormal);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, Time.deltaTime * slopeAlignSpeed));
    }

    /// <summary>
    /// Applies an extra sliding force if the slope is steep.
    /// </summary>
    void ApplySlidingForce()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, slideRayDistance, groundLayer))
        {
            float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
            if (slopeAngle > minSlopeAngle)
            {
                Vector3 gravity = Physics.gravity;
                Vector3 slidingDir = Vector3.ProjectOnPlane(gravity, hit.normal).normalized;
                rb.AddForce(slidingDir * slideForceMultiplier, ForceMode.Acceleration);
            }
        }
    }

    /// <summary>
    /// Returns a plane defined by three points.
    /// </summary>
    Plane BestFitPlaneFrom3Points(Vector3 p0, Vector3 p1, Vector3 p2)
    {
        Vector3 normal = Vector3.Cross(p1 - p0, p2 - p0).normalized;
        return new Plane(normal, p0);
    }

    /// <summary>
    /// Updates the engine RPM based on the player's input.
    /// </summary>
    private void UpdateEngineRPM()
    {
        float moveInput = Input.GetAxis("Vertical");

        // Increase RPM when accelerating.
        if (moveInput > 0.1f)
        {
            engineRPM += rpmIncreaseRate * Time.deltaTime;
        }
        else
        {
            // Decrease RPM back toward idle.
            engineRPM -= rpmDecreaseRate * Time.deltaTime;
        }
        engineRPM = Mathf.Clamp(engineRPM, minRPM, maxRPM);
    }

    /// <summary>
    /// Checks if the current RPM is within the "green" boost zone.
    /// </summary>
    public bool IsInGreenZone()
    {
        return engineRPM >= greenZoneMin && engineRPM <= greenZoneMax;
    }

    /// <summary>
    /// Applies an immediate boost force when the race starts.
    /// </summary>
    public void ApplyBoost(float boostForce)
    {
        rb.AddForce(transform.forward * boostForce, ForceMode.Impulse);
    }

    /// <summary>
    /// Called when colliding with objects tagged as "Wall".
    /// </summary>
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            Vector3 reflection = Vector3.Reflect(rb.linearVelocity, collision.contacts[0].normal);
            float newSpeed = reflection.magnitude * collisionBounceFactor;
            newSpeed = Mathf.Min(newSpeed, maxSpeed);
            rb.linearVelocity = reflection.normalized * newSpeed;
        }
    }
}
