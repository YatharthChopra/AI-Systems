using UnityEngine;

// RETURN — Sentinel lost the player and walks back to its first waypoint
// Transitions:
//   -> Patrol : reaches the waypoint
public class SentinelReturnState : State
{
    CryptSentinel sentinel;

    public SentinelReturnState(CryptSentinel _sentinel)
    {
        sentinel = _sentinel;
    }

    public override void Enter()
    {
        sentinel.agent.speed = sentinel.patrolSpeed;
        sentinel.patrolIndex = 0;

        // Head back to the start of the patrol route
        if (sentinel.patrolWaypoints.Length > 0)
            sentinel.agent.SetDestination(sentinel.patrolWaypoints[0].position);
    }

    public override void Execute()
    {
        // Switch back to patrol once home
        if (!sentinel.agent.pathPending && sentinel.agent.remainingDistance < 0.4f)
            sentinel.ChangeState(sentinel.patrolState);
    }

    public override void Exit() { }

    public override string ToString() => "Return";
}
