using TMPro;
using UnityEngine;
using Mirror;

public class PlayerUI : NetworkBehaviour
{
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI teamText;
    public Transform uiCanvas;

    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;

        // Hide UI canvas if this is not the local player
        if (!isLocalPlayer && uiCanvas != null)
        {
            uiCanvas.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        // Make the UI face the camera
        if (uiCanvas != null && mainCamera != null)
        {
            uiCanvas.rotation = Quaternion.LookRotation(uiCanvas.position - mainCamera.transform.position);
        }
    }

    public void SetHealth(float health)
    {
        if (healthText != null)
            healthText.text = $"Health: {health}";
    }

    public void SetTeam(string team)
    {
        if (teamText != null)
            teamText.text = $"Team: {team}";
    }
}
