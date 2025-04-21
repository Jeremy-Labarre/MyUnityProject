// FinishLineTrigger.cs
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class FinishLineTrigger : MonoBehaviour
{
    [Header("Drag the LevelCompletePanel here")]
    public GameObject levelCompleteUIPanel;

    private bool hasTriggered = false;

    void Awake()
    {
        // Ensure our collider is a trigger
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;

        // Hide the UI panel at start
        if (levelCompleteUIPanel != null)
            levelCompleteUIPanel.SetActive(false);
        else
            Debug.LogWarning("Level Complete UI Panel not assigned!");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered)
            return;

        // Only react to the PlayerCarController (not your enemy)
        PlayerCarController player = other.GetComponentInParent<PlayerCarController>();
        if (player == null)
            return;

        hasTriggered = true;

        // Pause the game
        Time.timeScale = 0f;

        // Show the level complete panel
        levelCompleteUIPanel.SetActive(true);
    }

    // Called by the Continue button
    public void OnContinueButton()
    {
        // Resume time
        Time.timeScale = 1f;

        // Load next scene in build order
        int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;
        if (nextIndex < SceneManager.sceneCountInBuildSettings)
            SceneManager.LoadScene(nextIndex);
        else
            Debug.Log("No more scenes to load!");
    }

    // Called by the Restart button
    public void OnRestartButton()
    {
        // Resume time
        Time.timeScale = 1f;

        // Reload current scene
        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentIndex);
    }
}
