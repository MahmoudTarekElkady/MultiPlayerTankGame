using UnityEngine;
using Mirror;

public class ReviveZone : MonoBehaviour
{
    private PlayerHealth playerHealth;
    private NetworkIdentity networkIdentity;
    private float reviveProgress = 0f;
    private float requiredReviveTime = 3f; // Time required to stay in the zone to revive
    private NetworkTankPlayer revivingPlayer = null;

    void Start()
    {
        playerHealth = GetComponentInParent<PlayerHealth>();
        networkIdentity = GetComponentInParent<NetworkIdentity>();

        // Disable this at start and only enable when the player dies
        GetComponent<Collider>().enabled = false;
    }

    public void EnableReviveZone()
    {
        GetComponent<Collider>().enabled = true;
    }

    public void DisableReviveZone()
    {
        GetComponent<Collider>().enabled = false;
        reviveProgress = 0f;
        revivingPlayer = null;
    }

    void OnTriggerEnter(Collider other)
    {
        // Check if a player entered the revive zone
        NetworkTankPlayer player = other.GetComponent<NetworkTankPlayer>();

        // If a player entered and no one is currently reviving
        if (player != null && player.isLocalPlayer && revivingPlayer == null && playerHealth.health <= 0)
        {
            revivingPlayer = player;
            Debug.Log("Player entered revive zone, starting revival process");
        }
    }

    void OnTriggerStay(Collider other)
    {
        // Check if the same player is still in the zone
        NetworkTankPlayer player = other.GetComponent<NetworkTankPlayer>();

        if (player != null && player == revivingPlayer && player.isLocalPlayer && playerHealth.health <= 0)
        {
            // Increment revive progress
            reviveProgress += Time.deltaTime;

            // Optional: Display revive progress UI
            Debug.Log($"Reviving: {reviveProgress / requiredReviveTime * 100}%");

            // Check if revive is complete
            if (reviveProgress >= requiredReviveTime)
            {
                // Call the revive method on the player
                player.StartReviveProcess(networkIdentity);

                // Reset progress and disable zone
                DisableReviveZone();
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        // Check if the reviving player left the zone
        NetworkTankPlayer player = other.GetComponent<NetworkTankPlayer>();

        if (player != null && player == revivingPlayer)
        {
            Debug.Log("Player left revive zone, resetting progress");
            reviveProgress = 0f;
            revivingPlayer = null;
        }
    }
}