using UnityEngine;
using Mirror;

public class PlayerHealth : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnHealthChanged))]
    public float health = 100f;

    private PlayerUI playerUI;
    public NetworkTankPlayer owner;

    public void TakeDamage(float damage)
    {
        if (!isServer) return;

        health -= damage;
        Debug.Log($"[{gameObject.name}] Took {damage} damage. New health: {health}");

        if (health <= 0)
        {
            health = 0;
            OnDeath();
        }
    }

    public void OnHealthChanged(float oldHealth, float newHealth)
    {
        if (playerUI == null)
        {
            playerUI = GetComponentInChildren<PlayerUI>();
            if (playerUI == null)
            {
                Debug.LogWarning("PlayerUI not found for " + gameObject.name);
                return;
            }
        }
        playerUI.SetHealth(newHealth);
    }

    void OnDeath()
    {
        if (isServer)
        {
            string killerName = "EnemyPlayer"; // Replace this with real owner logic later
            CmdPlayerDied(killerName, gameObject.name);
            RpcHandleDeath();
        }
    }

    public override void OnStartAuthority()
    {
        base.OnStartAuthority();
        playerUI = GetComponentInChildren<PlayerUI>();
        if (playerUI != null)
        {
            playerUI.SetHealth(health);
        }
    }

    [Command]
    void CmdPlayerDied(string killerName, string deadPlayerName)
    {
        RpcDisplayKillMessage(killerName, deadPlayerName);
    }

    [ClientRpc]
    void RpcDisplayKillMessage(string killerName, string deadPlayerName)
    {
        Debug.Log($"{killerName} killed {deadPlayerName}");
    }

    [ClientRpc]
    void RpcHandleDeath()
    {
        // Example visual feedback
        var renderer = GetComponentInChildren<MeshRenderer>();
        if (renderer != null)
            renderer.enabled = false;

        var controller = GetComponent<NetworkTankPlayer>();
        if (controller != null)
            controller.enabled = false;

        Debug.Log($"[{gameObject.name}] has died.");
    }
}
