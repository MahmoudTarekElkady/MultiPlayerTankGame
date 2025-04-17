using Mirror;
using UnityEngine;

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
        if (health <= 0)
        {
            health = 0;
            OnDeath();
        }
    }

    // Hook method to update health slider when health changes
   public void OnHealthChanged(float oldHealth, float newHealth)
    {
        if (playerUI == null)
            playerUI = GetComponentInChildren<PlayerUI>();

        if (playerUI != null)
            playerUI.SetHealth(newHealth);  // Updates the health slider
    }

    // Death handling logic
    void OnDeath()
    {
        if (isServer)
        {
            string killerName = "EnemyPlayer"; // Replace with real killer name if you have it
            CmdPlayerDied(killerName, gameObject.name);
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
}
