using UnityEngine;

public class NeedleGaugeParent : MonoBehaviour
{
    [Header("Gauge Settings")]
    [Tooltip("Angle of the needle when RPM is at minimum.")]
    public float minAngle = 0f;
    [Tooltip("Angle of the needle when RPM is at maximum.")]
    public float maxAngle = 180f;

    [Header("Rotation Speed")]
    [Tooltip("Multiplier for how fast the needle rotates to its target angle.")]
    public float rotationSpeed = 10f;

    [Header("Car / RPM")]
    [Tooltip("Reference to the PlayerCarController script, which holds the RPM values.")]
    public PlayerCarController playerCar;

    void Update()
    {
        if (playerCar == null)
            return;

        // Retrieve RPM values from the car.
        float currentRPM = playerCar.engineRPM;
        float minRPM = playerCar.minRPM;
        float maxRPM = playerCar.maxRPM;

        // Normalize the current RPM between 0 and 1.
        float normalizedRPM = Mathf.Clamp01((currentRPM - minRPM) / (maxRPM - minRPM));

        // Determine the target angle based on normalized RPM.
        float targetAngle = Mathf.Lerp(minAngle, maxAngle, normalizedRPM);

        // Smoothly interpolate from the current angle to the target angle.
        float currentAngle = transform.localRotation.eulerAngles.z;
        float newAngle = Mathf.LerpAngle(currentAngle, targetAngle, Time.deltaTime * rotationSpeed);

        // Apply the rotation.
        transform.localRotation = Quaternion.Euler(0f, 0f, newAngle);
    }
}
