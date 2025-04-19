using UnityEngine;
using Mirror;
using UnityEngine.UI;
using TMPro;

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
        Debug.Log($"Team changed from {oldTeam} to {newTeam}");

        // Update the team color based on the new team
        UpdateTeamColor();

        // Update player UI if available
        if (playerUI != null)
        {
            playerUI.SetTeam(newTeam.ToString());
        }

        // Force a visual update on the client
        if (isClient)
        {
            UpdateVisualColor();
        }
    }
    void OnTeamColorChanged(Color oldColor, Color newColor)
    {
        Debug.Log($"Team color changed from {oldColor} to {newColor}");

        UpdateVisualColor();
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

    void FixedUpdate()
    {
        if (isLocalPlayer) return;

        // Smooth interpolation for other clients
        transform.position = Vector3.Lerp(transform.position, syncPosition, Time.deltaTime * 10f);
        transform.rotation = Quaternion.Lerp(transform.rotation, syncRotation, Time.deltaTime * 10f);
    }

    void MoveAndRotate()
    {
        float move = Input.GetAxis("Vertical") * moveSpeed * Time.deltaTime;
        float turn = Input.GetAxis("Horizontal") * turnSpeed * Time.deltaTime;

        // Rotate around the Y-axis
        transform.Rotate(0, turn, 0);

        // Move forward/backward along local forward (XZ plane)
        transform.Translate(Vector3.forward * move);
    }

    [Command]
    void CmdSendPositionAndRotation(Vector3 position, Quaternion rotation)
    {
        syncPosition = position;
        syncRotation = rotation;
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


    [Command]
    public void CmdSetPlayerTeam(Team team)
    {
        playerTeam = team;
        // Team color is updated via the SyncVar hook
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
}