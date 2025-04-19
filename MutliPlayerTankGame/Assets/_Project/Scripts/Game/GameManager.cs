using Mirror;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Win UI")]
    public GameObject winScreen;
    public TextMeshProUGUI winnerText;
    public Button replayButton;

    [SyncVar(hook = nameof(OnGameStateChanged))]
    private string gameState = "TeamSelection"; // TeamSelection, Playing, GameOver

    private Dictionary<NetworkTankPlayer.Team, List<NetworkTankPlayer>> teamPlayers =
        new Dictionary<NetworkTankPlayer.Team, List<NetworkTankPlayer>>();

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

        // Initialize team dictionaries
        teamPlayers[NetworkTankPlayer.Team.Team1] = new List<NetworkTankPlayer>();
        teamPlayers[NetworkTankPlayer.Team.Team2] = new List<NetworkTankPlayer>();
        teamPlayers[NetworkTankPlayer.Team.None] = new List<NetworkTankPlayer>();
    }

    void Start()
    {
        if (winScreen != null)
        {
            winScreen.SetActive(false);
        }

        if (replayButton != null)
        {
            replayButton.onClick.AddListener(OnReplayButtonClicked);
        }
    }

    public void RegisterPlayer(NetworkTankPlayer player)
    {
        if (!isServer) return;

        // Clean up null entries first
        CleanupTeamLists();

        // Add player to appropriate team list
        if (!teamPlayers[player.playerTeam].Contains(player))
        {
            teamPlayers[player.playerTeam].Add(player);
        }

        Debug.Log($"Player {player.gameObject.name} registered with team {player.playerTeam}");
    }

    // Helper method to clean up null entries in team lists
    private void CleanupTeamLists()
    {
        if (!isServer) return;

        foreach (var team in teamPlayers.Keys)
        {
            // Remove any null references
            teamPlayers[team].RemoveAll(player => player == null);
        }
    }

    public void UpdatePlayerTeam(NetworkTankPlayer player, NetworkTankPlayer.Team oldTeam, NetworkTankPlayer.Team newTeam)
    {
        if (!isServer) return;

        // Clean up null entries first
        CleanupTeamLists();

        // Remove from old team
        if (teamPlayers.ContainsKey(oldTeam) && teamPlayers[oldTeam].Contains(player))
        {
            teamPlayers[oldTeam].Remove(player);
        }

        // Add to new team
        if (!teamPlayers[newTeam].Contains(player))
        {
            teamPlayers[newTeam].Add(player);
        }

        Debug.Log($"Player {player.gameObject.name} moved from team {oldTeam} to {newTeam}");

        // Check if all players have selected a team (and we have players)
        if (gameState == "TeamSelection" && teamPlayers[NetworkTankPlayer.Team.None].Count == 0 &&
            (teamPlayers[NetworkTankPlayer.Team.Team1].Count > 0 || teamPlayers[NetworkTankPlayer.Team.Team2].Count > 0))
        {
            // All players have selected teams, start the game
            StartGame();
        }
    }

    public void CheckWinCondition()
    {
        if (!isServer || gameState != "Playing") return;

        try
        {
            // Clean up teams before checking
            CleanupTeamLists();

            int team1Alive = CountAlivePlayers(NetworkTankPlayer.Team.Team1);
            int team2Alive = CountAlivePlayers(NetworkTankPlayer.Team.Team2);

            Debug.Log($"Win condition check: Team1 alive: {team1Alive}, Team2 alive: {team2Alive}");

            // Get counts before evaluating to avoid accessing potentially invalid references
            int team1Count = teamPlayers[NetworkTankPlayer.Team.Team1].Count;
            int team2Count = teamPlayers[NetworkTankPlayer.Team.Team2].Count;

            // Check if one team is completely eliminated
            if (team1Alive == 0 && team1Count > 0)
            {
                // Team 2 wins
                EndGame("Team 2");
            }
            else if (team2Alive == 0 && team2Count > 0)
            {
                // Team 1 wins
                EndGame("Team 1");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in CheckWinCondition: {e.Message}\n{e.StackTrace}");
        }
    }

    private int CountAlivePlayers(NetworkTankPlayer.Team team)
    {
        int count = 0;

        // Create a temporary list to avoid modification during iteration
        List<NetworkTankPlayer> currentPlayers = new List<NetworkTankPlayer>(teamPlayers[team]);

        foreach (var player in currentPlayers)
        {
            if (player == null) continue;

            PlayerHealth health = player.GetComponent<PlayerHealth>();
            if (health != null && health.health > 0)
            {
                count++;
            }
        }
        return count;
    }

    [Server]
    private void StartGame()
    {
        Debug.Log("Starting game - all players have selected teams");
        gameState = "Playing";
        RpcNotifyGameStarted();
    }

    [Server]
    private void EndGame(string winningTeam)
    {
        Debug.Log($"Game over - {winningTeam} wins!");
        gameState = "GameOver";
        RpcShowWinScreen(winningTeam);
    }

    [ClientRpc]
    private void RpcNotifyGameStarted()
    {
        Debug.Log("Game started!");

        // Hide the team selection UI if it's still open
        TeamSelectionUI teamUI = TeamSelectionUI.Instance;
        if (teamUI != null)
        {
            teamUI.gameObject.SetActive(false);
        }
    }

    [ClientRpc]
    private void RpcShowWinScreen(string winningTeam)
    {
        if (winScreen != null)
        {
            winScreen.SetActive(true);

            if (winnerText != null)
            {
                winnerText.text = $"{winningTeam} Wins!";
            }
        }
    }

    // This method needs to work for both host and clients
    public void OnReplayButtonClicked()
    {
        if (isServer)
        {
            // Server/host can restart directly
            RestartGame();
        }
        else
        {
            // Clients need to send command to server
            NetworkTankPlayer localPlayer = FindObjectOfType<NetworkTankPlayer>();
            if (localPlayer != null && localPlayer.isLocalPlayer)
            {
                localPlayer.CmdRequestRestart();
            }
        }
    }

    public bool CanPlayerMove()
    {
        return gameState == "Playing";
    }

    void OnGameStateChanged(string oldState, string newState)
    {
        Debug.Log($"Game state changed from {oldState} to {newState}");

        if (newState == "TeamSelection")
        {
            // Reset everything for a new game
            if (winScreen != null)
            {
                winScreen.SetActive(false);
            }

            // Show team selection UI
            TeamSelectionUI teamUI = TeamSelectionUI.Instance;
            if (teamUI != null)
            {
                teamUI.ShowUI();
            }
        }
    }

    public string GetCurrentGameState()
    {
        return gameState;
    }

    [Server]
    public void RestartGame()
    {
        Debug.Log("Restarting game");

        // Reset team lists - make sure to clear them first
        foreach (var team in teamPlayers.Keys)
        {
            teamPlayers[team].Clear();
        }

        // Reset all players and re-register them
        NetworkTankPlayer[] allPlayers = FindObjectsOfType<NetworkTankPlayer>();
        foreach (var player in allPlayers)
        {
            if (player == null) continue;

            try
            {
                // Reset health
                PlayerHealth health = player.GetComponent<PlayerHealth>();
                if (health != null)
                {
                    health.health = 100f;
                    // Force a visual state reset to fix potential issues
                    health.ForceResetVisualState();
                }

                // Reset team
                player.playerTeam = NetworkTankPlayer.Team.None;
                player.teamColor = player.defaultColor;

                // Re-enable if disabled
                player.enabled = true;

                // Reset position
                player.RpcResetPosition();

                // Re-add to None team list
                teamPlayers[NetworkTankPlayer.Team.None].Add(player);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error resetting player {player.name}: {e.Message}");
            }
        }

        // Change game state
        gameState = "TeamSelection";

        // Notify clients
        RpcGameRestarted();
    }

    [ClientRpc]
    void RpcGameRestarted()
    {
        // Show team selection UI
        TeamSelectionUI teamUI = TeamSelectionUI.Instance;
        if (teamUI != null)
        {
            teamUI.ShowUI();
            Debug.Log("Team Selection UI should be visible now");
        }
        else
        {
            Debug.LogError("TeamSelectionUI not found!");
        }

        // Make sure the win screen is hidden
        if (winScreen != null)
        {
            winScreen.SetActive(false);
        }

        Debug.Log("Game restarted on client. Game state: " + gameState);
    }
}