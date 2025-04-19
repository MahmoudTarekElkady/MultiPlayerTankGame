using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class SpawnManager : NetworkBehaviour
{
    public static SpawnManager Instance { get; private set; }

    [Header("Spawn Points")]
    public List<Transform> team1SpawnPoints = new List<Transform>();
    public List<Transform> team2SpawnPoints = new List<Transform>();
    public List<Transform> defaultSpawnPoints = new List<Transform>();

    // Keep track of which spawn points are currently in use
    private Dictionary<NetworkTankPlayer.Team, List<int>> usedSpawnIndices = new Dictionary<NetworkTankPlayer.Team, List<int>>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }

        // Initialize used indices lists
        usedSpawnIndices[NetworkTankPlayer.Team.None] = new List<int>();
        usedSpawnIndices[NetworkTankPlayer.Team.Team1] = new List<int>();
        usedSpawnIndices[NetworkTankPlayer.Team.Team2] = new List<int>();
    }

    // Get a spawn position for a specific team
    // In SpawnManager.cs
    public Vector3 GetSpawnPosition(NetworkTankPlayer.Team team)
    {
        List<Transform> spawnPoints;
        List<int> usedIndices;

        switch (team)
        {
            case NetworkTankPlayer.Team.Team1:
                spawnPoints = team1SpawnPoints;
                usedIndices = usedSpawnIndices[NetworkTankPlayer.Team.Team1];
                break;
            case NetworkTankPlayer.Team.Team2:
                spawnPoints = team2SpawnPoints;
                usedIndices = usedSpawnIndices[NetworkTankPlayer.Team.Team2];
                break;
            default:
                spawnPoints = defaultSpawnPoints;
                usedIndices = usedSpawnIndices[NetworkTankPlayer.Team.None];
                break;
        }

        if (spawnPoints == null || spawnPoints.Count == 0)
        {
            Debug.LogWarning($"No spawn points for team {team}, using random position");
            return new Vector3(Random.Range(-10, 10), 0, Random.Range(-10, 10));
        }

        // Find first available spawn point
        for (int i = 0; i < spawnPoints.Count; i++)
        {
            if (!usedIndices.Contains(i))
            {
                usedIndices.Add(i);
                Debug.Log($"Spawning at point {i} for team {team}");
                return spawnPoints[i].position;
            }
        }

        // If all used, pick random and log warning
        Debug.LogWarning($"All spawn points used for team {team}, selecting random");
        return spawnPoints[Random.Range(0, spawnPoints.Count)].position;
    }

    // Release a spawn point when a player switches teams or disconnects
    public void ReleaseSpawnPoint(NetworkTankPlayer.Team team, Vector3 position)
    {
        List<Transform> spawnPoints;
        List<int> usedIndices;

        switch (team)
        {
            case NetworkTankPlayer.Team.Team1:
                spawnPoints = team1SpawnPoints;
                usedIndices = usedSpawnIndices[NetworkTankPlayer.Team.Team1];
                break;
            case NetworkTankPlayer.Team.Team2:
                spawnPoints = team2SpawnPoints;
                usedIndices = usedSpawnIndices[NetworkTankPlayer.Team.Team2];
                break;
            default:
                spawnPoints = defaultSpawnPoints;
                usedIndices = usedSpawnIndices[NetworkTankPlayer.Team.None];
                break;
        }

        // Find which spawn point matches this position and release it
        for (int i = 0; i < spawnPoints.Count; i++)
        {
            if (Vector3.Distance(spawnPoints[i].position, position) < 0.1f)
            {
                usedIndices.Remove(i);
                break;
            }
        }
    }

    // Reset all spawn points (for game restart)
    public void ResetSpawnPoints()
    {
        usedSpawnIndices[NetworkTankPlayer.Team.None].Clear();
        usedSpawnIndices[NetworkTankPlayer.Team.Team1].Clear();
        usedSpawnIndices[NetworkTankPlayer.Team.Team2].Clear();
    }
}