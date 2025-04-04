using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerRespawn : MonoBehaviour
{
    [Header("Respawn Settings")]
    public float voidThreshold = -5f;  // Y position threshold for respawning.
    public Transform respawnPoint;     // Safe respawn location for the player.
    [Tooltip("Custom rotation (Euler angles) for the player when respawning.")]
    public Vector3 respawnEulerAngles = Vector3.zero;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("PlayerRespawn: No Rigidbody component found.");
        }
    }

    void Update()
    {
        if (transform.position.y < voidThreshold)
        {
            RespawnPlayer();
        }
    }

    /// <summary>
    /// Resets the player's position, velocity, and rotation.
    /// </summary>
    void RespawnPlayer()
    {
        if (respawnPoint != null)
        {
            Debug.Log("Player fell into the void. Respawning...");
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            transform.position = respawnPoint.position;
            transform.rotation = Quaternion.Euler(respawnEulerAngles); // Apply custom rotation.
        }
        else
        {
            Debug.LogError("PlayerRespawn: No respawn point assigned.");
        }
    }
}
