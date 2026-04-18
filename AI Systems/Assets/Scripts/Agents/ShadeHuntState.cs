using UnityEngine;

// HUNT — Shade locks onto the player and closes fast, draining stamina on contact
// Transitions:
//   -> Retreat : torch is lit nearby (handled by TheShade.OnTorchToggled)
//   -> Stalk   : player escapes beyond 5m
public class ShadeHuntState : State
{
    TheShade shade;
    float escapeRange = 5f;

    public ShadeHuntState(TheShade _shade)
    {
        shade = _shade;
    }

    public override void Enter()
    {
        shade.agent.speed = shade.huntSpeed;
        Debug.Log("[Shade] Hunting player!");
    }

    public override void Execute()
    {
        if (shade.playerTransform == null) return;

        float distToPlayer = Vector3.Distance(shade.transform.position, shade.playerTransform.position);

        // Chase the player
        shade.agent.SetDestination(shade.playerTransform.position);

        // Drain stamina on contact
        if (distToPlayer <= 0.8f)
            GameEvents.OnShadeContactPlayer?.Invoke();

        // Player escaped — drop back to stalk
        if (distToPlayer > escapeRange)
            shade.ChangeState(shade.stalkState);
    }

    public override void Exit()
    {
        shade.agent.speed = shade.driftSpeed;
    }

    public override string ToString() => "Hunt";
}
