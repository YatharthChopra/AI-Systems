using UnityEngine;

// ALERT — Shade heard a sound and orients toward it, deciding whether to stalk
// Transitions:
//   -> Stalk : location confirmed (Shade reaches the sound origin)
//   -> Drift : sound fades after soundFadeTime seconds
public class ShadeAlertState : State
{
    TheShade shade;
    float fadeTimer;

    public ShadeAlertState(TheShade _shade)
    {
        shade = _shade;
    }

    public override void Enter()
    {
        shade.agent.speed = shade.driftSpeed;
        fadeTimer = shade.soundFadeTime;
        shade.agent.SetDestination(shade.lastSoundPos);
    }

    public override void Execute()
    {
        fadeTimer -= Time.deltaTime;

        // If we reach the sound source, start stalking
        if (!shade.agent.pathPending && shade.agent.remainingDistance < 0.5f)
        {
            shade.ChangeState(shade.stalkState);
            return;
        }

        // Sound faded before we found anything — return to drifting
        if (fadeTimer <= 0f)
            shade.ChangeState(shade.driftState);
    }

    public override void Exit() { }

    public override string ToString() => "Alert";
}
