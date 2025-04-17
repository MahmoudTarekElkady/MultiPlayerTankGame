using UnityEngine;
using Mirror;

[RequireComponent(typeof(Rigidbody))]
public class Bullet : NetworkBehaviour
{
    [SyncVar]
    public NetworkTankPlayer owner;

    public float speed = 10f;
    public float damage = 10f;
    public float lifespan = 5f;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            Debug.LogError("Rigidbody is missing on Bullet object!");
            return;
        }

        if (isServer)
        {
            // Set initial velocity on the server only
            rb.linearVelocity = transform.forward * speed;  // Fix: we use velocity instead of linearVelocity
            // Destroy the bullet after its lifespan on the server
            Destroy(gameObject, lifespan);
        }
    }

    void Update()
    {
        if (isServer)
        {
            // Sync bullet movement to clients by updating position on the server
            rb.linearVelocity = transform.forward * speed;  // Fixed: use velocity

            // Sync bullet position with clients
            RpcSyncBulletPosition(transform.position, rb.linearVelocity);
        }
    }

    [Command]
    private void CmdTakeDamage(GameObject target, float damageAmount)
    {
        var health = target.GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.TakeDamage(damageAmount); // Apply damage on the server
            Debug.Log($"Applying {damageAmount} damage to {target.name}");
        }
        else
        {
            Debug.LogWarning("No PlayerHealth component found on target.");
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!isServer) return; // Only handle collision on the server

        GameObject hitObject = collision.gameObject;

        // Check if we hit a player
        var playerTank = hitObject.GetComponent<NetworkTankPlayer>();
        if (playerTank != null && playerTank != owner)
        {
            // Prevent friendly fire
            if (playerTank.playerTeam == owner.playerTeam) return;

            // Apply damage to the player on the server via Command
            CmdTakeDamage(hitObject, damage);

            // Optionally, call ClientRpc to notify the client about the bullet hit
            RpcApplyDamage(hitObject);

            // Destroy the bullet after collision
            NetworkServer.Destroy(gameObject);  // Destroy bullet on both server and client
        }
    }

    [ClientRpc]
    public void RpcSyncBulletPosition(Vector3 position, Vector3 velocity)
    {
        // Ensure the transform and Rigidbody are not null
        if (transform == null)
        {
            Debug.LogError("Transform is null in RpcSyncBulletPosition!");
            return;
        }

        if (rb == null)
        {
            Debug.LogError("Rigidbody is null in RpcSyncBulletPosition!");
            return;
        }

        // Proceed with position and velocity update
        transform.position = position;  // Update the position
        rb.linearVelocity = velocity;         // Update the velocity (Fix: changed from linearVelocity to velocity)
    }

    [ClientRpc]
    private void RpcApplyDamage(GameObject target)
    {
        // This method can be used to notify the client about the damage
        var playerHealth = target.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            // Update health on the client side (use the current health to show the decrease)
            playerHealth.OnHealthChanged(playerHealth.health, playerHealth.health - damage);
        }
    }
}
