using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class PlayerUI : NetworkBehaviour
{
    [Header("References")]
    public Slider healthSlider;
    public TextMeshProUGUI teamText;
    public Transform uiCanvas;

    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;

        // Only enable UI for local player
        if (!isLocalPlayer && uiCanvas != null)
        {
            uiCanvas.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (uiCanvas != null && mainCamera != null)
        {
            // Face the camera
            uiCanvas.rotation = Quaternion.LookRotation(uiCanvas.position - mainCamera.transform.position);
        }
    }

    public void SetHealth(float health, float maxHealth = 100f)
    {
        if (healthSlider != null)
        {
            healthSlider.value = health / maxHealth;
        }
    }

    public void SetTeam(string team)
    {
        if (teamText != null)
            teamText.text = $"Team: {team}";
    }
}
