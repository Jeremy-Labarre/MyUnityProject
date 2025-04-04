using UnityEngine;

public class BoostPickup : MonoBehaviour
{
    // The amount of force to apply as a boost.
    public float boostForce = 1500f;

    // Optional: How long the boost lasts (if you want to create a temporary state).
    // public float boostDuration = 2f;

    // When something enters the trigger collider.
    void OnTriggerEnter(Collider other)
    {
        // Check if the object that hit the pickup has the "Player" tag.
        if (other.CompareTag("Player"))
        {
            Rigidbody playerRb = other.GetComponent<Rigidbody>();
            if (playerRb != null)
            {
                // Apply an impulse force in the direction the player is facing.
                playerRb.AddForce(other.transform.forward * boostForce, ForceMode.Impulse);
            }

            // Destroy the pickup after it is collected.
            Destroy(gameObject);
        }
    }
}
