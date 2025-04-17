using Mirror;
using System.Collections.Generic;
using UnityEngine.UI;

public class KillFeed : NetworkBehaviour
{
    public Text killText; // Reference to your UI Text element
    private List<string> killMessages = new List<string>();

    [ClientRpc]
    public void RpcDisplayKillMessage(string killerName, string deadName)
    {
        string killMessage = $"{killerName} killed {deadName}";

        // Add the kill message to the list
        killMessages.Add(killMessage);

        // Update the kill feed UI
        UpdateKillFeedUI();
    }

    void UpdateKillFeedUI()
    {
        killText.text = "";
        foreach (var message in killMessages)
        {
            killText.text += message + "\n";
        }

        // Optionally, you can limit the number of messages displayed (e.g., max 5)
        if (killMessages.Count > 5)
        {
            killMessages.RemoveAt(0); // Remove the oldest message
        }
    }

    [Command]
    public void CmdPlayerDied(string killerName, string deadName)
    {
        RpcDisplayKillMessage(killerName, deadName);
    }
}
