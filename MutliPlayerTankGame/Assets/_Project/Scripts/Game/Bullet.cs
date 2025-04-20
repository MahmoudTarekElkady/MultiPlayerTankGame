using Mirror;
using UnityEngine;


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

        Debug.Log($"Bullet collision with: {collision.gameObject.name}");

        // IMPORTANT: Process everything BEFORE destroying the bullet

        // Find the root tank object
        Transform rootObject = collision.transform;
        while (rootObject.parent != null)
        {
            rootObject = rootObject.parent;
        }

        // Try to find the player components on the root object
        NetworkTankPlayer playerTank = rootObject.GetComponent<NetworkTankPlayer>();
        PlayerHealth playerHealth = rootObject.GetComponent<PlayerHealth>();

        Debug.Log($"Found playerTank: {playerTank != null}, Found playerHealth: {playerHealth != null}");

        if (playerTank != null && playerHealth != null)
        {
            // Skip if we hit ourselves
            if (playerTank == owner)
            {
                Debug.Log("Hit self, ignoring damage");
                NetworkServer.Destroy(gameObject);
                return;
            }

            // Check team (Friendly Fire)
            bool sameTeam = (owner != null && playerTank.playerTeam == owner.playerTeam &&
                              playerTank.playerTeam != NetworkTankPlayer.Team.None);

            Debug.Log($"Same team check: {sameTeam}, Owner team: {owner?.playerTeam}, Target team: {playerTank.playerTeam}");

            if (sameTeam)
            {
                Debug.Log("Friendly fire prevented");
                NetworkServer.Destroy(gameObject);
                return;
            }

            // Apply damage
            Debug.Log($"Applying {damage} damage to {playerTank.gameObject.name}");
            playerHealth.TakeDamage(damage, owner);
        }
        else
        {
            Debug.Log($"Hit object without player components: {collision.gameObject.name}");
        }

        // Always destroy the bullet at the end
        NetworkServer.Destroy(gameObject);
    }

    [ClientRpc]
    public void RpcSyncBulletPosition(Vector3 position, Vector3 velocity)
    {
        if (rb == null || transform == null) return;
        transform.position = position;
        rb.linearVelocity = velocity;
    }

    [ClientRpc]
    private void RpcShowHitEffect(GameObject target)
    {
        Debug.Log($"Hit effect shown on {target.name}");
        // You could add visual hit effects here
    }
}