using UnityEngine;

// RALLIED — Shade summoned by Sentinel's Rallying Cry, immediately starts hunting
// Enters Stalk then Hunt toward the player
// Transitions:
//   -> Stalk : immediately on enter
public class ShadeRalliedState : State
{
    TheShade shade;

    public ShadeRalliedState(TheShade _shade)
    {
        shade = _shade;
    }

    public override void Enter()
    {
        Debug.Log("[Shade] Rallied by Sentinel!");

        // Go straight to stalk toward the player's current position
        if (shade.playerTransform != null)
            shade.lastSoundPos = shade.playerTransform.position;

        shade.ChangeState(shade.stalkState);
    }

    public override void Execute() { }

    public override void Exit() { }

    public override string ToString() => "Rallied";
}
