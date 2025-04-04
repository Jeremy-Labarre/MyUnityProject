using UnityEngine;
using UnityEngine.UI;

public class RPMStatusBar : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the PlayerCarController that holds the RPM value.")]
    public PlayerCarController playerCar;

    [Tooltip("UI Image component set to Filled type that will represent the RPM status bar.")]
    public Image rpmFillImage;

    void Update()
    {
        if (playerCar == null || rpmFillImage == null)
            return;

        // Calculate the normalized RPM between 0 and 1.
        float normalizedRPM = (playerCar.engineRPM - playerCar.minRPM) / (playerCar.maxRPM - playerCar.minRPM);
        normalizedRPM = Mathf.Clamp01(normalizedRPM);

        // Update the fill amount to reflect the current RPM.
        rpmFillImage.fillAmount = normalizedRPM;
    }
}
