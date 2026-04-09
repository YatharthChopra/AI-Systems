using UnityEngine;

// LevelManager — wires all events between agents and player
// This is the single place where subscriptions are connected and disconnected
// Keeps agents decoupled from each other — they only communicate through GameEvents
public class LevelManager : MonoBehaviour
{
    [Header("References")]
    public CryptSentinel sentinel;
    public TheShade shade;
    public PlayerController player;

    void OnEnable()
    {
        // Wire Sentinel events
        GameEvents.OnPlayerHitSentinel     += sentinel.OnHitByPlayer;
        GameEvents.OnSentinelLowHP         += sentinel.OnLowHP;
        GameEvents.OnShadeSharesPlayerPosition += sentinel.OnShadeSharedPosition;

        // Wire Shade events
        GameEvents.OnSentinelRallyingCry   += shade.OnRallyingCry;
        GameEvents.OnTorchToggled          += shade.OnTorchToggled;
    }

    void OnDisable()
    {
        // Always unsubscribe to prevent memory leaks when scene unloads
        GameEvents.OnPlayerHitSentinel     -= sentinel.OnHitByPlayer;
        GameEvents.OnSentinelLowHP         -= sentinel.OnLowHP;
        GameEvents.OnShadeSharesPlayerPosition -= sentinel.OnShadeSharedPosition;

        GameEvents.OnSentinelRallyingCry   -= shade.OnRallyingCry;
        GameEvents.OnTorchToggled          -= shade.OnTorchToggled;
    }

    // Called from a UI button or player attack script when the player hits the Sentinel
    public void NotifyPlayerHitSentinel()
    {
        GameEvents.OnPlayerHitSentinel?.Invoke();
    }
}
