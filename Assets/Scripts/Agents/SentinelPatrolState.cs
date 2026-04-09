using UnityEngine;

// PATROL — Sentinel walks between waypoints and scans with its vision cone
// Transitions:
//   -> Investigate : hears a sound nearby OR sees the player
public class SentinelPatrolState : State
{
    CryptSentinel sentinel;

    public SentinelPatrolState(CryptSentinel _sentinel)
    {
        sentinel = _sentinel;
    }

    public override void Enter()
    {
        sentinel.agent.speed = sentinel.patrolSpeed;

        // Listen for sound events while on patrol
        GameEvents.OnSoundEmitted += OnSoundHeard;

        GoToCurrentWaypoint();
    }

    public override void Execute()
    {
        // Vision check every frame
        if (sentinel.vision.CanSee(sentinel.playerTransform))
        {
            sentinel.lastKnownPlayerPos = sentinel.playerTransform.position;
            sentinel.ChangeState(sentinel.investigateState);
            return;
        }

        // Move to next waypoint when close enough to the current one
        if (!sentinel.agent.pathPending && sentinel.agent.remainingDistance < 0.4f)
        {
            sentinel.patrolIndex = (sentinel.patrolIndex + 1) % sentinel.patrolWaypoints.Length;
            GoToCurrentWaypoint();
        }
    }

    public override void Exit()
    {
        // Always unsubscribe when leaving this state
        GameEvents.OnSoundEmitted -= OnSoundHeard;
    }

    void GoToCurrentWaypoint()
    {
        if (sentinel.patrolWaypoints.Length == 0) return;
        sentinel.agent.SetDestination(sentinel.patrolWaypoints[sentinel.patrolIndex].position);
    }

    // React to sounds — louder sounds are heard from farther away
    void OnSoundHeard(Vector3 soundPos, float intensity)
    {
        float range = sentinel.hearingRange * intensity;
        if (Vector3.Distance(sentinel.transform.position, soundPos) <= range)
        {
            sentinel.lastKnownPlayerPos = soundPos;
            sentinel.ChangeState(sentinel.investigateState);
        }
    }

    public override string ToString() => "Patrol";
}
