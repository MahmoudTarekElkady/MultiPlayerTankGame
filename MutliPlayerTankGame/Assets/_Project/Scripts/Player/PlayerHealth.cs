using UnityEngine;
using Mirror;

public class PlayerHealth : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnHealthChanged))]
    public float health = 100f;

    private PlayerUI playerUI;
    public NetworkTankPlayer owner;

    private ReviveZone reviveZone;

    void Start()
    {
        // Get the revive zone component
        reviveZone = GetComponentInChildren<ReviveZone>();
        if (reviveZone == null)
        {
            Debug.LogError($"No ReviveZone found on {gameObject.name} or its children!");
        }

        // Ensure we have UI reference
        playerUI = GetComponentInChildren<PlayerUI>();
        if (playerUI == null)
        {
            Debug.LogError($"PlayerUI component not found on {gameObject.name} or its children!");
        }
        else
        {
            // Set initial health
            playerUI.SetHealth(health);
            Debug.Log($"Initial health set for {gameObject.name}: {health}");
        }
    }
    public void TakeDamage(float damage, NetworkTankPlayer attacker = null)
    {
        if (!isServer)
        {
            Debug.LogWarning("TakeDamage called on client, ignoring!");
            return;
        }

        Debug.Log($"SERVER: {gameObject.name} taking {damage} damage from {(attacker != null ? attacker.gameObject.name : "unknown")}");

        // Store old health for logging
        float oldHealth = health;

        // Apply damage
        health -= damage;
        health = Mathf.Max(0, health); // Ensure health doesn't go below 0

        Debug.Log($"SERVER: {gameObject.name} health changed from {oldHealth} to {health}");

        // Check for death
        if (health <= 0)
        {
            string killerName = attacker != null ? attacker.gameObject.name : "Unknown";
            Debug.Log($"SERVER: {gameObject.name} killed by {killerName}");
            OnDeath(killerName);
        }

        // Force UI update on all clients
        RpcForceHealthUpdate(health);
    }

    [ClientRpc]
    void RpcForceHealthUpdate(float newHealth)
    {
        Debug.Log($"CLIENT: Force updating health UI for {gameObject.name} to {newHealth}");
        if (playerUI == null)
        {
            playerUI = GetComponentInChildren<PlayerUI>();
            if (playerUI == null)
            {
                Debug.LogError($"CLIENT: PlayerUI not found for {gameObject.name}");
                return;
            }
        }

        playerUI.SetHealth(newHealth);
    }

    public void OnHealthChanged(float oldHealth, float newHealth)
    {
        Debug.Log($"SYNCVAR HOOK: {gameObject.name} health changed from {oldHealth} to {newHealth}");

        if (playerUI == null)
        {
            playerUI = GetComponentInChildren<PlayerUI>();
            if (playerUI == null)
            {
                Debug.LogError($"SYNCVAR HOOK: PlayerUI not found for {gameObject.name}");
                return;
            }
        }

        playerUI.SetHealth(newHealth);
    }

    // Modify the OnDeath method in PlayerHealth.cs

    void OnDeath(string killerName)
    {
        if (!isServer) return;

        Debug.Log($"SERVER: Processing death of {gameObject.name} killed by {killerName}");

        // Update kill feed
        KillFeed killFeed = KillFeed.Instance;
        if (killFeed != null)
        {
            killFeed.RegisterKill(killerName, gameObject.name);
        }
        else
        {
            Debug.LogError("SERVER: KillFeed instance not found!");
        }

        // Notify the owner
        if (owner != null)
        {
            owner.OnPlayerDeath(killerName);
        }
        else
        {
            Debug.LogError($"SERVER: No owner set for {gameObject.name}, can't process death properly");
        }

        RpcHandleDeath();

        // Enable revive zone
        if (reviveZone != null)
        {
            reviveZone.EnableReviveZone();
        }

        // Check win condition
        GameManager gameManager = GameManager.Instance;
        if (gameManager != null)
        {
            gameManager.CheckWinCondition();
        }
    }

    public override void OnStartAuthority()
    {
        base.OnStartAuthority();

        Debug.Log($"Authority started for {gameObject.name}");

        playerUI = GetComponentInChildren<PlayerUI>();
        if (playerUI != null)
        {
            playerUI.SetHealth(health);

            // Set team color if owner is available
            if (owner != null)
            {
                playerUI.SetTeam(owner.playerTeam.ToString());
            }
            else
            {
                Debug.LogWarning($"No owner set for {gameObject.name} during authority start");
            }
        }
        else
        {
            Debug.LogError($"PlayerUI not found for {gameObject.name} during authority start");
        }
    }

    [ClientRpc]
    void RpcHandleDeath()
    {
        Debug.Log($"CLIENT: Handling death of {gameObject.name}");

        // Example visual feedback
        var renderer = GetComponentInChildren<MeshRenderer>();
        if (renderer != null)
            renderer.enabled = false;
        else
            Debug.LogWarning($"No MeshRenderer found for death visual on {gameObject.name}");

        var controller = GetComponent<NetworkTankPlayer>();
        if (controller != null)
            controller.enabled = false;
        else
            Debug.LogWarning($"No NetworkTankPlayer found to disable on {gameObject.name}");

        // Enable the revive zone after death
        if (reviveZone != null)
        {
            Debug.Log($"CLIENT: Enabling ReviveZone for {gameObject.name}");
            reviveZone.EnableReviveZone();
        }
        else
        {
            Debug.LogError($"CLIENT: No ReviveZone component found on {gameObject.name}!");
        }
    }

    // Method to revive player
    public void Revive(float reviveHealth = 50f)
    {
        if (!isServer) return;

        Debug.Log($"SERVER: Reviving {gameObject.name} with {reviveHealth} health");
        health = reviveHealth;

        // Notify clients that player is revived
        RpcRevivePlayer();

        // Disable revive zone
        if (reviveZone != null)
        {
            reviveZone.DisableReviveZone();
        }
    }

    [ClientRpc]
    void RpcRevivePlayer()
    {
        Debug.Log($"CLIENT: Reviving {gameObject.name}");

        // Enable renderer
        var renderer = GetComponentInChildren<MeshRenderer>();
        if (renderer != null)
            renderer.enabled = true;

        // Enable controller
        var controller = GetComponent<NetworkTankPlayer>();
        if (controller != null)
            controller.enabled = true;
    }
    // Add this method to PlayerHealth.cs
    public void ForceResetVisualState()
    {
        // Ensure player is visually alive and functional
        var renderers = GetComponentsInChildren<Renderer>(true);
        foreach (var renderer in renderers)
        {
            renderer.enabled = true;
        }

        // Re-enable controller
        var controller = GetComponent<NetworkTankPlayer>();
        if (controller != null)
        {
            controller.enabled = true;
        }

        // Disable revive zone if it's active
        if (reviveZone != null)
        {
            reviveZone.DisableReviveZone();
        }

        // Force client update
        RpcForceReset();
    }

    [ClientRpc]
    void RpcForceReset()
    {
        // Ensure player is visually alive on all clients
        var renderers = GetComponentsInChildren<Renderer>(true);
        foreach (var renderer in renderers)
        {
            renderer.enabled = true;
        }

        // Update health UI to show full health
        if (playerUI != null)
        {
            playerUI.SetHealth(health);
        }
    }
}