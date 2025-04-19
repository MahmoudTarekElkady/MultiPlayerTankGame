using Mirror;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class KillFeed : NetworkBehaviour
{
    public Text killText; // Reference to your UI Text element
    private List<string> killMessages = new List<string>();

    // Singleton pattern for easy access
    public static KillFeed Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (killText == null)
        {
            GameObject killTextObj = GameObject.Find("KillText");
            if (killTextObj != null)
            {
                killText = killTextObj.GetComponent<Text>();
            }

            if (killText == null)
            {
                Debug.LogError("KillFeed: killText reference is missing!");
            }
        }
    }

    [ClientRpc]
    public void RpcDisplayKillMessage(string killerName, string deadName)
    {
        string killMessage = $"{killerName} killed {deadName}";
        Debug.Log("Kill Feed: " + killMessage);

        // Add the kill message to the list
        killMessages.Add(killMessage);

        // Update the kill feed UI
        UpdateKillFeedUI();
    }

    void UpdateKillFeedUI()
    {
        if (killText == null) return;

        killText.text = "";
        foreach (var message in killMessages)
        {
            killText.text += message + "\n";
        }

        // Limit the number of messages displayed (max 5)
        if (killMessages.Count > 5)
        {
            killMessages.RemoveAt(0); // Remove the oldest message
        }
    }

    // This should be called from the PlayerHealth script
    [Server]
    public void RegisterKill(string killerName, string deadName)
    {
        RpcDisplayKillMessage(killerName, deadName);
    }
}