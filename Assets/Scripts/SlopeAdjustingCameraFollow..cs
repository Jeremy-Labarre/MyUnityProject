using UnityEngine;

public class SlopeAdjustingCameraFollow : MonoBehaviour
{
    [Header("Camera Follow Settings")]
    [Tooltip("The target (player) the camera follows.")]
    public Transform target;
    [Tooltip("Base offset from the target's position.")]
    public Vector3 offset = new Vector3(0, 7, -12);
    [Tooltip("Speed at which the camera follows.")]
    public float followSpeed = 8f;

    [Header("Slope Adjustment Settings")]
    [Tooltip("How far below the target to cast the ray to detect the ground.")]
    public float raycastDistance = 5f;
    [Tooltip("Layer(s) considered as ground.")]
    public LayerMask groundLayer;
    [Tooltip("Additional vertical offset added per degree of slope.")]
    public float slopeMultiplier = 0.2f;

    void LateUpdate()
    {
        if (target == null)
            return;

        // Start with the base offset.
        Vector3 adjustedOffset = offset;

        // Cast a ray downward from the target to detect ground.
        RaycastHit hit;
        if (Physics.Raycast(target.position, Vector3.down, out hit, raycastDistance, groundLayer))
        {
            // Calculate the angle between the ground normal and the upward direction.
            float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);

            // Increase vertical offset based on slope angle.
            // (For example, if slopeAngle is 20 degrees and slopeMultiplier is 0.2, add 4 units to y.)
            adjustedOffset.y += slopeAngle * slopeMultiplier;
        }

        // Compute the desired camera position based on the target position plus the adjusted offset.
        Vector3 desiredPosition = target.position + adjustedOffset;

        // Smoothly interpolate the camera's position.
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);

        // Make the camera look at the target.
        transform.LookAt(target.position);
    }
}
