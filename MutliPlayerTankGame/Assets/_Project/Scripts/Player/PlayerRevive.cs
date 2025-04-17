using Mirror;
using UnityEngine;

public class PlayerRevive : NetworkBehaviour
{
    public float reviveRange = 5f;

    [Command]
    public void CmdRevivePlayer(NetworkIdentity revivedPlayer)
    {
        if (revivedPlayer != null && Vector3.Distance(transform.position, revivedPlayer.transform.position) <= reviveRange)
        {
            // Revive logic
            revivedPlayer.GetComponent<PlayerHealth>().health = 100f;
            RpcRevivePlayer(revivedPlayer.gameObject);
        }
    }

    [ClientRpc]
    public void RpcRevivePlayer(GameObject revivedPlayer)
    {
        revivedPlayer.SetActive(true);
        revivedPlayer.GetComponent<PlayerHealth>().health = 100f;
    }
}
