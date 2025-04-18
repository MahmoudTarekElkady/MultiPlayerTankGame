using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;

public class TeamSelectionUI : MonoBehaviour
{
    public TMP_Dropdown teamDropdown;
    public Button confirmButton;

    // Make this a singleton that persists
    public static TeamSelectionUI Instance;

    private void Awake()
    {
        Instance = this;
        // Hide the UI at start
        gameObject.SetActive(false);
    }

    void Start()
    {
        // Set up dropdown options
        teamDropdown.ClearOptions();
        teamDropdown.AddOptions(new System.Collections.Generic.List<string> { "None", "Team 1", "Team 2" });

        // Set up confirm button
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(ConfirmTeamSelection);
        }

        Debug.Log("TeamSelectionUI initialized and ready");
    }

    // Called when the confirm button is clicked
    void ConfirmTeamSelection()
    {
        // Find local player
        NetworkTankPlayer localPlayer = FindLocalPlayer();
        if (localPlayer != null)
        {
            // Pass the selected team index
            localPlayer.OnTeamSelected(teamDropdown.value);
            Debug.Log($"Team selection confirmed: {teamDropdown.value}");

            // Hide the UI after selection
            gameObject.SetActive(false);
        }
        else
        {
            Debug.LogError("Could not find local player to set team!");
        }
    }

    public void ShowUI()
    {
        gameObject.SetActive(true);
        Debug.Log("Team selection UI shown");
    }

    NetworkTankPlayer FindLocalPlayer()
    {
        // Find all players in the scene
        NetworkTankPlayer[] players = GameObject.FindObjectsOfType<NetworkTankPlayer>();

        // Return the local player
        foreach (NetworkTankPlayer player in players)
        {
            if (player.isLocalPlayer)
            {
                Debug.Log($"Found local player: {player.gameObject.name}");
                return player;
            }
        }

        Debug.LogWarning("No local player found!");
        return null;
    }
}