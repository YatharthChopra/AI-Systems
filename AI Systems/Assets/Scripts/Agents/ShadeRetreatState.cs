using UnityEngine;

// RETREAT — Shade exposed to torchlight, flees to the nearest shadow
// Transitions:
//   -> Drift : torch is extinguished OR safe distance reached
public class ShadeRetreatState : State
{
    TheShade shade;

    public ShadeRetreatState(TheShade _shade)
    {
        shade = _shade;
    }

    public override void Enter()
    {
        shade.agent.speed = shade.huntSpeed;

        // Flee directly away from the player
        Vector3 fleeDir = (shade.transform.position - shade.playerTransform.position).normalized;
        Vector3 fleeTarget = shade.transform.position + fleeDir * shade.retreatRadius;

        UnityEngine.AI.NavMeshHit hit;
        if (UnityEngine.AI.NavMesh.SamplePosition(fleeTarget, out hit, shade.retreatRadius, UnityEngine.AI.NavMesh.AllAreas))
            shade.agent.SetDestination(hit.position);

        // Listen for torch being extinguished
        GameEvents.OnTorchToggled += OnTorchToggled;
    }

    public override void Execute()
    {
        float distToPlayer = Vector3.Distance(shade.transform.position, shade.playerTransform.position);

        // Safe distance reached
        if (distToPlayer >= shade.retreatRadius)
            shade.ChangeState(shade.driftState);
    }

    public override void Exit()
    {
        GameEvents.OnTorchToggled -= OnTorchToggled;
    }

    void OnTorchToggled(bool isLit)
    {
        // Torch went out — no longer fleeing
        if (!isLit)
            shade.ChangeState(shade.driftState);
    }

    public override string ToString() => "Retreat";
}
