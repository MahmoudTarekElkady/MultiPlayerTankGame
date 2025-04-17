using Mirror;
using System.Collections;
using UnityEngine;

public class PlayerRespawn : NetworkBehaviour
{
    public Transform spawnPoint;
    public float respawnTime = 5f;

    // Call this method when a player dies
    public void RespawnPlayer()
    {
        if (!isServer) return;

        // Wait before respawning player
        StartCoroutine(RespawnCoroutine());
    }

    private IEnumerator RespawnCoroutine()
    {
        yield return new WaitForSeconds(respawnTime);

        // Instantiate the player at the spawn point
        GameObject newPlayer = Instantiate(gameObject, spawnPoint.position, spawnPoint.rotation);
        NetworkServer.ReplacePlayerForConnection(connectionToClient, newPlayer);

        // Optionally, reset health
        newPlayer.GetComponent<PlayerHealth>().health = 100f;
    }
}
