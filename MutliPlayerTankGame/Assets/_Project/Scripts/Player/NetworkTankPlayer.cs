using UnityEngine;
using Mirror;
using UnityEngine.UI;

public class NetworkTankPlayer : NetworkBehaviour
{
    public enum Team { None, Team1, Team2 }

    [SyncVar]
    public Team playerTeam = Team.None;
    public Color teamColor;

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
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        playerUI.SetTeamColor(teamColor);

        playerUI = GetComponentInChildren<PlayerUI>();
        if (playerUI != null)
        {
            playerUI.SetTeam(playerTeam.ToString());

            var health = GetComponent<PlayerHealth>();
            if (health != null)
                playerUI.SetHealth(health.health);
        }

        var healthComponent = GetComponent<PlayerHealth>();
        if (healthComponent != null)
            healthComponent.owner = this;
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        MoveAndRotate();
        CmdSendPositionAndRotation(transform.position, transform.rotation);

        if (isLocalPlayer && Input.GetKeyDown(KeyCode.K))
        {
            GetComponent<PlayerHealth>().TakeDamage(20f);
        }
        if (!isLocalPlayer) return;

        MoveAndRotate();
        CmdSendPositionAndRotation(transform.position, transform.rotation);

        // Check for firing input
        if (Input.GetKeyDown(KeyCode.Space))
        {
            CmdFireBullet();
        }

        if (isLocalPlayer && Input.GetKeyDown(KeyCode.K))
        {
            GetComponent<PlayerHealth>().TakeDamage(20f);
        }
    }

    void FixedUpdate()
    {
        if (isLocalPlayer) return;

        transform.position = Vector3.Lerp(transform.position, syncPosition, Time.deltaTime * 10f);
        transform.rotation = Quaternion.Lerp(transform.rotation, syncRotation, Time.deltaTime * 10f);
    }

    void MoveAndRotate()
    {
        float move = Input.GetAxis("Vertical") * moveSpeed * Time.deltaTime;
        float turn = Input.GetAxis("Horizontal") * turnSpeed * Time.deltaTime;

        transform.Translate(0, 0, move);
        transform.Rotate(0, turn, 0);
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



    [Command]
    void CmdSetPlayerTeam(Team team)
    {
        playerTeam = team;

        if (playerTeam == Team.Team1)
            teamColor = Color.red;
        else if (playerTeam == Team.Team2)
            teamColor = Color.blue;

        RpcUpdatePlayerColor(teamColor);
    }

    [ClientRpc]
    void RpcUpdatePlayerColor(Color color)
    {
        GetComponent<Renderer>().material.color = color;
    }

    public void OnTeamSelected(int teamIndex)
    {
        if (isLocalPlayer)
        {
            playerTeam = (Team)teamIndex;
            CmdSetPlayerTeam(playerTeam);
        }
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
    }

    [Command]
    void CmdRevivePlayer(NetworkIdentity playerIdentity)
    {
        RpcRevivePlayer(playerIdentity);
    }

    [ClientRpc]
    void RpcRevivePlayer(NetworkIdentity playerIdentity)
    {
        Debug.Log($"Player {playerIdentity.gameObject.name} has been revived.");
    }

    public void OnPlayerDeath(string killerName)
    {
        CmdPlayerDied(killerName, gameObject.name);
    }

    bool CanRevive(NetworkIdentity playerToRevive)
    {
        return true;
    }
}
