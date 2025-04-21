using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("References")]
    public Transform target; // Player car Transform
    private Camera cam;
    private Vector3 smoothDampVelocity; // For SmoothDamp position smoothing
    private float currentRotationVelocity; // For SmoothDampAngle on rotation (if we choose that approach)

    [Header("Camera Offsets")]
    public Vector3 defaultOffset = new Vector3(0f, 7f, -12f);

    [Header("Follow Speeds")]
    public float followSpeed = 8f;             // Base follow speed
    public float offsetTransitionSpeed = 2f;   // How fast the offset transitions

    [Header("Dynamic Lag Settings")]
    public float lagSpeedDivider = 40f;
    public float minFollowSpeed = 2f;

    [Header("Ground Collision Settings")]
    public LayerMask groundMask;
    public float extraHeight = 0.5f;

    [Header("FOV Stretch Effect")]
    public float baseFOV = 60f;
    public float maxFOV = 80f;
    public float fovSpeedThreshold = 10f;
    public float fovMaxSpeed = 30f;
    public float fovTransitionSpeed = 2f;

    [Header("Drift Effect")]
    public float driftAngleThreshold = 15f;
    public float driftLateralOffset = 1.5f;

    [Header("Smooth Rotation")]
    [Tooltip("How quickly the camera rotates toward looking at the target.")]
    public float rotationSmoothSpeed = 5f;

    private Vector3 currentOffset;

    void Start()
    {
        if (target != null)
        {
            currentOffset = defaultOffset;
        }

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

        // -- Get the speed if available
        Rigidbody targetRb = target.GetComponent<Rigidbody>();
        float speed = (targetRb != null) ? targetRb.linearVelocity.magnitude : 0f;

        // -------------------------------------------------------------
        // 1) FOV Stretch Effect
        // -------------------------------------------------------------
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

        // -------------------------------------------------------------
        // 2) Calculate desired offset (including drift).
        // -------------------------------------------------------------
        Vector3 desiredOffset = target.rotation * defaultOffset;

        // If drifting, shift laterally.
        if (targetRb != null && speed > fovSpeedThreshold)
        {
            Vector3 velocityDir = targetRb.linearVelocity.normalized;
            float driftAngle = Vector3.Angle(target.forward, velocityDir);
            if (driftAngle > driftAngleThreshold)
            {
                float sign = Mathf.Sign(Vector3.Cross(target.forward, velocityDir).y);
                desiredOffset += target.right * driftLateralOffset * sign;
            }
        }

        // Smoothly transition offset
        currentOffset = Vector3.Lerp(
            currentOffset,
            desiredOffset,
            offsetTransitionSpeed * Time.deltaTime
        );

        // -------------------------------------------------------------
        // 3) Dynamic Lag for follow speed
        // -------------------------------------------------------------
        float adjustedFollowSpeed = followSpeed;
        if (targetRb != null)
        {
            adjustedFollowSpeed = Mathf.Max(
                minFollowSpeed,
                followSpeed - (speed / lagSpeedDivider)
            );
        }

        // -------------------------------------------------------------
        // 4) Desired camera position
        // -------------------------------------------------------------
        Vector3 desiredPosition = target.position + currentOffset;

        // -- Use SmoothDamp instead of Lerp
        //    The "smoothTime" is about (1 / adjustedFollowSpeed), but you can tweak
        float smoothTime = 1f / adjustedFollowSpeed;
        Vector3 smoothedPosition = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref smoothDampVelocity,
            smoothTime
        );

        // -------------------------------------------------------------
        // 5) Ground Collision
        // -------------------------------------------------------------
        RaycastHit hit;
        if (Physics.Linecast(target.position, smoothedPosition, out hit, groundMask))
        {
            smoothedPosition = hit.point + Vector3.up * extraHeight;
        }

        // -------------------------------------------------------------
        // 6) Assign final position
        // -------------------------------------------------------------
        transform.position = smoothedPosition;

        // -------------------------------------------------------------
        // 7) Smooth Rotation
        //    Instead of transform.LookAt(target) instantly, do a slerp
        // -------------------------------------------------------------
        Vector3 dirToTarget = target.position - transform.position;
        if (dirToTarget.sqrMagnitude > 0.001f)
        {
            Quaternion desiredRot = Quaternion.LookRotation(dirToTarget, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                desiredRot,
                rotationSmoothSpeed * Time.deltaTime
            );
        }
    }
}
