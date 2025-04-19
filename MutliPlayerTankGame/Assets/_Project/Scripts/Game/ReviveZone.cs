using UnityEngine;
using Mirror;

public class ReviveZone : MonoBehaviour
{
    private PlayerHealth playerHealth;
    private NetworkIdentity networkIdentity;
    private float reviveProgress = 0f;
    private float requiredReviveTime = 3f; // Time required to stay in the zone to revive
    private NetworkTankPlayer revivingPlayer = null;
    private Collider zoneCollider;

    void Awake()
    {
        // Get the collider component
        zoneCollider = GetComponent<Collider>();
        if (zoneCollider == null)
        {
            Debug.LogError("ReviveZone needs a Collider component!");
            return;
        }

        // Make sure it's a trigger
        zoneCollider.isTrigger = true;

        // Initially disable the collider
        zoneCollider.enabled = false;

        Debug.Log("ReviveZone initialized");
    }

    void Start()
    {
        playerHealth = GetComponentInParent<PlayerHealth>();
        networkIdentity = GetComponentInParent<NetworkIdentity>();

        if (playerHealth == null)
        {
            Debug.LogError("ReviveZone couldn't find PlayerHealth component in parent!");
        }

        if (networkIdentity == null)
        {
            Debug.LogError("ReviveZone couldn't find NetworkIdentity component in parent!");
        }

        Debug.Log($"ReviveZone setup complete. Collider enabled: {zoneCollider.enabled}");
    }

    public void EnableReviveZone()
    {
        if (zoneCollider != null)
        {
            zoneCollider.enabled = true;
            Debug.Log($"ReviveZone ENABLED for {transform.parent.name}. Collider state: {zoneCollider.enabled}");
        }
        else
        {
            Debug.LogError("Cannot enable ReviveZone - no collider found!");
        }
    }

    public void DisableReviveZone()
    {
        if (zoneCollider != null)
        {
            zoneCollider.enabled = false;
            Debug.Log($"ReviveZone DISABLED for {transform.parent.name}");
        }
        reviveProgress = 0f;
        revivingPlayer = null;
    }

    void OnTriggerEnter(Collider other)
    {
        // Check if a player entered the revive zone
        NetworkTankPlayer player = other.GetComponent<NetworkTankPlayer>();
        if (player == null)
        {
            // Try to find it in parent
            player = other.GetComponentInParent<NetworkTankPlayer>();
        }

        // If a player entered and no one is currently reviving
        if (player != null && player.isLocalPlayer && revivingPlayer == null && playerHealth.health <= 0)
        {
            revivingPlayer = player;
            Debug.Log($"Player {player.name} entered revive zone for {transform.parent.name}, starting revival process");
        }
    }

    void OnTriggerStay(Collider other)
    {
        // Check if the same player is still in the zone
        NetworkTankPlayer player = other.GetComponent<NetworkTankPlayer>();
        if (player == null)
        {
            // Try to find it in parent
            player = other.GetComponentInParent<NetworkTankPlayer>();
        }

        if (player != null && player == revivingPlayer && player.isLocalPlayer && playerHealth.health <= 0)
        {
            // Increment revive progress
            reviveProgress += Time.deltaTime;

            // Debug revive progress
            Debug.Log($"Reviving {transform.parent.name}: {reviveProgress}/{requiredReviveTime} seconds - {(reviveProgress / requiredReviveTime * 100):F0}%");

            // Check if revive is complete
            if (reviveProgress >= requiredReviveTime)
            {
                // Call the revive method on the player
                Debug.Log($"Revive complete! Starting revive process for {transform.parent.name}");
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
        if (player == null)
        {
            // Try to find it in parent
            player = other.GetComponentInParent<NetworkTankPlayer>();
        }

        if (player != null && player == revivingPlayer)
        {
            Debug.Log($"Player {player.name} left revive zone for {transform.parent.name}, resetting progress");
            reviveProgress = 0f;
            revivingPlayer = null;
        }
    }
}