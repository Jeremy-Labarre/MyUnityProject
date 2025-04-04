using UnityEngine;
using TMPro;
using System.Collections;

public class RaceStartManager : MonoBehaviour
{
    public static bool raceStarted = false;  // This flag is false until the countdown ends.

    [Header("Countdown Settings")]
    public TMP_Text countdownText;          // Drag your TextMeshPro UI text element here.
    public float countdownTime = 3f;        // Number of seconds for the countdown.

    [Header("References")]
    public PlayerCarController playerCar;   // Drag your PlayerCarController GameObject here.

    [Header("Boost Settings")]
    public float boostForce = 5000f;        // The force applied if the RPM is within the boost range.

    [Header("Boost RPM Range (Green Zone)")]
    [Tooltip("Lower bound for RPM boost (e.g., 3000).")]
    public float boostRPMMin = 3000f;
    [Tooltip("Upper bound for RPM boost (e.g., 4000).")]
    public float boostRPMMax = 4000f;

    void Start()
    {
        // Lock the player's car movement until the countdown is finished.
        playerCar.canMove = false;
        raceStarted = false;
        StartCoroutine(StartCountdown());
    }

    private IEnumerator StartCountdown()
    {
        float currentTime = countdownTime;

        // Display the countdown numbers (3, 2, 1).
        while (currentTime > 0)
        {
            countdownText.text = currentTime.ToString("0");
            yield return new WaitForSeconds(1f);
            currentTime--;
        }

        // Display "GO!".
        countdownText.text = "GO!";

        // Check if the player's engine RPM is within the boost range.
        if (playerCar.engineRPM >= boostRPMMin && playerCar.engineRPM <= boostRPMMax)
        {
            playerCar.ApplyBoost(boostForce);
        }

        // Enable car movement and mark the race as started.
        playerCar.canMove = true;
        raceStarted = true;

        // Hide the countdown text after a short delay.
        yield return new WaitForSeconds(1f);
        countdownText.gameObject.SetActive(false);
    }
}
