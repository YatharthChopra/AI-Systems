using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

// Handles win and lose conditions — subscribes to OnPlayerCaught and OnPlayerEscaped
// Freezes time and shows an overlay message; R key reloads the scene to restart
public class GameManager : MonoBehaviour
{
    [Header("UI Reference")]
    public TextMeshProUGUI overlayText;

    bool gameEnded = false;

    // --- UNITY LIFECYCLE ---

    void Start()
    {
        // Reset time scale in case we loaded from a frozen previous run
        Time.timeScale = 1f;
        if (overlayText != null)
            overlayText.gameObject.SetActive(false);
    }

    void OnEnable()
    {
        GameEvents.OnPlayerCaught  += HandleCaught;
        GameEvents.OnPlayerEscaped += HandleEscaped;
    }

    void OnDisable()
    {
        GameEvents.OnPlayerCaught  -= HandleCaught;
        GameEvents.OnPlayerEscaped -= HandleEscaped;
    }

    void OnDestroy()
    {
        // Restore time so the reloaded scene isn't frozen from the start
        Time.timeScale = 1f;
    }

    void Update()
    {
        if (gameEnded && Input.GetKeyDown(KeyCode.R))
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // --- EVENT HANDLERS ---

    void HandleCaught()
    {
        if (gameEnded) return;
        EndGame("CAUGHT BY THE SENTINEL\n<size=60%>Press R to try again</size>",
                new Color(1f, 0.15f, 0.10f, 1f));
    }

    void HandleEscaped()
    {
        if (gameEnded) return;
        EndGame("YOU ESCAPED THE VAULT!\n<size=60%>Press R to play again</size>",
                new Color(0.20f, 1f, 0.30f, 1f));
    }

    // --- HELPERS ---

    void EndGame(string message, Color colour)
    {
        gameEnded      = true;
        Time.timeScale = 0f;

        if (overlayText == null) return;
        overlayText.text  = message;
        overlayText.color = colour;
        overlayText.gameObject.SetActive(true);
    }
}
