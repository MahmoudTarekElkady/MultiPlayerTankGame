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
    // In PlayerRespawn.cs
    private IEnumerator RespawnCoroutine()
    {
        yield return new WaitForSeconds(respawnTime);

        // Get spawn position from SpawnManager based on team
        Vector3 spawnPosition = SpawnManager.Instance.GetSpawnPosition(GetComponent<NetworkTankPlayer>().playerTeam);
        Quaternion spawnRotation = Quaternion.identity;

        GameObject newPlayer = Instantiate(gameObject, spawnPosition, spawnRotation);

        // Copy team assignment
        newPlayer.GetComponent<NetworkTankPlayer>().playerTeam = GetComponent<NetworkTankPlayer>().playerTeam;

        NetworkServer.ReplacePlayerForConnection(connectionToClient, newPlayer);

        // Reset health and movement
        newPlayer.GetComponent<PlayerHealth>().health = 100f;
        newPlayer.GetComponent<NetworkTankPlayer>().RpcResetMovementState();
    }
}
