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
