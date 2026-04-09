using UnityEngine;

// INVESTIGATE — Sentinel moves to the last known noise or sight position
// Transitions:
//   -> Combat   : confirms player sighting while investigating
//   -> Patrol   : reaches the location and memory timer runs out
public class SentinelInvestigateState : State
{
    CryptSentinel sentinel;
    float memoryTimer;
    bool arrived;

    public SentinelInvestigateState(CryptSentinel _sentinel)
    {
        sentinel = _sentinel;
    }

    public override void Enter()
    {
        sentinel.agent.speed = sentinel.patrolSpeed;
        memoryTimer = sentinel.memoryDuration;
        arrived = false;
        sentinel.agent.SetDestination(sentinel.lastKnownPlayerPos);
    }

    public override void Execute()
    {
        // If player steps into the cone during investigation, escalate to Combat
        if (sentinel.vision.CanSee(sentinel.playerTransform))
        {
            sentinel.lastKnownPlayerPos = sentinel.playerTransform.position;
            sentinel.ChangeState(sentinel.combatState);
            return;
        }

        // Check if we have arrived at the investigation point
        if (!arrived && !sentinel.agent.pathPending && sentinel.agent.remainingDistance < 0.4f)
            arrived = true;

        // Once arrived, count down the memory timer
        if (arrived)
        {
            memoryTimer -= Time.deltaTime;
            if (memoryTimer <= 0f)
                sentinel.ChangeState(sentinel.patrolState);
        }
    }

    public override void Exit() { }

    public override string ToString() => "Investigate";
}
