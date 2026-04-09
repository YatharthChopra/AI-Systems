using System;
using UnityEngine;

// Central event hub — all AI and player events are defined here as C# Actions
// Subscribe in LevelManager, unsubscribe on destroy to avoid memory leaks
public static class GameEvents
{
    // Fired when any object makes a sound (position, intensity 0-1)
    public static Action<Vector3, float> OnSoundEmitted;

    // Fired when the player toggles their torch on (true) or off (false)
    public static Action<bool> OnTorchToggled;

    // Fired by Sentinel during Rallying Cry — Shade listens and enters Rallied
    public static Action<Vector3> OnSentinelRallyingCry;

    // Fired by Shade when it confirms player position — shared back to Sentinel
    public static Action<Vector3> OnShadeSharesPlayerPosition;

    // Fired when the player lands a hit on the Sentinel
    public static Action OnPlayerHitSentinel;

    // Fired when Sentinel HP drops to the rally threshold
    public static Action OnSentinelLowHP;

    // Fired when Shade makes contact with the player (stamina drain)
    public static Action OnShadeContactPlayer;
}
