using UnityEngine;
using Mirror;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class NetworkTankPlayer : NetworkBehaviour
{
    public enum Team { None, Team1, Team2 }

    [SyncVar(hook = nameof(OnTeamChanged))]
    public Team playerTeam = Team.None;

    // Make teamColor a SyncVar too, so it syncs directly
    [SyncVar(hook = nameof(OnTeamColorChanged))]
    public Color teamColor = Color.white;
    
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float turnSpeed = 100f;

    [SyncVar] private Vector3 syncPosition;
    [SyncVar] private Quaternion syncRotation;

    private Camera mainCamera;
    private Text killText;

    private float reviveTime = 7f;

    public PlayerUI playerUI;

    public GameObject bulletPrefab;
    public Transform firePoint;

    [Header("Team Colors")]
    public Color team1Color = Color.red;
    public Color team2Color = Color.blue;
    public Color defaultColor = Color.white;


    void Awake()
    {
        // Register with game manager when created
        if (isServer)
        {
            GameManager gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                gameManager.RegisterPlayer(this);
            }
        }
    }
    void Start()
    {
        if (isLocalPlayer)
        {
            mainCamera = Camera.main;
            if (mainCamera != null)
            {
                var cameraFollow = mainCamera.GetComponent<CameraFollowTopDown>();
                if (cameraFollow != null)
                    cameraFollow.target = transform;
            }

            GameObject killTextObject = GameObject.Find("KillText");
            if (killTextObject != null)
            {
                killText = killTextObject.GetComponent<Text>();
            }
            else
            {
                Debug.LogError("KillText UI element not found!");
            }

            // Show team selection UI if we're local player
            ShowTeamSelectionUI();
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        // Get the PlayerUI component
        playerUI = GetComponentInChildren<PlayerUI>();

        if (playerUI != null)
        {
            // Set the team color and team name in UI
            playerUI.SetTeam(playerTeam.ToString());

            var health = GetComponent<PlayerHealth>();
            if (health != null)
                playerUI.SetHealth(health.health);
        }

        var healthComponent = GetComponent<PlayerHealth>();
        if (healthComponent != null)
            healthComponent.owner = this;

        // Initialize renderer color
        UpdateVisualColor();
    }


    // Hook called when team changes
    void OnTeamChanged(Team oldTeam, Team newTeam)
    {
        Debug.Log($"Team changed from {oldTeam} to {newTeam} for {gameObject.name}");

        // Update team color based on the team
        UpdateTeamColorFromTeam();

        // Apply the color to all renderers
        ApplyTeamColorToRenderers();

        // Update UI if available
        if (playerUI != null)
        {
            playerUI.SetTeam(newTeam.ToString());
        }

        // Notify game manager
        if (isServer)
        {
            GameManager gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                gameManager.UpdatePlayerTeam(this, oldTeam, newTeam);
            }
        }
    }
    void OnTeamColorChanged(Color oldColor, Color newColor)
    {
        Debug.Log($"Team color changed from {oldColor} to {newColor} for {gameObject.name}");

        // Apply the new color to all renderers
        ApplyTeamColorToRenderers();
    }


    // Update player's visual color
    void UpdateVisualColor()
    {
        var renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = teamColor;
        }
    }

    // Add this method to NetworkTankPlayer.cs
    void Update()
    {
        if (!isLocalPlayer) return;

        MoveAndRotate();
        CmdSendPositionAndRotation(transform.position, transform.rotation);

        // Check for firing input
        if (Input.GetKeyDown(KeyCode.Space))
        {
            CmdFireBullet();
        }

        // Debug: Apply damage to self with T key
        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log("DEBUG: Self-damage triggered");
            var health = GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage(20f, this);
            }
        }

        // Debug: Apply damage to others with Y key
        if (Input.GetKeyDown(KeyCode.Y))
        {
            Debug.Log("DEBUG: Attempting to damage all other players");
            var players = FindObjectsOfType<NetworkTankPlayer>();
            foreach (var player in players)
            {
                if (player != this)
                {
                    CmdDebugDamagePlayer(player.netId);
                }
            }
        }
    }
    void UpdateTeamColorFromTeam()
    {
        switch (playerTeam)
        {
            case Team.Team1:
                teamColor = team1Color;
                break;
            case Team.Team2:
                teamColor = team2Color;
                break;
            default:
                teamColor = defaultColor;
                break;
        }
    }
    void ApplyTeamColorToRenderers()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        foreach (var renderer in renderers)
        {
            // Skip renderers with specific tags if needed
            if (renderer.CompareTag("DoNotColorize"))
                continue;

            renderer.material.color = teamColor;
        }

        Debug.Log($"Applied color {teamColor} to {renderers.Length} renderers on {gameObject.name}");
    }


    [Command]
    void CmdDebugDamagePlayer(uint targetNetId)
    {
        NetworkIdentity targetIdentity = NetworkServer.spawned[targetNetId];
        if (targetIdentity != null)
        {
            PlayerHealth targetHealth = targetIdentity.GetComponent<PlayerHealth>();
            if (targetHealth != null)
            {
                Debug.Log($"SERVER: Applying debug damage to {targetIdentity.name}");
                targetHealth.TakeDamage(20f, this);
            }
        }
    }

 
    void MoveAndRotate()
    {
        // Add comprehensive debug logs
        if (!isLocalPlayer)
        {
            Debug.Log("Movement blocked - not local player");
            return;
        }

        // More detailed game state check
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager instance is null!");
            return;
        }

        string currentState = GameManager.Instance.GetCurrentGameState();
        Debug.Log($"Current game state: {currentState}");

        if (currentState != "Playing")
        {
            Debug.Log($"Movement blocked - game state is {currentState}");
            return;
        }

        if (!enabled)
        {
            Debug.LogError("NetworkTankPlayer component is disabled!");
            return;
        }

        // Actual movement code
        float move = Input.GetAxis("Vertical") * moveSpeed * Time.deltaTime;
        float turn = Input.GetAxis("Horizontal") * turnSpeed * Time.deltaTime;

        Debug.Log($"Movement input - vertical: {move}, horizontal: {turn}");

        transform.Rotate(0, turn, 0);
        transform.Translate(Vector3.forward * move);
    }


    private IEnumerator DelayedTeamUIShow()
    {
        yield return new WaitForSeconds(0.1f); // Small delay to ensure UI is ready

        TeamSelectionUI teamUI = TeamSelectionUI.Instance ??
                               FindObjectOfType<TeamSelectionUI>(true);

        if (teamUI != null)
        {
            teamUI.gameObject.SetActive(true);
            teamUI.ShowUI();
        }
    }

    // In NetworkTankPlayer.cs
    [Command]
    void CmdSendPositionAndRotation(Vector3 position, Quaternion rotation)
    {
        // Only update if the difference is significant
        if (Vector3.Distance(syncPosition, position) > 0.1f ||
            Quaternion.Angle(syncRotation, rotation) > 1f)
        {
            syncPosition = position;
            syncRotation = rotation;
            RpcSyncTransform(position, rotation);
        }
    }

    [ClientRpc]
    void RpcSyncTransform(Vector3 position, Quaternion rotation)
    {
        if (!isLocalPlayer)
        {
            transform.position = position;
            transform.rotation = rotation;
        }
    }

    void FixedUpdate()
    {
        if (isLocalPlayer)
        {
            CmdSendPositionAndRotation(transform.position, transform.rotation);
        }
        else
        {
            // Smooth interpolation for other clients
            transform.position = Vector3.Lerp(transform.position, syncPosition, Time.deltaTime * 10f);
            transform.rotation = Quaternion.Lerp(transform.rotation, syncRotation, Time.deltaTime * 10f);
        }
    }

    [Command]
    void CmdFireBullet()
    {
        if (bulletPrefab == null || firePoint == null)
        {
            Debug.LogError("Missing bulletPrefab or firePoint reference on NetworkTankPlayer!");
            return;
        }

        // Instantiate the bullet on the server
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);

        // Set the shooter/owner before spawning
        Bullet bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript != null)
        {
            bulletScript.owner = this; // Reference to the shooter for damage attribution
            Debug.Log($"Bullet fired by {gameObject.name} on team {playerTeam}");
        }
        else
        {
            Debug.LogWarning("Bullet prefab is missing Bullet script!");
        }

        // Spawn bullet for all clients with ownership
        NetworkServer.Spawn(bullet, connectionToClient);

        // Set the bullet's velocity (move it forward)
        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = bullet.transform.forward * bulletScript.speed;
        }
        else
        {
            Debug.LogWarning("Missing Rigidbody on bullet prefab.");
        }
    }
    // Helper method to set the correct color based on team
    private void UpdateTeamColor()
    {
        switch (playerTeam)
        {
            case Team.Team1:
                teamColor = team1Color;
                break;
            case Team.Team2:
                teamColor = team2Color;
                break;
            default:
                teamColor = defaultColor;
                break;
        }
        Debug.Log($"Team color updated to {teamColor} for team {playerTeam}");
    }
    [ClientRpc]
