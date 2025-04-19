using Mirror;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

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

    // In GameManager.cs
    [Server]
    public void CheckWinCondition()
    {
        if (!isServer || gameState != "Playing") return;

        try
        {
            // Clean up teams before checking
            CleanupTeamLists();

            int team1Alive = CountAlivePlayers(NetworkTankPlayer.Team.Team1);
            int team2Alive = CountAlivePlayers(NetworkTankPlayer.Team.Team2);

            Debug.Log($"Win check - Team1: {team1Alive}, Team2: {team2Alive}");

            // Get counts before evaluating
            int team1Count = teamPlayers[NetworkTankPlayer.Team.Team1].Count;
            int team2Count = teamPlayers[NetworkTankPlayer.Team.Team2].Count;

            // Check if one team is completely eliminated
            if (team1Alive == 0 && team1Count > 0)
            {
                EndGame("Team 2");
  
            }
            else if (team2Alive == 0 && team2Count > 0)
            {
                EndGame("Team 1");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Win check error: {e.Message}");
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

    // In GameManager.cs
[Server]
    public void RestartGame()
    {
        Debug.Log("=== SERVER RESTART INITIATED ===");

        // 1. Reset spawn points
        SpawnManager.Instance?.ResetSpawnPoints();

        // 2. Reset game state
        gameState = "TeamSelection";

        // 3. Reset all players
        foreach (var player in FindObjectsOfType<NetworkTankPlayer>())
        {
            if (player == null) continue;

            // Get spawn position
            Vector3 spawnPos = SpawnManager.Instance?.GetSpawnPosition(player.playerTeam) ??
                              new Vector3(Random.Range(-10, 10), 0, Random.Range(-10, 10));

            // Reset everything
            player.transform.position = spawnPos;
            player.transform.rotation = Quaternion.identity;
            player.playerTeam = NetworkTankPlayer.Team.None;
            player.teamColor = player.defaultColor;
            player.enabled = true;

            // Reset health
            PlayerHealth health = player.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.health = 100f;
                health.ForceResetVisualState();
            }

            // Network sync
            player.RpcFullPlayerReset(spawnPos);
        }

        // 4. Notify clients
        RpcGameRestarted();

        Debug.Log("=== SERVER RESTART COMPLETE ===");
    }

    [ClientRpc]
    void RpcGameRestarted()
    {
        Debug.Log("Client received restart notification");

        // Force UI update
        if (TeamSelectionUI.Instance != null)
        {
            TeamSelectionUI.Instance.ShowUI();
        }

        // Reset local player
        var localPlayer = NetworkClient.localPlayer?.GetComponent<NetworkTankPlayer>();
        if (localPlayer != null)
        {
            localPlayer.ClientReset();
        }
    }
    
}