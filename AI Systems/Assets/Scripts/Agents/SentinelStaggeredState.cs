using UnityEngine;

// STAGGERED — Sentinel briefly stunned after being hit by the player
// Transitions:
//   -> Combat : stagger timer expires (recover)
public class SentinelStaggeredState : State
{
    CryptSentinel sentinel;
    float staggerTimer;

    public SentinelStaggeredState(CryptSentinel _sentinel)
    {
        sentinel = _sentinel;
    }

    public override void Enter()
    {
        // Stop moving while staggered
        sentinel.agent.ResetPath();
        sentinel.agent.speed = 0f;
        staggerTimer = sentinel.staggerDuration;
        Debug.Log("[Sentinel] Staggered!");
    }

    public override void Execute()
    {
        staggerTimer -= Time.deltaTime;
        if (staggerTimer <= 0f)
            sentinel.ChangeState(sentinel.combatState);
    }

    public override void Exit()
    {
        sentinel.agent.speed = sentinel.patrolSpeed;
    }

    public override string ToString() => "Staggered";
}