public void RpcFullReset()
{
    if (isLocalPlayer)
    {
        ClientReset();
    }
}


[Command]
public void CmdRequestMovementReset()
{
    RpcResetMovement();
}

[ClientRpc]
public void RpcResetMovement()
{
    if (isLocalPlayer)
    {
        Debug.Log("Resetting movement state for local player");
        enabled = true;
        
        // Force input reset
        // (Add any input-specific reset logic here)
    }
}

    [Command]
    public void CmdSetPlayerTeam(Team team)
    {
        Debug.Log($"CmdSetPlayerTeam: Setting {gameObject.name} to team {team}");

        // Set the new team
        playerTeam = team;

        // Update team color on server
        UpdateTeamColorFromTeam();

        // Force sync colors to all clients
        RpcSyncTeamColors();
    }

    // Called from UI dropdown or button
    public void OnTeamSelected(int teamIndex)
    {
        if (!isLocalPlayer) return;

        Team selectedTeam = (Team)teamIndex;
        Debug.Log($"Team selected: {selectedTeam}");
        CmdSetPlayerTeam(selectedTeam);

        // Also update the visual color immediately on the client
        switch (selectedTeam)
        {
            case Team.Team1:
                teamColor = team1Color;
                break;
            case Team.Team2:
                teamColor = team2Color;
                break;
            default:
                teamColor = defaultColor;
                break;
        }

        // Update the renderer color
        var renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = teamColor;
        }

        // Hide team selection UI after selection
        HideTeamSelectionUI();
    }

    [Command]
    public void CmdPlayerDied(string killerName, string deadPlayerName)
    {
        RpcDisplayKillMessage(killerName, deadPlayerName);
    }

    [ClientRpc]
    void RpcDisplayKillMessage(string killerName, string deadPlayerName)
    {
        if (killText != null)
        {
            killText.text += $"\n{killerName} killed {deadPlayerName}";
        }

        // Optional: Also log to console for debugging
        Debug.Log($"{killerName} killed {deadPlayerName}");
    }

    [Command]
    void CmdRevivePlayer(NetworkIdentity playerIdentity)
    {
        // Check if the player is on the same team
        var playerToRevive = playerIdentity.GetComponent<NetworkTankPlayer>();
        if (playerToRevive != null && playerToRevive.playerTeam == this.playerTeam)
        {
            var playerHealth = playerIdentity.GetComponent<PlayerHealth>();
            if (playerHealth != null && playerHealth.health <= 0)
            {
                // Revive the player with half health
                playerHealth.health = 50f;
                RpcRevivePlayer(playerIdentity);
            }
        }
    }
    [Command]
    public void CmdRequestRestart()
    {
        // Only allow restart if in GameOver state or if server/host
        GameManager gameManager = GameManager.Instance;
        if (gameManager != null)
        {
            string currentState = gameManager.GetCurrentGameState();
            Debug.Log($"Restart requested in state: {currentState}");

            // Allow restart in GameOver state or if player has authority
            if (currentState == "GameOver" || isLocalPlayer)
            {
                gameManager.RestartGame();
            }
        }
    }

    [ClientRpc]
    public void RpcResetPosition()
    {
        // Reset to spawn position or a predefined location
        // This is optional - you might want dedicated spawn points
        transform.position = new Vector3(Random.Range(-10, 10), 0, Random.Range(-10, 10));
        transform.rotation = Quaternion.identity;
    }
    [ClientRpc]
    void RpcRevivePlayer(NetworkIdentity playerIdentity)
    {
        Debug.Log($"Player {playerIdentity.gameObject.name} has been revived.");

        // Re-enable player components
        var playerHealth = playerIdentity.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            // Reset visual state
            var renderer = playerIdentity.GetComponentInChildren<MeshRenderer>();
            if (renderer != null)
                renderer.enabled = true;

            // Re-enable controller
            var controller = playerIdentity.GetComponent<NetworkTankPlayer>();
            if (controller != null)
                controller.enabled = true;
        }
    }
    [ClientRpc]
    public void RpcSyncTeamColors()
    {
        // Update the team color based on current team
        UpdateTeamColorFromTeam();

        // Apply the color to all renderers
        ApplyTeamColorToRenderers();

        Debug.Log($"RpcSyncTeamColors: {gameObject.name} is on team {playerTeam} with color {teamColor}");
    }

    [ClientRpc]
    public void RpcResetMovementState()
    {
        if (isLocalPlayer)
        {
            // Re-enable input processing
            enabled = true;

            // Reset any movement variables
            Debug.Log("Movement state reset");
        }
    }

    [TargetRpc]
    public void TargetResetCameraFollow(NetworkConnection target)
    {
        if (isLocalPlayer && mainCamera != null)
        {
            var cameraFollow = mainCamera.GetComponent<CameraFollowTopDown>();
            if (cameraFollow != null)
            {
                cameraFollow.target = transform;
                Debug.Log("Camera follow reset");
            }
        }
    }
    public void OnPlayerDeath(string killerName)
    {
        CmdPlayerDied(killerName, gameObject.name);
    }

    // Check if can revive a player
    bool CanRevive(NetworkIdentity playerToRevive)
    {
        var player = playerToRevive.GetComponent<NetworkTankPlayer>();
        return player != null && player.playerTeam == this.playerTeam;
    }

    // Team selection UI methods
    void ShowTeamSelectionUI()
    {
        // First try to find the UI through the singleton
        TeamSelectionUI teamUI = TeamSelectionUI.Instance;

        // If not found through singleton, try to find by name
        if (teamUI == null)
        {
            GameObject teamSelectionUI = GameObject.Find("TeamSelectionUI");
            if (teamSelectionUI != null)
            {
                teamUI = teamSelectionUI.GetComponent<TeamSelectionUI>();
                if (teamUI != null)
                {
                    teamUI.ShowUI();
                }
                else
                {
                    teamSelectionUI.SetActive(true);
                    Debug.LogWarning("TeamSelectionUI component not found but GameObject was activated");
                }
            }
            else
            {
                Debug.LogError("TeamSelectionUI not found in scene! Make sure it exists and has the correct name.");
            }
        }
        else
        {
            teamUI.ShowUI();
        }
    }

    void HideTeamSelectionUI()
    {
        // First try to find the UI through the singleton
        TeamSelectionUI teamUI = TeamSelectionUI.Instance;

        // If not found through singleton, try to find by name
        if (teamUI == null)
        {
            GameObject teamSelectionUI = GameObject.Find("TeamSelectionUI");
            if (teamSelectionUI != null)
            {
                teamSelectionUI.SetActive(false);
            }
        }
        else
        {
            teamUI.gameObject.SetActive(false);
        }
    }

    // Add a method to handle revive zone detection
    public void StartReviveProcess(NetworkIdentity deadPlayer)
    {
        if (!isLocalPlayer) return;

        // Check if the dead player is a teammate
        if (CanRevive(deadPlayer))
        {
            // Start the revive process
            CmdRevivePlayer(deadPlayer);
        }
    }
    // In NetworkTankPlayer.cs
    [ClientRpc]
    public void RpcFullPlayerReset(Vector3 spawnPosition)
    {
        transform.position = spawnPosition;
        transform.rotation = Quaternion.identity;

        if (isLocalPlayer)
        {
            ClientReset();

            // Force camera update
            if (mainCamera != null)
            {
                var camFollow = mainCamera.GetComponent<CameraFollowTopDown>();
                if (camFollow != null)
                {
                    camFollow.target = transform;
                    camFollow.transform.position = transform.position + new Vector3(0, 10, -7);
                }
            }
        }

        // Reset physics on all clients
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
    }

    public void ClientReset()
    {
        Debug.Log("CLIENT RESET");

        // 1. Enable components
        enabled = true;
        GetComponent<PlayerHealth>().enabled = true;

        // 2. Reset input
        // (Add any input-specific reset logic here)

        // 3. Reset physics
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
    }
}