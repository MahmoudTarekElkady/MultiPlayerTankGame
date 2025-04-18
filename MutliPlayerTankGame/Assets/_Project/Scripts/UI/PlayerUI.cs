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
        Debug.Log($"PlayerUI started for {transform.parent?.name ?? gameObject.name}");

        if (healthSlider == null)
        {
            Debug.LogError($"Health slider reference missing on PlayerUI for {transform.parent?.name ?? gameObject.name}");
        }

        if (teamText == null)
        {
            Debug.LogError($"Team text reference missing on PlayerUI for {transform.parent?.name ?? gameObject.name}");
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
            float normalizedHealth = health / maxHealth;
            healthSlider.value = normalizedHealth;
            Debug.Log($"Set health UI for {transform.parent?.name ?? gameObject.name} to {health}/{maxHealth} = {normalizedHealth}");
        }
        else
        {
            Debug.LogError($"Health slider is null on PlayerUI for {transform.parent?.name ?? gameObject.name}");
        }
    }

    public void SetTeam(string team)
    {
        if (teamText != null)
        {
            teamText.text = $"Team: {team}";
            Debug.Log($"Set team text for {transform.parent?.name ?? gameObject.name} to {team}");
        }
        else
        {
            Debug.LogError($"Team text is null on PlayerUI for {transform.parent?.name ?? gameObject.name}");
        }
    }
}