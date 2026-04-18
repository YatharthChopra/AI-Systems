using UnityEngine;
using UnityEngine.UI;

// Updates the stamina fill bar every frame based on the player's current stamina
// Assign the player and fill Image reference in the Inspector (or via SceneSetup)
public class StaminaBar : MonoBehaviour
{
    [Header("References")]
    public PlayerController player;
    public Image fillImage;

    // --- UNITY LIFECYCLE ---

    void Update()
    {
        if (player == null || fillImage == null) return;

        // Scale the fill width to match the stamina percentage (0-1)
        fillImage.rectTransform.localScale = new Vector3(player.GetStaminaPercent(), 1f, 1f);
    }
}
