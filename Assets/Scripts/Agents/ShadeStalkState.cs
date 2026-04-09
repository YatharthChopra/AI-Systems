using UnityEngine;

// STALK — Shade silently moves toward the confirmed sound origin
// Also fires the shared position event so the Sentinel can update its memory
// Transitions:
//   -> Hunt    : player comes within huntRange
//   -> Retreat : torch is lit nearby (handled by TheShade.OnTorchToggled)
//   -> Drift   : loses the location (no player nearby after arriving)
public class ShadeStalkState : State
{
    TheShade shade;

    public ShadeStalkState(TheShade _shade)
    {
        shade = _shade;
    }

    public override void Enter()
    {
        shade.agent.speed = shade.driftSpeed;
        shade.agent.SetDestination(shade.lastSoundPos);

        // Share confirmed position with the Sentinel
        GameEvents.OnShadeSharesPlayerPosition?.Invoke(shade.lastSoundPos);
    }

    public override void Execute()
    {
        if (shade.playerTransform == null) return;

        float distToPlayer = Vector3.Distance(shade.transform.position, shade.playerTransform.position);

        // Close enough — switch to Hunt
        if (distToPlayer <= shade.huntRange)
        {
            shade.ChangeState(shade.huntState);
            return;
        }

        // Arrived at last sound pos but player is gone — go back to drifting
        if (!shade.agent.pathPending && shade.agent.remainingDistance < 0.4f)
            shade.ChangeState(shade.driftState);
    }

    public override void Exit() { }

    public override string ToString() => "Stalk";
}
