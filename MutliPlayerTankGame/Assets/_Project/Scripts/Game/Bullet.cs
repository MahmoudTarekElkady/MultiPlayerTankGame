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

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        if (rb == null)
        {
            Debug.LogError("Rigidbody is missing on Bullet!");
            return;
        }

        if (isServer)
        {
            rb.linearVelocity = transform.forward * speed;
            Destroy(gameObject, lifespan);
        }
    }

    void Update()
    {
        if (isServer)
        {
            rb.linearVelocity = transform.forward * speed;
            RpcSyncBulletPosition(transform.position, rb.linearVelocity);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!isServer) return;

        // Try to find the NetworkTankPlayer on the hit object or its parents
        GameObject hitObject = collision.gameObject;
        NetworkTankPlayer playerTank = hitObject.GetComponent<NetworkTankPlayer>();
        if (playerTank == null)
        {
            // Try to get from parent if not found directly
            playerTank = hitObject.GetComponentInParent<NetworkTankPlayer>();
        }

        // If we found a player tank
        if (playerTank != null && playerTank != owner)
        {
            // Prevent friendly fire, but still destroy bullet
            if (playerTank.playerTeam == owner.playerTeam)
            {
                NetworkServer.Destroy(gameObject);
                return;
            }

            // Apply damage
            var health = playerTank.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage(damage);
                RpcApplyDamage(playerTank.gameObject);
            }

            // Always destroy the bullet
            NetworkServer.Destroy(gameObject);
        }
        else
        {
            // Hit something else (wall, etc.)
            NetworkServer.Destroy(gameObject);
        }

        // Debug to verify collision detection is happening
        Debug.Log($"Bullet collided with {hitObject.name} and was destroyed");
    }

    [ClientRpc]
    public void RpcSyncBulletPosition(Vector3 position, Vector3 velocity)
    {
        if (rb == null || transform == null) return;
        transform.position = position;
        rb.linearVelocity = velocity;
    }

    [ClientRpc]
    private void RpcApplyDamage(GameObject target)
    {
        var playerHealth = target.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged(playerHealth.health, playerHealth.health - damage);
        }
    }
}
