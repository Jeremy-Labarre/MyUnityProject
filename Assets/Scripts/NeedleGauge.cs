using UnityEngine;

public class NeedleGauge : MonoBehaviour
{
    [Header("Needle Setup")]
    [Tooltip("The RectTransform of your needle Image (child of the gauge).")]
    public RectTransform needle;

    [Tooltip("Angle (Z rotation) for the needle at the lowest RPM.")]
    public float minAngle = 0f;

    [Tooltip("Angle (Z rotation) for the needle at the highest RPM.")]
    public float maxAngle = -180f;

    [Header("Car / RPM")]
    [Tooltip("Reference to your PlayerCarController script, which holds the RPM values.")]
    public PlayerCarController playerCar;

    void Update()
    {
        if (needle == null || playerCar == null)
            return;

        // Get the current RPM from the PlayerCarController.
        float currentRPM = playerCar.engineRPM;
        float minRPM = playerCar.minRPM;
        float maxRPM = playerCar.maxRPM;

        // Normalize the RPM to a value between 0 and 1.
        float normalizedRPM = (currentRPM - minRPM) / (maxRPM - minRPM);
        normalizedRPM = Mathf.Clamp01(normalizedRPM);

        // Lerp the angle between minAngle and maxAngle.
        float targetAngle = Mathf.Lerp(minAngle, maxAngle, normalizedRPM);

        // Apply the rotation to the needle's RectTransform (Z-axis).
        needle.localRotation = Quaternion.Euler(0f, 0f, targetAngle);
    }
}
