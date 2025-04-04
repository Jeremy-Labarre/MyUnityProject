using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("References")]
    public Transform target; // Your player car Transform.
    private Camera cam;

    [Header("Camera Offsets")]
    public Vector3 defaultOffset = new Vector3(0f, 7f, -12f);

    [Header("Follow Speeds")]
    public float followSpeed = 8f;             // Base follow speed.
    public float offsetTransitionSpeed = 2f;   // How fast the offset transitions.

    [Header("Dynamic Lag Settings")]
    [Tooltip("Divider used to reduce follow speed as car speed increases.")]
    public float lagSpeedDivider = 40f;
    [Tooltip("The minimum follow speed, even at high speeds.")]
    public float minFollowSpeed = 2f;

    [Header("Ground Collision Settings")]
    public LayerMask groundMask;               // Assign this to the "Ground" layer in the Inspector.
    public float extraHeight = 0.5f;           // Height above ground when collision is detected.

    [Header("FOV Stretch Effect")]
    [Tooltip("Base Field of View (FOV) when the car is at low speed.")]
    public float baseFOV = 60f;
    [Tooltip("Maximum Field of View when the car is at high speed.")]
    public float maxFOV = 80f;
    [Tooltip("Speed above which the FOV effect starts.")]
    public float fovSpeedThreshold = 10f;
    [Tooltip("Speed at which the FOV is fully stretched.")]
    public float fovMaxSpeed = 30f;
    [Tooltip("How quickly the FOV transitions to its target value.")]
    public float fovTransitionSpeed = 2f;

    [Header("Drift Effect")]
    [Tooltip("Angle (in degrees) above which drifting is detected.")]
    public float driftAngleThreshold = 15f;
    [Tooltip("Lateral offset applied when drifting.")]
    public float driftLateralOffset = 1.5f;

    private Vector3 currentOffset;

    void Start()
    {
        if (target != null)
        {
            currentOffset = defaultOffset;
        }

        // Cache the Camera component.
        cam = GetComponent<Camera>();
        if (cam != null)
        {
            cam.fieldOfView = baseFOV;
        }
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        Rigidbody targetRb = target.GetComponent<Rigidbody>();
        float speed = targetRb ? targetRb.linearVelocity.magnitude : 0f;

        // ---------------------------------------------------------------------
        // 1) FOV Stretch Effect: Increase the camera's Field of View (FOV)
        //    as the car speeds up to enhance the sensation of high speed.
        // ---------------------------------------------------------------------
        if (cam != null)
        {
            float fovLerp = 0f;
            if (speed > fovSpeedThreshold)
            {
                fovLerp = Mathf.Clamp01((speed - fovSpeedThreshold) / (fovMaxSpeed - fovSpeedThreshold));
            }
            float desiredFOV = Mathf.Lerp(baseFOV, maxFOV, fovLerp);
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, desiredFOV, fovTransitionSpeed * Time.deltaTime);
        }

        // ---------------------------------------------------------------------
        // 2) Calculate the desired offset based solely on the target's rotation.
        //    (We no longer modify vertical offset for speed.)
        // ---------------------------------------------------------------------
        Vector3 desiredOffset = target.rotation * defaultOffset;

        // ---------------------------------------------------------------------
        // 3) Drift Effect: If the car is drifting (its forward direction and
        //    velocity direction differ), adjust the offset laterally.
        // ---------------------------------------------------------------------
        if (targetRb != null && speed > fovSpeedThreshold)
        {
            Vector3 velocityDir = targetRb.linearVelocity.normalized;
            float driftAngle = Vector3.Angle(target.forward, velocityDir);
            if (driftAngle > driftAngleThreshold)
            {
                // Use cross product to determine left/right direction.
                float sign = Mathf.Sign(Vector3.Cross(target.forward, velocityDir).y);
                desiredOffset += target.right * driftLateralOffset * sign;
            }
        }

        // ---------------------------------------------------------------------
        // 4) Smoothly transition the current offset toward the desired offset.
        // ---------------------------------------------------------------------
        currentOffset = Vector3.Lerp(currentOffset, desiredOffset, offsetTransitionSpeed * Time.deltaTime);

        // ---------------------------------------------------------------------
        // 5) Dynamic Lag: Adjust the follow speed based on the car's speed.
        //    At higher speeds the camera will "lag" behind more.
        // ---------------------------------------------------------------------
        float adjustedFollowSpeed = followSpeed;
        if (targetRb != null)
        {
            adjustedFollowSpeed = Mathf.Max(minFollowSpeed, followSpeed - (speed / lagSpeedDivider));
        }

        // ---------------------------------------------------------------------
        // 6) Determine the desired camera position and smooth the movement.
        // ---------------------------------------------------------------------
        Vector3 desiredPosition = target.position + currentOffset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, adjustedFollowSpeed * Time.deltaTime);

        // ---------------------------------------------------------------------
        // 7) Ground Collision: Ensure the camera doesn't clip into the terrain.
        // ---------------------------------------------------------------------
        RaycastHit hit;
        if (Physics.Linecast(target.position, smoothedPosition, out hit, groundMask))
        {
            smoothedPosition = hit.point + Vector3.up * extraHeight;
        }

        // ---------------------------------------------------------------------
        // 8) Apply the final position and make the camera look at the target.
        // ---------------------------------------------------------------------
        transform.position = smoothedPosition;
        transform.LookAt(target);
    }
}
