using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class PlayerCarController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float acceleration = 1000f;         // Forward/backward force
    public float steering = 30f;               // Maximum turning speed at low speed
    public float minSteering = 10f;            // Minimum turning speed at high speed
    public float speedForMinSteering = 30f;    // Speed at which steering is at its minimum
    public float maxSpeed = 30f;               // Speed cap
    public float collisionBounceFactor = 0.8f; // Speed retained after hitting walls

    [Header("Four-Corner Slope Alignment")]
    [Tooltip("Local positions of the four corners (front-left, front-right, rear-left, rear-right).")]
    public Vector3[] cornerOffsets = new Vector3[]
    {
        new Vector3(-0.5f, 0f,  1f),
        new Vector3( 0.5f, 0f,  1f),
        new Vector3(-0.5f, 0f, -1f),
        new Vector3( 0.5f, 0f, -1f)
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
    public bool canMove = true;
    public float engineRPM = 1000f;
    public float minRPM = 1f;
    public float maxRPM = 12f;
    public float greenZoneMin = 10.5f;
    public float greenZoneMax = 11.5f;
    public float rpmIncreaseRate = 1f;
    public float rpmDecreaseRate = 0.5f;

    [Header("Air Control Settings")]
    [Tooltip("Extra downward force applied when the car is not grounded.")]
    public float extraGravityForce = 50f; // Tweak to taste

    [Header("Respawn Settings")]
    public Transform respawnPoint;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.linearDamping = 0.1f;
        rb.angularDamping = 0.05f;
        rb.centerOfMass = new Vector3(0f, -0.5f, 0f);

        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    void FixedUpdate()
    {
        UpdateEngineRPM();

        if (canMove)
        {
            HandleMovement();

            // -- 1) Check how many corners are grounded.
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

            // -- 2) If at least 3 corners are grounded, slope-align and slide.
            if (cornerHits.Count >= 3)
            {
                AlignToSlopeFourCorners(cornerHits);
                ApplySlidingForce();
            }
            else
            {
                // If fewer than 3 corners are grounded, we're effectively in the air.
                // Apply extra downward force to make the car less floaty.
                rb.AddForce(Vector3.down * extraGravityForce, ForceMode.Acceleration);
            }
        }
    }

    /// <summary>
    /// Raises or lowers RPM based on player input, clamped to [minRPM..maxRPM].
    /// </summary>
    private void UpdateEngineRPM()
    {
        float moveInput = Input.GetAxis("Vertical");

        // Increase if throttle pressed, decrease if not.
        if (moveInput > 0.1f)
        {
            engineRPM += rpmIncreaseRate * Time.deltaTime;
        }
        else
        {
            engineRPM -= rpmDecreaseRate * Time.deltaTime;
        }

        engineRPM = Mathf.Clamp(engineRPM, minRPM, maxRPM);
    }

    /// <summary>
    /// Handles forward/back input, plus speed-based steering.
    /// The faster the car, the smaller the steering angle.
    /// </summary>
    private void HandleMovement()
    {
        float moveInput = Input.GetAxis("Vertical");
        float steerInput = Input.GetAxis("Horizontal");

        // Speed-based steering
        float speed = rb.linearVelocity.magnitude;
        float t = Mathf.Clamp01(speed / speedForMinSteering);
        float currentSteering = Mathf.Lerp(steering, minSteering, t);

        // Steering
        if (Mathf.Abs(steerInput) > 0.01f)
        {
            float turn = steerInput * currentSteering * Time.deltaTime;
            Quaternion turnRotation = Quaternion.Euler(0f, turn, 0f);
            rb.MoveRotation(rb.rotation * turnRotation);
        }

        // Forward/backward force
        if (Mathf.Abs(moveInput) > 0.1f && rb.linearVelocity.magnitude < maxSpeed)
        {
            rb.AddForce(transform.forward * moveInput * acceleration * Time.deltaTime);
        }
    }

    private void AlignToSlopeFourCorners(List<Vector3> cornerHits)
    {
        if (cornerHits.Count < 3) return;

        Plane plane = BestFitPlaneFrom3Points(cornerHits[0], cornerHits[1], cornerHits[2]);
        Vector3 planeNormal = plane.normal.normalized;
        Vector3 forwardOnPlane = Vector3.ProjectOnPlane(transform.forward, planeNormal).normalized;
        if (forwardOnPlane.sqrMagnitude < 0.001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(forwardOnPlane, planeNormal);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, Time.deltaTime * slopeAlignSpeed));
    }

    private void ApplySlidingForce()
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

    private Plane BestFitPlaneFrom3Points(Vector3 p0, Vector3 p1, Vector3 p2)
    {
        Vector3 normal = Vector3.Cross(p1 - p0, p2 - p0).normalized;
        return new Plane(normal, p0);
    }

    public bool IsInGreenZone()
    {
        return engineRPM >= greenZoneMin && engineRPM <= greenZoneMax;
    }

    public void ApplyBoost(float boostForce)
    {
        rb.AddForce(transform.forward * boostForce, ForceMode.Impulse);
    }

    public void Respawn()
    {
        if (respawnPoint != null)
        {
            transform.position = respawnPoint.position;
            transform.rotation = respawnPoint.rotation;
        }
        else
        {
            Debug.LogWarning("No respawn point assigned in PlayerCarController.");
        }
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        engineRPM = minRPM;
        canMove = true;
    }

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
