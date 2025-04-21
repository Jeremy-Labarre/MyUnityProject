using UnityEngine;

public class NeedleGaugeParent : MonoBehaviour
{
    [Header("Gauge Settings")]
    [Tooltip("Angle of the needle when RPM is at minimum.")]
    public float minAngle = 0f;
    [Tooltip("Angle of the needle when RPM is at maximum.")]
    public float maxAngle = 180f;

    [Header("Rotation Speed")]
    [Tooltip("How fast the needle rotates to its target angle.")]
    public float rotationSpeed = 10f;

    [Header("Shake Effect (at max RPM)")]
    [Tooltip("Amplitude of the needle shake in degrees.")]
    public float shakeAmplitude = 2f;
    [Tooltip("Frequency of the needle shake (in Hz).")]
    public float shakeFrequency = 10f;

    [Header("Car / RPM")]
    [Tooltip("Reference to the PlayerCarController script, which holds the RPM values.")]
    public PlayerCarController playerCar;

    void Update()
    {
        if (playerCar == null)
            return;

        // Get normalized RPM (0 to 1).
        float normalizedRPM = Mathf.Clamp01((playerCar.engineRPM - playerCar.minRPM) / (playerCar.maxRPM - playerCar.minRPM));

        // Calculate target angle based on normalized RPM.
        float targetAngle = Mathf.Lerp(minAngle, maxAngle, normalizedRPM);

        // When near or at max RPM, add a shake effect.
        if (normalizedRPM >= 0.98f)
        {
            targetAngle += shakeAmplitude * Mathf.Sin(Time.time * shakeFrequency);
        }

        // Smoothly interpolate from current angle to target angle.
        float currentAngle = transform.localRotation.eulerAngles.z;
        float newAngle = Mathf.LerpAngle(currentAngle, targetAngle, Time.deltaTime * rotationSpeed);
        transform.localRotation = Quaternion.Euler(0f, 0f, newAngle);
    }
}
