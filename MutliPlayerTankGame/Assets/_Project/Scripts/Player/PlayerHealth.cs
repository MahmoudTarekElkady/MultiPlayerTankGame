using Mirror;
using UnityEngine;

public class PlayerHealth : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnHealthChanged))]
    public float health = 100f;

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

    void OnHealthChanged(float oldHealth, float newHealth)
    {
        if (owner != null && owner.playerUI != null)
        {
            owner.playerUI.SetHealth(newHealth);
        }
    }

    void OnDeath()
    {
        if (isServer)
        {
            string killerName = "EnemyPlayer"; // Replace with real killer name if you have it
            CmdPlayerDied(killerName, gameObject.name);
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        var tankPlayer = GetComponent<NetworkTankPlayer>();
        if (tankPlayer != null)
            owner = tankPlayer;
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
